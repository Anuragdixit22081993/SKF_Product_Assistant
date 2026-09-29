namespace SKF_Product_Assistant.Models;

public sealed class ConversationState
{
    public string ConversationId { get; set; } = string.Empty;

    public string? LastProductDesignation { get; set; }

    public string? LastAttribute { get; set; }

    public string? LastAnswer { get; set; }
}

