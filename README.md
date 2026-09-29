# SKF Product Assistant (Mini)

A .NET 8 Azure Functions application that provides SKF product information using **Microsoft Semantic Kernel, Azure OpenAI, function calling, conversation state, and Redis-based feedback persistence**.

The solution contains two AI agents and a lightweight orchestrator:

* **Q&A Agent** — answers SKF product questions using local JSON datasheets.
* **Feedback Agent** — detects and stores user feedback or corrections.
* **Orchestrator** — classifies the incoming message and routes it to the appropriate agent.

---

## 1. Technology Stack

* C#
* .NET 8
* Azure Functions v4 – Isolated Worker
* Microsoft Semantic Kernel
* Azure OpenAI
* Redis
* JSON product datasheets
* REST/HTTP API
* Git/GitHub

---

## 2. Architecture

```text
                     HTTP POST
                         |
                         v
              ProductAssistantFunction
                         |
                         v
          ProductAssistantOrchestrator
                    /          \
                   /            \
                  v              v
             Q&A Agent      Feedback Agent
                  |              |
                  v              v
       ProductDataPlugin   FeedbackPlugin
                  |              |
                  v              v
       ProductDataService    FeedbackStore
                  |              |
                  v              v
          Local JSON Files       Redis
```

The application uses Semantic Kernel to provide AI orchestration and function calling while keeping product data grounded in the supplied SKF datasheets.

---

## 3. Project Structure

```text
SKF_Product_Assistant/
│
├── Agents/
│   ├── QaAgent.cs
│   ├── FeedbackAgent.cs
│   └── ProductAssistantOrchestrator.cs
│
├── Data/
│   ├── 6205.json
│   └── 6205 N.json
│
├── Functions/
│   └── ProductAssistantFunction.cs
│
├── Models/
│   ├── UserRequest.cs
│   ├── ConversationState.cs
│   └── ProductAssistantResponse.cs
│
├── Plugins/
│   ├── ProductDataPlugin.cs
│   └── FeedbackPlugin.cs
│
├── Services/
│   ├── ProductDataService.cs
│   ├── ConversationStateService.cs
│   ├── RedisConnectionService.cs
│   └── FeedbackStore.cs
│
├── Program.cs
├── SKF_Product_Assistant.csproj
├── host.json
├── .gitignore
└── README.md
```

---

# 4. Prerequisites

The project is designed to run on any development environment that supports the required .NET and Azure Functions versions.

Install:

* .NET 8 SDK
* Azure Functions Core Tools v4
* Git

The application also requires access to:

* Azure OpenAI
* Redis

No machine-specific paths are required when .NET 8 is already configured as the active SDK.

---

# 5. Verify Prerequisites

Check the installed .NET version:

```bash
dotnet --version
```

The project targets:

```text
.NET 8
```

Check Azure Functions Core Tools:

```bash
func --version
```

Check Git:

```bash
git --version
```

---

# 6. Clone the Repository

Clone the repository:

```bash
git clone https://github.com/Anuragdixit22081993/SKF_Product_Assistant.git
```

Navigate to the project:

```bash
cd SKF_Product_Assistant
```

---

# 7. Restore and Build

Restore dependencies:

```bash
dotnet restore
```

Build the application:

```bash
dotnet build
```

The build should complete successfully before starting the Azure Function.

---

# 8. Configuration

The application uses environment-based configuration.

Required settings:

```text
AZURE_OPENAI_ENDPOINT
AZURE_OPENAI_API_KEY
AZURE_OPENAI_DEPLOYMENT
REDIS_CONNECTION_STRING
```

For local development, create or update:

```text
local.settings.json
```

Example structure:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "AZURE_OPENAI_ENDPOINT": "<your-azure-openai-endpoint>",
    "AZURE_OPENAI_API_KEY": "<your-azure-openai-key>",
    "AZURE_OPENAI_DEPLOYMENT": "<your-deployment-name>",
    "REDIS_CONNECTION_STRING": "<your-redis-connection-string>"
  }
}
```

### Security

Do not commit `local.settings.json` or any credentials to source control.

The repository `.gitignore` excludes local configuration and build output.

For deployed environments, configure these values through the Azure Function App application settings or the organization's approved secret-management solution.

---

# 9. Run Locally

Start the Azure Function:

```bash
func start
```

The HTTP endpoint will normally be available at:

```text
http://localhost:7071/api/ProductAssistant
```

The endpoint accepts HTTP POST requests.

### macOS / Multiple .NET Versions

If a development machine has multiple .NET versions installed, the machine may need to explicitly select .NET 8.

For example, the original development environment used Homebrew .NET 8 with:

```bash
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH func start
```

This command is **environment-specific** and is not required when .NET 8 is already the active SDK.

---

# 10. API Request

### Endpoint

```text
POST /api/ProductAssistant
```

### Header

```text
Content-Type: application/json
```

### Request Body

```json
{
  "message": "What is the width of SKF 6205?",
  "conversationId": "test-001"
}
```

---

# 11. Q&A Example

Request:

```json
{
  "message": "What is the width of SKF 6205?",
  "conversationId": "test-001"
}
```

Example response:

```json
{
  "ConversationId": "test-001",
  "Message": "The width of SKF 6205 is 15 mm.",
  "ProductDesignation": "6205",
  "Attribute": "Width",
  "FeedbackType": null,
  "Feedback": null
}
```

The Q&A Agent uses Semantic Kernel function calling to retrieve the requested value from the product datasheet.

---

# 12. Conversation Follow-up

The application maintains lightweight conversation state using the `conversationId`.

For example, after asking:

```text
What is the width of SKF 6205?
```

The user can ask:

```text
What about the bore diameter?
```

using the same conversation ID:

```json
{
  "message": "What about the bore diameter?",
  "conversationId": "test-001"
}
```

The application can use the previously identified product:

```text
6205
```

and retrieve the new attribute.

Example response:

```json
{
  "ConversationId": "test-001",
  "Message": "The bore diameter is 25 mm.",
  "ProductDesignation": "6205",
  "Attribute": "Bore diameter",
  "FeedbackType": null,
  "Feedback": null
}
```

---

# 13. Supported Product Datasheets

The current implementation includes the supplied local product datasheets:

```text
Data/
├── 6205.json
└── 6205 N.json
```

The `ProductDataService` reads these JSON files and searches supported product sections for the requested attribute.

The implementation currently supports:

* Dimensions
* Properties
* Performance
* Logistics
* Specifications

Only information present in the supplied datasheets is returned.

---

# 14. Grounded Q&A Behavior

The Q&A Agent is instructed to:

1. Identify the product designation.
2. Identify the requested attribute.
3. Use previous conversation state when appropriate.
4. Call the product data function before answering.
5. Never invent product information.
6. Return a clear response when an attribute is unavailable.
7. Keep responses concise.
8. Avoid exposing internal implementation details.

Example:

```text
User:
What is the weight of SKF 6205?

Assistant:
Weight is not available in the SKF datasheet for product 6205.
```

This prevents unsupported product information from being generated by the model.

---

# 15. Feedback Agent

The Feedback Agent handles messages such as:

```text
The answer was helpful.
```

```text
The answer is incorrect.
```

```text
The width should be 16 mm.
```

The agent identifies:

* Product
* Attribute
* Feedback type
* Feedback text

It uses the current conversation state to resolve product and attribute information when they are not explicitly repeated in the feedback.

---

# 16. Feedback Persistence

Feedback is persisted using Redis.

The flow is:

```text
User Feedback
      |
      v
Feedback Agent
      |
      v
Semantic Kernel Function Calling
      |
      v
FeedbackPlugin
      |
      v
FeedbackStore
      |
      v
Redis
```

Each feedback record contains information such as:

```text
ConversationId
ProductDesignation
Attribute
FeedbackType
Feedback
CreatedAtUtc
```

The Redis connection is configured using:

```text
REDIS_CONNECTION_STRING
```

The application does not hard-code Redis credentials.

---

# 17. Semantic Kernel Function Calling

The application uses Semantic Kernel functions for controlled access to application capabilities.

### Product function

```text
get_product_attribute
```

Purpose:

```text
Retrieve an attribute from the local SKF product datasheet.
```

### Feedback function

```text
save_feedback
```

Purpose:

```text
Persist user feedback to Redis.
```

The agents use Semantic Kernel's function-calling capability rather than directly embedding product data inside prompts.

---

# 18. Orchestration

The `ProductAssistantOrchestrator` acts as the entry point for AI routing.

The incoming message is classified as either:

```text
QUESTION
```

or:

```text
FEEDBACK
```

Routing:

```text
QUESTION
   |
   v
Q&A Agent
```

or:

```text
FEEDBACK
   |
   v
Feedback Agent
```

This keeps the responsibilities of the two agents separated.

---

# 19. Error Handling

The HTTP function validates:

* Request body
* Message
* Conversation ID

Invalid requests return HTTP `400`.

Unexpected application errors return HTTP `500`.

The application also handles unavailable product attributes by returning a grounded response instead of inventing a value.

---

# 20. Security and Configuration Practices

The implementation follows basic secure configuration practices:

* Secrets are not hard-coded in source code.
* `local.settings.json` is excluded from Git.
* Azure OpenAI credentials are read from environment variables.
* Redis credentials are read from environment variables.
* User input is validated before processing.
* Product data access is restricted to the supplied local datasheets.
* The AI agent is instructed not to fabricate unavailable product information.
* Build artifacts are excluded from source control.

For production deployment, secrets should be stored using the organization's approved Azure secret-management approach rather than committed to the repository.

---

# 21. Design Decisions

### Semantic Kernel

Semantic Kernel provides:

* AI orchestration
* Prompt execution
* Function calling
* Plugin integration

### Separate Agents

The Q&A and Feedback responsibilities are separated to improve maintainability and allow each agent to have focused instructions.

### Local Product Data

The supplied SKF product information is stored locally in JSON files.

This provides a deterministic data source for product attribute retrieval and reduces the risk of the LLM generating unsupported product information.

### Conversation State

A lightweight in-memory conversation state service is used for the current application instance.

The state tracks:

```text
ConversationId
LastProductDesignation
LastAttribute
LastAnswer
```

This enables contextual follow-up questions.

### Redis

Redis is used for feedback persistence.

This separates feedback storage from the AI agent logic and allows the storage implementation to be changed independently.

---

# 22. Current Validation

The following flows have been validated during development:

### Basic Q&A

```text
What is the width of SKF 6205?
```

Returns:

```text
15 mm
```

### Follow-up Question

```text
What about the bore diameter?
```

Returns:

```text
25 mm
```

### Second Product

```text
What is the outside diameter of SKF 6205 N?
```

Returns:

```text
52 mm
```

### Missing Attribute

```text
What is the weight of SKF 6205?
```

Returns an unavailable-data response rather than an invented value.

### Helpful Feedback

```text
The answer was helpful.
```

### Correction Feedback

```text
The width answer is incorrect. The correct width is 16 mm.
```

Feedback is persisted to Redis.

---

# 23. Limitations

The current implementation intentionally keeps the solution lightweight.

* Conversation state is currently stored in memory.
* Restarting the Function App clears the conversation state.
* Product lookup currently covers the supplied datasheets.
* Redis is used for feedback persistence rather than conversation state.
* No UI is included because the assignment requires an HTTP endpoint.
* Automated unit tests are not included in this submission.

These choices keep the implementation focused on the assignment requirements while leaving clear extension points for production hardening.

---

# 24. Possible Production Extensions

For a production implementation, the following could be added:

* Persistent conversation state using Redis or another distributed store.
* Managed identity for Azure service authentication where supported.
* Azure Key Vault integration for secrets.
* Structured application logging.
* Application Insights monitoring.
* Automated unit and integration tests.
* Authentication and authorization for the HTTP endpoint.
* Rate limiting and abuse protection.
* Additional product datasheets.
* More robust product/attribute normalization.
* CI/CD deployment through GitHub Actions or Azure DevOps.

---

# 25. Running the Project — Quick Reference

```bash
git clone https://github.com/Anuragdixit22081993/SKF_Product_Assistant.git

cd SKF_Product_Assistant

dotnet restore

dotnet build

func start
```

Then call:

```text
POST http://localhost:7071/api/ProductAssistant
```

with:

```json
{
  "message": "What is the width of SKF 6205?",
  "conversationId": "test-001"
}
```

---

# 26. Summary

SKF Product Assistant demonstrates a lightweight AI-enabled backend using:

* .NET 8
* Azure Functions
* Microsoft Semantic Kernel
* Azure OpenAI
* Function calling
* Two specialized AI agents
* Conversation state
* Local SKF product datasheets
* Redis feedback persistence
* Environment-based configuration

The solution is designed to be cloned and run in another development environment without relying on the original developer's machine-specific paths or local setup.
