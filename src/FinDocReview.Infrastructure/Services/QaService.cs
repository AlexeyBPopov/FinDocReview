using FinDocReview.Core.Entities;
using FinDocReview.Infrastructure.Data;
using Microsoft.SemanticKernel.ChatCompletion;

namespace FinDocReview.Infrastructure.Services;

public class QaService
{
    private readonly SemanticSearchService _searchService;
    private readonly IChatCompletionService _chatService;
    private readonly AppDbContext _db;
    private const string ModelUsed = "gpt-4o-mini";

    public QaService(
        SemanticSearchService searchService,
        IChatCompletionService chatService,
        AppDbContext db)
    {
        _searchService = searchService;
        _chatService = chatService;
        _db = db;
    }

    public async Task<AiQueryLog> AskAsync(
        Guid documentId,
        string userId,
        string question,
        CancellationToken ct = default)
    {
        // 1. Find relevant chunks
        var relevantChunks = await _searchService.FindRelevantChunksAsync(
            documentId, question, topK: 5, ct: ct);

        // 2. Build context from chunks
        var context = string.Join("\n\n---\n\n",
            relevantChunks.Select((c, i) => $"[Excerpt {i + 1}]\n{c.Content}"));

        // 3. Build prompt
        var prompt = $"""
            You are an assistant helping review financial documents.
            Answer the question based ONLY on the provided context.
            If the answer is not in the context, say "I could not find this information in the document."
            Be concise and precise.

            Context:
            {context}

            Question: {question}
            """;

        // 4. Call AI
        var chat = new ChatHistory();
        chat.AddUserMessage(prompt);

        var start = DateTime.UtcNow;
        var response = await _chatService.GetChatMessageContentAsync(chat, cancellationToken: ct);
        var latencyMs = (int)(DateTime.UtcNow - start).TotalMilliseconds;

        var answerText = response.Content ?? "No response received.";

        // 5. Log to DB
        var log = new AiQueryLog
        {
            DocumentId = documentId,
            UserId = userId,
            UserPrompt = question,
            AiResponse = answerText,
            SourceChunkIdsJson = System.Text.Json.JsonSerializer.Serialize(
                relevantChunks.Select(c => c.Id)),
            ModelUsed = ModelUsed,
            LatencyMs = latencyMs
        };

        _db.AiQueryLogs.Add(log);
        await _db.SaveChangesAsync(ct);

        return log;
    }
}