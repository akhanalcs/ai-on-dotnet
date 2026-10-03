using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.AI;
using RagChat.Web.Services.Ingestion;

namespace RagChat.Web.Services;

// A search result: the chunk + the sentence(s) the semantic ranker found most relevant (verbatim from the chunk).
public sealed record SearchHit(IngestedChunk Chunk, string? Caption);

public class SemanticSearch(
    SearchIndexClient indexClient,
    IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
    [FromKeyedServices("ingestion_directory")] DirectoryInfo ingestionDirectory,
    DataIngestor dataIngestor)
{
    private readonly SearchClient _searchClient = indexClient.GetSearchClient(IngestedChunk.IndexName);
    private Task? _ingestionTask;

    // The first call starts ingestion and stores the Task in _ingestionTask.
    // The await then waits until that Task is done, which means all documents are processed.
    // Any later call gets the same cached Task back. It's already complete, so the await returns immediately and ingestion doesn't run again.
    // SemanticSearch is a singleton, so _ingestionTask is shared by every user and request.
    public async Task LoadDocumentsAsync() => await (_ingestionTask ??= dataIngestor.IngestDataAsync(ingestionDirectory));

    // Hybrid search + semantic reranking, trimmed to what the user may read. One Azure AI Search query does it all:
    //   1. keyword (BM25) on the text  +  2. vector similarity on the embedding -> merged with Reciprocal Rank Fusion (RRF)
    //   3. the semantic ranker re-scores the top results by reading question + chunk together, and extracts a caption
    //   4. the security filter runs inside the index, so forbidden chunks never reach the app or the model
    public async Task<IReadOnlyList<SearchHit>> SearchAsync(
        string text, string? documentIdFilter, IReadOnlyCollection<string> userGroups, int maxResults)
    {
        // Default deny: a user with no groups sees nothing
        if (userGroups.Count == 0)
        {
            return [];
        }

        // Ensure documents have been loaded before searching
        await LoadDocumentsAsync();

        // Embed the question so it can be compared with the chunk embeddings
        var queryVector = await embeddingGenerator.GenerateVectorAsync(text);

        var options = new SearchOptions
        {
            // How many results to return to the model
            Size = maxResults,
            // Security trimming (+ optional single-file filter), applied inside the index
            Filter = Filter(userGroups, documentIdFilter),
            // Vector half of hybrid: the 50 chunks whose embeddings are closest to the question's
            VectorSearch = new()
            {
                Queries = { new VectorizedQuery(queryVector) { KNearestNeighborsCount = 50, Fields = { "embedding" } } }
            },
            // Turn on the semantic ranker to re-order the merged results, and return an extractive caption
            // (the most relevant sentence(s), copied word for word from the chunk)
            QueryType = SearchQueryType.Semantic,
            SemanticSearch = new()
            {
                SemanticConfigurationName = IngestedChunk.SemanticConfiguration,
                QueryCaption = new(QueryCaptionType.Extractive)
            }
        };

        // Keyword half of hybrid: passing the text runs BM25 over the searchable fields (content, context).
        // Azure AI Search merges keyword + vector results (RRF), then the semantic ranker re-orders them.
        var response = await _searchClient.SearchAsync<IngestedChunk>(text, options);

        var hits = new List<SearchHit>();
        await foreach (var result in response.Value.GetResultsAsync())
        {
            hits.Add(new SearchHit(result.Document, result.SemanticSearch?.Captions?.FirstOrDefault()?.Text));
        }
        return hits;
    }

    // OData filter, e.g. allowed_groups/any(g: search.in(g, 'COC-4|COC-6', '|')) and documentid eq 'x.pdf'
    private static string Filter(IReadOnlyCollection<string> userGroups, string? documentIdFilter)
    {
        var filter = $"allowed_groups/any(g: search.in(g, '{Escape(string.Join('|', userGroups))}', '|'))";
        return documentIdFilter is { Length: > 0 }
            ? $"{filter} and documentid eq '{Escape(documentIdFilter)}'"
            : filter;
    }

    // In OData string literals a single quote is escaped by doubling it
    internal static string Escape(string value) => value.Replace("'", "''");
}
