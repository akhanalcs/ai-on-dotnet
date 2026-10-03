using System.ComponentModel;
using Microsoft.Extensions.AI;

namespace RagChat.Web.Services;

// The RAG "brain": system prompt + the tools the model can call.
// Shared by the chat UI and the evaluation tests so both exercise the exact same behavior.
public sealed class RagAssistant(SemanticSearch search)
{
    public const string SystemPrompt = @"
        You are an assistant who answers questions about information you retrieve.
        Do not answer questions about anything else.
        Use only simple markdown to format your responses.
        Use the LoadDocuments tool to prepare for searches before answering any questions.
        Use the Search tool to find relevant information. When you do this, end your
        reply with citations in the special XML format:
        <citation filename='string'>exact quote here</citation>
        Always include the citation in your response if there are results.
        The quote must be max 5 words, taken word-for-word from the search result, and is the basis for why the citation is relevant.
        Don't refer to the presence of citations; just emit these tags right at the end, with no surrounding text.
        ";

    // Tools are created per user: the user's groups are captured here, in code.
    // They are NOT a tool parameter, so the model (or a prompt-injection) can't choose or widen them.
    public ChatOptions CreateChatOptions(IReadOnlyCollection<string> userGroups) => new()
    {
        Tools =
        [
            AIFunctionFactory.Create(
                search.LoadDocumentsAsync,
                name: "LoadDocuments",
                description: "Loads the documents needed for performing searches. Must be completed before a search can be executed, but only needs to be completed once."),
            AIFunctionFactory.Create(
                async (
                    [Description("The phrase to search for.")] string searchPhrase,
                    [Description("If possible, specify the filename to search that file only. If not provided or empty, the search includes all files.")] string? filenameFilter = null) =>
                {
                    var results = await search.SearchAsync(searchPhrase, filenameFilter, userGroups, maxResults: 5);
                    // Provenance travels with each result, so answers can point to the page/section
                    return results.Select(hit =>
                        $"<result filename=\"{hit.Chunk.DocumentId}\" page=\"{hit.Chunk.PageNumber}\" section=\"{hit.Chunk.Context}\">{hit.Chunk.Text}</result>");
                },
                name: "Search",
                description: "Searches for information using a phrase or keyword. Relies on documents already being loaded.")
        ]
    };
}
