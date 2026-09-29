using System.ComponentModel;
using Microsoft.SemanticKernel;
using SKF_Product_Assistant.Services;

namespace SKF_Product_Assistant.Plugins;

public sealed class FeedbackPlugin
{
    private readonly FeedbackStore _feedbackStore;

    public FeedbackPlugin(FeedbackStore feedbackStore)
    {
        _feedbackStore = feedbackStore;
    }

    [KernelFunction("save_feedback")]
    [Description("Stores user feedback about an SKF product answer.")]
    public async Task<string> SaveFeedback(
        [Description("Conversation identifier.")]
        string conversationId,
        [Description("Product designation.")]
        string productDesignation,
        [Description("Product attribute.")]
        string attribute,
        [Description("Type of feedback such as helpful, unhelpful, or correction.")]
        string feedbackType,
        [Description("The user's feedback text.")]
        string feedback)
    {
        if (string.IsNullOrWhiteSpace(conversationId) ||
            string.IsNullOrWhiteSpace(feedback))
        {
            return "Feedback could not be saved because required information is missing.";
        }

        await _feedbackStore.SaveAsync(
            conversationId,
            productDesignation,
            attribute,
            feedbackType,
            feedback);

        return "Feedback saved successfully.";
    }
}