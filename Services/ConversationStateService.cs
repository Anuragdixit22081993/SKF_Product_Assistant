using System.Collections.Concurrent;
using SKF_Product_Assistant.Models;

namespace SKF_Product_Assistant.Services;

public sealed class ConversationStateService
{
    private readonly ConcurrentDictionary<string, ConversationState> _states = new();

    public ConversationState GetOrCreate(string conversationId)
    {
        if (string.IsNullOrWhiteSpace(conversationId))
        {
            throw new ArgumentException(
                "Conversation ID is required.",
                nameof(conversationId));
        }

        return _states.GetOrAdd(
            conversationId,
            id => new ConversationState
            {
                ConversationId = id
            });
    }

    public void Update(
        string conversationId,
        string? productDesignation,
        string? attribute,
        string? answer)
    {
        var state = GetOrCreate(conversationId);

        if (!string.IsNullOrWhiteSpace(productDesignation))
        {
            state.LastProductDesignation = productDesignation;
        }

        if (!string.IsNullOrWhiteSpace(attribute))
        {
            state.LastAttribute = attribute;
        }

        if (!string.IsNullOrWhiteSpace(answer))
        {
            state.LastAnswer = answer;
        }
    }
}