using System.Text.Json.Serialization;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;

namespace RagChat.Web.Services;

// One chunk = one document in the Azure AI Search index.
// The attributes define the index schema (FieldBuilder reads them), JsonPropertyName = field name in the index.
public class IngestedChunk
{
    public const string IndexName = "ragchat-chunks";
    public const string SemanticConfiguration = "default";
    private const string VectorProfile = "default";
    public const int VectorDimensions = 1536; // text-embedding-3-small output size

    [SimpleField(IsKey = true)]
    [JsonPropertyName("key")]
    public string Key { get; set; } = "";

    // Source file name; filterable so search can be limited to one document
    [SimpleField(IsFilterable = true)]
    [JsonPropertyName("documentid")]
    public string DocumentId { get; set; } = "";

    // The chunk text. Searchable = keyword (BM25) half of hybrid search
    [SearchableField(AnalyzerName = LexicalAnalyzerName.Values.EnMicrosoft)]
    [JsonPropertyName("content")]
    public string Text { get; set; } = "";

    // Section heading path, e.g. "3. Setup and Installation > 3.1 Charging"
    [SearchableField]
    [JsonPropertyName("context")]
    public string? Context { get; set; }

    // PDF page the chunk starts on (null for Markdown). Lets a citation open the exact page.
    [SimpleField]
    [JsonPropertyName("page")]
    public int? PageNumber { get; set; }

    // Groups allowed to read this chunk, e.g. ["COC-6"]. Every search filters on it (security trimming).
    [SimpleField(IsFilterable = true)]
    [JsonPropertyName("allowed_groups")]
    public string[] AllowedGroups { get; set; } = [];

    // Fingerprint of the source file + its access groups. Unchanged hash = skip re-ingesting.
    [SimpleField]
    [JsonPropertyName("content_hash")]
    public string? ContentHash { get; set; }

    // Embedding of Text = vector half of hybrid search
    [VectorSearchField(VectorSearchDimensions = VectorDimensions, VectorSearchProfileName = VectorProfile)]
    [JsonPropertyName("embedding")]
    public ReadOnlyMemory<float>? Embedding { get; set; }

    // The index definition, like a CREATE TABLE. Three parts:
    public static SearchIndex CreateIndex() => new(
        IndexName,
        // 1. Columns: FieldBuilder turns the attributes on the properties above into index fields
        new FieldBuilder().Build(typeof(IngestedChunk)))
    {
        // 2. How "embedding" is searched: HNSW = a graph index for fast nearest-neighbor lookup (cosine similarity by default).
        //    The [VectorSearchField] above points at this profile by name.
        VectorSearch = new()
        {
            Algorithms = { new HnswAlgorithmConfiguration("hnsw") },
            Profiles = { new VectorSearchProfile(VectorProfile, "hnsw") }
        },
        // 3. What the semantic ranker (reranker) reads: the section heading as the title, the chunk text as the content
        SemanticSearch = new()
        {
            Configurations =
            {
                new SemanticConfiguration(SemanticConfiguration, new SemanticPrioritizedFields
                {
                    TitleField = new SemanticField("context"),
                    ContentFields = { new SemanticField("content") }
                })
            }
        }
    };
}
