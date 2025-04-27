using Microsoft.Extensions.VectorData;

namespace AIChat.Services;

public class SemanticSearchRecord
{
    [VectorStoreRecordKey]
    public required string Key { get; set; }

    [VectorStoreRecordData(IsFilterable = true)]
    public required string FileName { get; set; }

    [VectorStoreRecordData]
    public int PageNumber { get; set; }

    [VectorStoreRecordData]
    public required string Text { get; set; }
    // https://dotnetfiddle.net/Vqft5f
    // 1536 is the default vector size for the OpenAI text-embedding-3-small model
    [VectorStoreRecordVector(1536, DistanceFunction.CosineSimilarity)]
    public ReadOnlyMemory<float> Vector { get; set; }
}
