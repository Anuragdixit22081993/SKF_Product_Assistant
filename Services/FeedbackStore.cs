using System.Text.Json;
using StackExchange.Redis;

namespace SKF_Product_Assistant.Services;

public sealed class FeedbackStore
{
    private readonly RedisConnectionService _redis;

    public FeedbackStore(RedisConnectionService redis)
    {
        _redis = redis;
    }

    public async Task SaveAsync(
        string conversationId,
        string productDesignation,
        string attribute,
        string feedbackType,
        string feedback)
    {
        var record = new
        {
            ConversationId = conversationId,
            ProductDesignation = productDesignation,
            Attribute = attribute,
            FeedbackType = feedbackType,
            Feedback = feedback,
            CreatedAtUtc = DateTime.UtcNow
        };

        var key = $"feedback:{conversationId}:{Guid.NewGuid():N}";

        await _redis.Database.StringSetAsync(
            key,
            JsonSerializer.Serialize(record));
    }
}