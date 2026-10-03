using System.Text.Json.Serialization;
using Microsoft.Extensions.VectorData;

namespace RagChat.Web.Services;

// One chunk = one Qdrant point. StorageName is the field name in Qdrant (payload key, or the point ID for the key).
public class IngestedChunk
{
    public const int VectorDimensions = 1536; // 1536 is the default vector size for the OpenAI text-embedding-3-small model
    public const string VectorDistanceFunction = DistanceFunction.CosineSimilarity;
    public const string CollectionName = "data-ragchat-chunks";

    // Qdrant point ID
    [VectorStoreKey(StorageName = "key")]
    [JsonPropertyName("key")]
    public required Guid Key { get; set; }

    [VectorStoreData(StorageName = "documentid")]
    [JsonPropertyName("documentid")]
    public required string DocumentId { get; set; }

    [VectorStoreData(StorageName = "content")]
    [JsonPropertyName("content")]
    public required string Text { get; set; }

    [VectorStoreData(StorageName = "context")]
    [JsonPropertyName("context")]
    public string? Context { get; set; }

    // A string vector property means "embed this text for me" with the registered IEmbeddingGenerator.
    // Qdrant stores the resulting 1536 floats as the point's vector (dimensions, not a length limit on Text).
    [VectorStoreVector(VectorDimensions, DistanceFunction = VectorDistanceFunction, StorageName = "embedding")]
    [JsonPropertyName("embedding")]
    public string? Vector => Text;
}
