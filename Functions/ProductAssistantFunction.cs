using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using SKF_Product_Assistant.Agents;
using SKF_Product_Assistant.Models;

namespace SKF_Product_Assistant.Functions;

public sealed class ProductAssistantFunction
{
    private readonly ProductAssistantOrchestrator _orchestrator;

    public ProductAssistantFunction(
        ProductAssistantOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
    }

    [Function("ProductAssistant")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post")]
        HttpRequestData req)
    {
        var response = req.CreateResponse();

        try
        {
            var request = await JsonSerializer.DeserializeAsync<UserRequest>(
                req.Body,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (request is null ||
                string.IsNullOrWhiteSpace(request.Message) ||
                string.IsNullOrWhiteSpace(request.ConversationId))
            {
                response.StatusCode = HttpStatusCode.BadRequest;

                await response.WriteAsJsonAsync(new
                {
                    message = "Message and conversationId are required."
                });

                return response;
            }

            var agentResponse = await _orchestrator.HandleAsync(
                request.ConversationId,
                request.Message);

            response.StatusCode = HttpStatusCode.OK;

            await response.WriteAsJsonAsync(agentResponse);

            return response;
        }
        catch (Exception ex)
        {
            Console.WriteLine("ERROR: ProductAssistant failed.");
            Console.WriteLine($"ERROR TYPE: {ex.GetType().FullName}");
            Console.WriteLine($"ERROR MESSAGE: {ex.Message}");

            response.StatusCode = HttpStatusCode.InternalServerError;

            await response.WriteAsJsonAsync(new
            {
                message = "An error occurred while processing the request.",
                error = ex.Message
            });

            return response;
        }
    }
}