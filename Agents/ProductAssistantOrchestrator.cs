using Microsoft.SemanticKernel;
using SKF_Product_Assistant.Models;

namespace SKF_Product_Assistant.Agents;

public sealed class ProductAssistantOrchestrator
{
    private readonly Kernel _kernel;
    private readonly QaAgent _qaAgent;
    private readonly FeedbackAgent _feedbackAgent;

    public ProductAssistantOrchestrator(
        Kernel kernel,
        QaAgent qaAgent,
        FeedbackAgent feedbackAgent)
    {
        _kernel = kernel;
        _qaAgent = qaAgent;
        _feedbackAgent = feedbackAgent;
    }

    public async Task<ProductAssistantResponse> HandleAsync(
        string conversationId,
        string message)
    {
        var classificationPrompt = $"""
            Classify the following user message.

            Return ONLY one word:
            QUESTION
            or
            FEEDBACK

            QUESTION means the user is asking for SKF product information.

            FEEDBACK means the user is commenting on, correcting, or
            evaluating a previous answer.

            User message:
            {message}
            """;

        var result = await _kernel.InvokePromptAsync(
            classificationPrompt);

        var classification = result
            .ToString()
            .Trim()
            .ToUpperInvariant();

        if (classification.Contains("FEEDBACK"))
        {
            return await _feedbackAgent.HandleAsync(
                conversationId,
                message);
        }

        return await _qaAgent.AskAsync(
            conversationId,
            message);
    }
}