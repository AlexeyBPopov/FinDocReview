using System.Text.Json;
using FinDocReview.Core.Entities;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace FinDocReview.Infrastructure.Services;

public class AiSummarizationService
{
    private readonly IChatCompletionService _chatService;
    private const string ModelUsed = "gpt-4o-mini";

    public AiSummarizationService(IChatCompletionService chatService)
    {
        _chatService = chatService;
    }

    public async Task<DocumentSummary> SummarizeAsync(
        Guid documentId,
        string documentText,
        CancellationToken ct = default)
    {
        // Use first ~3000 chars to keep costs low
        var excerpt = documentText.Length > 3000
            ? documentText[..3000]
            : documentText;

        var jsonTemplate = """
            {
                "summary": "2-3 sentence overview of the document",
                "keyRisks": ["risk1", "risk2"],
                "actionItems": ["item1", "item2"]
            }
            """;

        var prompt = $"""
            You are a financial document analyst. Analyze the following document excerpt.
            Respond ONLY with valid JSON in this exact structure, no markdown, no extra text:
            {jsonTemplate}

            Document:
            {excerpt}
            """;

        var chat = new ChatHistory();
        chat.AddUserMessage(prompt);

        var start = DateTime.UtcNow;
        var response = await _chatService.GetChatMessageContentAsync(chat, cancellationToken: ct);
        var latencyMs = (int)(DateTime.UtcNow - start).TotalMilliseconds;

        var responseText = response.Content ?? string.Empty;

        // Parse JSON response
        SummaryResponse? parsed = null;
        try
        {
            parsed = JsonSerializer.Deserialize<SummaryResponse>(responseText,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            // If parsing fails, store raw response as summary
        }

        return new DocumentSummary
        {
            DocumentId = documentId,
            Summary = parsed?.Summary ?? responseText,
            KeyRisksJson = parsed?.KeyRisks != null
                ? JsonSerializer.Serialize(parsed.KeyRisks)
                : null,
            ActionItemsJson = parsed?.ActionItems != null
                ? JsonSerializer.Serialize(parsed.ActionItems)
                : null,
            ModelUsed = ModelUsed,
            InputTokens = 0,  // SK doesn't expose token counts easily — acceptable for MVP
            OutputTokens = 0
        };
    }

    private sealed class SummaryResponse
    {
        public string Summary { get; set; } = string.Empty;
        public List<string>? KeyRisks { get; set; }
        public List<string>? ActionItems { get; set; }
    }
}