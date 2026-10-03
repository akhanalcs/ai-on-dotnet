using System.Runtime.CompilerServices;
using Microsoft.Extensions.DataIngestion;
using RagChat.Web.Services.Security;

namespace RagChat.Web.Services.Ingestion;

// Ingestion step: stamps every chunk with the groups allowed to read its source document.
// VectorStoreWriter stores chunk metadata as payload, so this lands in Qdrant as "allowed_groups".
public sealed class AccessControlProcessor(DocumentAccessPolicy policy) : IngestionChunkProcessor<string>
{
    public const string MetadataKey = "allowed_groups";

    public override async IAsyncEnumerable<IngestionChunk<string>> ProcessAsync(
        IAsyncEnumerable<IngestionChunk<string>> chunks,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var chunk in chunks.WithCancellation(cancellationToken))
        {
            chunk.Metadata[MetadataKey] = policy.GroupsFor(chunk.Document.Identifier);
            yield return chunk;
        }
    }
}
