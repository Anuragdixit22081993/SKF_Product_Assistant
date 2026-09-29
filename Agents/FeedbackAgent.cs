using System.Text.Json;
using Microsoft.SemanticKernel;
using SKF_Product_Assistant.Models;
using SKF_Product_Assistant.Plugins;
using SKF_Product_Assistant.Services;

namespace SKF_Product_Assistant.Agents;

public sealed class FeedbackAgent
{
    private readonly Kernel _kernel;
    private readonly ConversationStateService _stateService;

    public FeedbackAgent(
        Kernel kernel,
        ConversationStateService stateService,
        FeedbackStore feedbackStore)
    {
        _kernel = kernel;
        _stateService = stateService;

        var feedbackPlugin = new FeedbackPlugin(feedbackStore);

        _kernel.Plugins.AddFromObject(
            feedbackPlugin,
            nameof(FeedbackPlugin));
    }

    public async Task<ProductAssistantResponse> HandleAsync(
        string conversationId,
        string message)
    {
        var state = _stateService.GetOrCreate(conversationId);

        var prompt = $"""
            You are the SKF Product Feedback Agent.

            Analyze the user's message as feedback about the previous
            SKF product answer.

            Feedback types:
            - helpful
            - unhelpful
            - correction
            - general

            Use previous conversation context when available.

            Conversation ID:
            {conversationId}

            Previous product:
            {state.LastProductDesignation ?? "none"}

            Previous attribute:
            {state.LastAttribute ?? "none"}

            Previous answer:
            {state.LastAnswer ?? "none"}

            User feedback:
            {message}

            If the message contains feedback, call the save_feedback
            function.

            Use the previous product and attribute when the user does
            not explicitly mention them.

            Return ONLY valid JSON.

            Return these fields:
            message
            productDesignation
            attribute
            feedbackType
            feedback
            """;

        var settings = new PromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };

        var result = await _kernel.InvokePromptAsync(
            prompt,
            new(settings));

        var rawResponse = result.ToString().Trim();

        var json = ExtractJson(rawResponse);

        if (json is null)
        {
            return new ProductAssistantResponse
            {
                ConversationId = conversationId,
                Message = rawResponse,
                ProductDesignation = state.LastProductDesignation,
                Attribute = state.LastAttribute
            };
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<FeedbackResult>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (parsed is null)
            {
                return new ProductAssistantResponse
                {
                    ConversationId = conversationId,
                    Message = rawResponse,
                    ProductDesignation = state.LastProductDesignation,
                    Attribute = state.LastAttribute
                };
            }

            var productDesignation =
                string.IsNullOrWhiteSpace(parsed.ProductDesignation)
                    ? state.LastProductDesignation
                    : parsed.ProductDesignation;

            var attribute =
                string.IsNullOrWhiteSpace(parsed.Attribute)
                    ? state.LastAttribute
                    : parsed.Attribute;

            _stateService.Update(
                conversationId,
                productDesignation,
                attribute,
                parsed.Message);

            return new ProductAssistantResponse
            {
                ConversationId = conversationId,
                Message = parsed.Message,
                ProductDesignation = productDesignation,
                Attribute = attribute,
                FeedbackType = parsed.FeedbackType,
                Feedback = parsed.Feedback
            };
        }
        catch (JsonException)
        {
            return new ProductAssistantResponse
            {
                ConversationId = conversationId,
                Message = rawResponse,
                ProductDesignation = state.LastProductDesignation,
                Attribute = state.LastAttribute
            };
        }
    }

    private static string? ExtractJson(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
        {
            return null;
        }

        var cleaned = response.Trim();

        if (cleaned.StartsWith("```"))
        {
            var firstNewLine = cleaned.IndexOf('\n');

            if (firstNewLine >= 0)
            {
                cleaned = cleaned[(firstNewLine + 1)..];
            }

            var closingFence = cleaned.LastIndexOf("```");

            if (closingFence >= 0)
            {
                cleaned = cleaned[..closingFence];
            }
        }

        var start = cleaned.IndexOf('{');
        var end = cleaned.LastIndexOf('}');

        if (start < 0 || end <= start)
        {
            return null;
        }

        return cleaned[start..(end + 1)];
    }

    private sealed class FeedbackResult
    {
        public string Message { get; set; } = string.Empty;

        public string? ProductDesignation { get; set; }

        public string? Attribute { get; set; }

        public string? FeedbackType { get; set; }

        public string? Feedback { get; set; }
    }
}