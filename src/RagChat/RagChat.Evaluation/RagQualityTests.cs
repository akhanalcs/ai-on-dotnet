using System.Text.Json;
using Microsoft.Extensions.AI.Evaluation;
using Microsoft.Extensions.AI.Evaluation.Quality;
using Xunit.Abstractions;

namespace RagChat.Evaluation;

// UserGroups = who is asking. Each question is asked by a user allowed to read the expected document.
public sealed record GoldenItem(string Question, string ExpectedDocument, string GroundTruth, string[] UserGroups);

// Runs every golden question through the real RAG pipeline and scores the answer.
// Two kinds of checks:
//   1. Retrieval (deterministic): did search return the expected document?
//   2. Answer quality (LLM-as-judge): relevance, groundedness, completeness, retrieval, each scored 1-5.
[Collection(RagCollection.Name)]
public sealed class RagQualityTests(RagFixture fixture, ITestOutputHelper output)
{
    public static TheoryData<GoldenItem> GoldenSet()
    {
        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "golden-set.json"));
        var items = JsonSerializer.Deserialize<List<GoldenItem>>(json, JsonSerializerOptions.Web)!;
        return new TheoryData<GoldenItem>(items);
    }

    // one test per golden question
    [Theory]
    [MemberData(nameof(GoldenSet))]
    public async Task Answers_are_relevant_grounded_and_cite_the_right_document(GoldenItem item)
    {
        // Asks like the UI does, as a user allowed to read the expected document (see RagFixture.AskAsync)
        var answer = await fixture.AskAsync(item.Question, item.UserGroups);

        output.WriteLine($"Q: {item.Question}  (as {string.Join(", ", item.UserGroups)})");
        output.WriteLine($"A: {answer.Response.Text}");
        output.WriteLine($"Retrieved: {string.Join(", ", answer.RetrievedDocuments)}");

        // Check 1 (deterministic): Assert.Contains(expectedDocument, retrievedDocuments). No AI is involved; either search found the right document or it didn't.
        Assert.Contains(item.ExpectedDocument, answer.RetrievedDocuments);

        // Check 2 (LLM-as-judge): EvaluateAsync sends the question, the answer and some extra context to the judge:
        // the retrieved chunks, for Groundedness: "is every claim backed by these chunks?"
        // the correct answer from the golden set, for Completeness: "did the answer cover it?"
        // the retrieved chunks again, for Retrieval: "were these the right chunks to fetch?"
        await using var scenario = await fixture.Reporting.CreateScenarioRunAsync(item.Question);
        var result = await scenario.EvaluateAsync(
            answer.Messages,
            answer.Response,
            additionalContext:
            [
                new GroundednessEvaluatorContext(string.Join("\n\n", answer.RetrievedChunks)),
                new CompletenessEvaluatorContext(item.GroundTruth),
                new RetrievalEvaluatorContext(answer.RetrievedChunks)
            ]);

        // Each metric gets a score from 1 to 5 with the judge's reasoning, printed in the test output. The test fails if any rating is flagged as failed.
        foreach (var metric in result.Metrics.Values)
        {
            var score = (metric as NumericMetric)?.Value;
            output.WriteLine($"{metric.Name}: {score} ({metric.Interpretation?.Rating}) {metric.Reason}");
        }

        Assert.All(result.Metrics.Values, metric =>
            Assert.False(metric.Interpretation?.Failed ?? true, $"{metric.Name} failed: {metric.Reason}"));
    }
}
