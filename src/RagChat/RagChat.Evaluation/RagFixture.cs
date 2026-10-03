using System.Text.Json;
using System.Text.RegularExpressions;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;
using Microsoft.Extensions.AI.Evaluation.Quality;
using Microsoft.Extensions.AI.Evaluation.Reporting;
using Microsoft.Extensions.AI.Evaluation.Reporting.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RagChat.Web.Services;

namespace RagChat.Evaluation;

// One answer from the RAG pipeline, plus what the Search tool returned while producing it.
public sealed record RagAnswer(List<ChatMessage> Messages, ChatResponse Response, List<string> RetrievedChunks, List<string> RetrievedDocuments);

// All test classes in this collection share ONE fixture, so the AppHost starts only once per test run.
[CollectionDefinition(Name)]
public sealed class RagCollection : ICollectionFixture<RagFixture>
{
    public const string Name = "Rag";
}

// Starts the AppHost once (Qdrant, MarkItDown, Azure OpenAI), then builds the same RAG services the web app uses.
public sealed partial class RagFixture : IAsyncLifetime
{
    private DistributedApplication? _app;
    private IHost? _host;

    public IServiceProvider Services => _host!.Services;
    public ReportingConfiguration Reporting { get; private set; } = default!;

    public async Task InitializeAsync()
    {
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.RagChat_AppHost>();
        _app = await appHost.BuildAsync();
        await _app.StartAsync();
        await _app.ResourceNotifications.WaitForResourceHealthyAsync("vectordb");
        await _app.ResourceNotifications.WaitForResourceAsync("openai", KnownResourceStates.Running);
        await _app.ResourceNotifications.WaitForResourceAsync("markitdown", KnownResourceStates.Running);

        // DocumentReader reads this to reach the PDF -> Markdown MCP server
        Environment.SetEnvironmentVariable("MARKITDOWN_MCP_URL", _app.GetEndpoint("markitdown", "http").ToString().TrimEnd('/'));

        // Content root = test output folder, which holds the web app's appsettings.json (incl. the DocumentAccess policy) and Data/
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { ContentRootPath = AppContext.BaseDirectory });
        builder.Configuration["ConnectionStrings:openai"] = await _app.GetConnectionStringAsync("openai");
        builder.Configuration["ConnectionStrings:vectordb"] = await _app.GetConnectionStringAsync("vectordb");
        builder.AddRagChat(ingestionDirectory: Path.Combine(AppContext.BaseDirectory, "Data"));
        _host = builder.Build();

        // The same chat deployment acts as the judge. A stronger judge model gives more reliable scores.
        var judge = new ChatConfiguration(Services.GetRequiredService<IChatClient>());
        // This sets up the evaluation
        Reporting = DiskBasedReportingConfiguration.Create(
            // where results are saved
            storageRootPath: Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "eval-results")),
            // the 4 judges (Relevance, Groundedness, Completeness, Retrieval)
            evaluators: [new RelevanceEvaluator(), new GroundednessEvaluator(), new CompletenessEvaluator(), new RetrievalEvaluator()],
            // judge is the same gpt-5-mini deployment
            chatConfiguration: judge,
            // if nothing changed, the judge isn't called again
            enableResponseCaching: true,
            // groups this run's results in the report
            executionName: DateTime.Now.ToString("yyyyMMddTHHmmss"));
    }

    // Asks a question exactly like the chat UI does, as a user who belongs to userGroups.
    public async Task<RagAnswer> AskAsync(string question, IReadOnlyCollection<string> userGroups)
    {
        var assistant = Services.GetRequiredService<RagAssistant>();
        var chatClient = Services.GetRequiredService<IChatClient>();

        // It asks the question the way the UI does: system prompt + question + the assistant's tools, then calls GetResponseAsync.
        List<ChatMessage> messages = [new(ChatRole.System, RagAssistant.SystemPrompt), new(ChatRole.User, question)];
        var response = await chatClient.GetResponseAsync(messages, assistant.CreateChatOptions(userGroups));

        // What the Search tool returned to the model during this answer
        // It collects what search returned from the response's FunctionResultContent (the tool results) and extracts the file names from the <result filename="..."> tags.
        var chunks = response.Messages
            .SelectMany(m => m.Contents)
            .OfType<FunctionResultContent>()
            .SelectMany(r => AsStrings(r.Result))
            .ToList();
        var documents = chunks
            .Select(c => ResultFilename().Match(c))
            .Where(m => m.Success)
            .Select(m => m.Groups["file"].Value)
            .Distinct()
            .ToList();

        return new RagAnswer(messages, response, chunks, documents);
    }

    public async Task DisposeAsync()
    {
        _host?.Dispose();
        if (_app is not null)
        {
            await _app.DisposeAsync();
        }
    }

    private static IEnumerable<string> AsStrings(object? result) => result switch
    {
        IEnumerable<string> strings => strings,
        JsonElement { ValueKind: JsonValueKind.Array } array => array.EnumerateArray().Select(e => e.GetString() ?? ""),
        null => [],
        _ => [result.ToString() ?? ""]
    };

    [GeneratedRegex("<result filename=\"(?<file>[^\"]+)\">")]
    private static partial Regex ResultFilename();
}
