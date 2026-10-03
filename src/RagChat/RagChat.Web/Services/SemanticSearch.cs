using RagChat.Web.Services.Ingestion;
using Microsoft.Extensions.VectorData;

namespace RagChat.Web.Services;

public class SemanticSearch(
    VectorStoreCollection<Guid, IngestedChunk> vectorCollection,
    [FromKeyedServices("ingestion_directory")] DirectoryInfo ingestionDirectory,
    DataIngestor dataIngestor)
{
    private Task? _ingestionTask;

    // The first call starts ingestion and stores the Task in _ingestionTask.
    // The await then waits until that Task is done, which means all documents are processed.
    // Any later call gets the same cached Task back. It's already complete, so the await returns immediately and ingestion doesn't run again.
    // SemanticSearch is a singleton, so _ingestionTask is shared by every user and request.
    public async Task LoadDocumentsAsync() => await ( _ingestionTask ??= dataIngestor.IngestDataAsync(ingestionDirectory, searchPattern: "*.*"));

    // Security trimming: only chunks the user's groups may read can come back.
    // The filter runs inside Qdrant, so forbidden chunks never reach the app or the model.
    public async Task<IReadOnlyList<IngestedChunk>> SearchAsync(
        string text, string? documentIdFilter, IReadOnlyCollection<string> userGroups, int maxResults)
    {
        // Default deny: a user with no groups sees nothing
        if (userGroups.Count == 0)
        {
            return [];
        }

        // Ensure documents have been loaded before searching
        await LoadDocumentsAsync();

        // Qdrant translates this into a payload filter: allowed_groups matches ANY of the user's groups
        // (+ same document, when the model asked for one file).
        // userGroups stays an IReadOnlyCollection (not an array) so Contains is Enumerable.Contains, which the filter translator understands.
        var nearest = vectorCollection.SearchAsync(text, maxResults, new VectorSearchOptions<IngestedChunk>
        {
            Filter = documentIdFilter is { Length: > 0 }
                ? r => r.AllowedGroups.Any(g => userGroups.Contains(g)) && r.DocumentId == documentIdFilter
                : r => r.AllowedGroups.Any(g => userGroups.Contains(g)),
        });

        return await nearest.Select(result => result.Record).ToListAsync();
    }
}
