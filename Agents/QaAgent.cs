using System.Text.Json;
using Microsoft.SemanticKernel;
using SKF_Product_Assistant.Models;
using SKF_Product_Assistant.Plugins;
using SKF_Product_Assistant.Services;

namespace SKF_Product_Assistant.Agents;

public sealed class QaAgent
{
    private readonly Kernel _kernel;
    private readonly ConversationStateService _stateService;

    public QaAgent(
        Kernel kernel,
        ConversationStateService stateService,
        ProductDataService productDataService)
    {
        _kernel = kernel;
        _stateService = stateService;

        var productDataPlugin = new ProductDataPlugin(
            productDataService);

        _kernel.Plugins.AddFromObject(
            productDataPlugin,
            nameof(ProductDataPlugin));
    }

    public async Task<ProductAssistantResponse> AskAsync(
        string conversationId,
        string message)
    {
        var state = _stateService.GetOrCreate(conversationId);

        var prompt = $"""
            You are the SKF Product Q&A Agent.

            Answer questions only using SKF product information available
            through the get_product_attribute function.

            Rules:
            1. Identify the exact product designation.
            2. Identify the requested attribute.
            3. If the product is not mentioned in the current question,
               use the previous product from conversation state.
            4. Always call get_product_attribute before answering.
            5. Never invent or guess product information.
            6. If the information is unavailable, clearly state that it is
               not available in the SKF datasheet.
            7. Keep the answer concise.
            8. Do not mention internal tools or implementation details.
            9. Return ONLY valid JSON.
            10. Do not use Markdown formatting.

            Return JSON with these fields:
            message
            productDesignation
            attribute

            Previous product:
            {state.LastProductDesignation ?? "none"}

            Previous attribute:
            {state.LastAttribute ?? "none"}

            User question:
            {message}
            """;

        var settings = new PromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };

        var result = await _kernel.InvokePromptAsync(
            prompt,
            new(settings));

        var rawResponse = result.ToString();

        var json = ExtractJson(rawResponse);

        if (json is null)
        {
            return new ProductAssistantResponse
            {
                ConversationId = conversationId,
                Message = rawResponse.Trim(),
                ProductDesignation = state.LastProductDesignation,
                Attribute = state.LastAttribute
            };
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<QaAgentResult>(
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
                    Message = rawResponse.Trim(),
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
                Attribute = attribute
            };
        }
        catch (JsonException)
        {
            return new ProductAssistantResponse
            {
                ConversationId = conversationId,
                Message = rawResponse.Trim(),
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

    private sealed class QaAgentResult
    {
        public string Message { get; set; } = string.Empty;

        public string? ProductDesignation { get; set; }

        public string? Attribute { get; set; }
    }
}
