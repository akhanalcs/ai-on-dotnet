using Microsoft.Extensions.AI;
using RagChat.Web.Services.Ingestion;
using RagChat.Web.Services.Security;

namespace RagChat.Web.Services;

public static class RagChatServiceExtensions
{
    // Registers everything the RAG pipeline needs. Used by Program.cs and by the evaluation tests.
    public static IHostApplicationBuilder AddRagChat(this IHostApplicationBuilder builder, string ingestionDirectory)
    {
        var openai = builder.AddAzureOpenAIClient("openai");
        openai.AddChatClient("chat")
            .UseFunctionInvocation()
            .UseOpenTelemetry(configure: c =>
                c.EnableSensitiveData = builder.Environment.IsDevelopment());
        openai.AddEmbeddingGenerator("text-embedding-3-small");

        builder.AddQdrantClient("vectordb");
        builder.Services.AddQdrantVectorStore();
        builder.Services.AddQdrantCollection<Guid, IngestedChunk>(IngestedChunk.CollectionName);
        builder.Services.AddSingleton<DocumentAccessPolicy>();
        builder.Services.AddSingleton<AccessControlProcessor>();
        builder.Services.AddSingleton<DataIngestor>();
        builder.Services.AddSingleton<SemanticSearch>();
        builder.Services.AddKeyedSingleton("ingestion_directory", new DirectoryInfo(ingestionDirectory));
        builder.Services.AddSingleton<RagAssistant>();

        return builder;
    }
}
