namespace SKF_Product_Assistant.Models;

public sealed class ProductAssistantResponse
{
    public string ConversationId { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string? ProductDesignation { get; set; }

    public string? Attribute { get; set; }

    public string? FeedbackType { get; set; }

    public string? Feedback { get; set; }
}