namespace SKF_Product_Assistant.Models;

public sealed class UserRequest
{
    public string Message { get; set; } = string.Empty;
    public string ConversationId { get; set; } = string.Empty;
}