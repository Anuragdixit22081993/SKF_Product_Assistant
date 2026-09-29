# SKF Product Assistant (Mini)

A lightweight AI-powered SKF product assistant built with **C#/.NET 8, Azure Functions, Microsoft Semantic Kernel, Azure OpenAI, local JSON datasheets, and Redis**.

The application exposes a single HTTP endpoint that supports:

* SKF product Q&A
* Product attribute lookup
* Conversation-aware follow-up questions
* Feedback detection and persistence
* Semantic Kernel function calling
* Remote Redis feedback storage

---

## 1. Technology Stack

| Technology                | Purpose                                |
| ------------------------- | -------------------------------------- |
| C# / .NET 8               | Application development                |
| Azure Functions v4        | HTTP API hosting                       |
| Microsoft Semantic Kernel | AI orchestration and function calling  |
| Azure OpenAI              | LLM-based reasoning and classification |
| System.Text.Json          | JSON parsing                           |
| Redis                     | Feedback persistence                   |
| StackExchange.Redis       | Redis client                           |
| Local JSON                | SKF product datasheets                 |

---

## 2. Project Architecture

The application uses a lightweight multi-agent architecture.

```text
                    HTTP Request
                         |
                         v
              ProductAssistantFunction
                         |
                         v
             ProductAssistantOrchestrator
                    /            \
                   /              \
                  v                v
             QaAgent          FeedbackAgent
                |                  |
                v                  v
       ProductDataPlugin     FeedbackPlugin
                |                  |
                v                  v
       ProductDataService     FeedbackStore
                |                  |
                v                  v
          Local JSON          Remote Redis
          Datasheets
```

### Main components

#### ProductAssistantFunction

The Azure Function exposes the HTTP API endpoint:

```text
POST /api/ProductAssistant
```

It validates the incoming request and forwards it to the orchestrator.

#### ProductAssistantOrchestrator

The orchestrator determines whether the incoming message is:

* A product question
* User feedback

The message is then routed to the appropriate agent.

#### QaAgent

The Q&A Agent:

1. Understands the user's product question.
2. Identifies the product designation.
3. Identifies the requested attribute.
4. Uses the Semantic Kernel product function.
5. Retrieves information from the local SKF datasheets.
6. Returns a concise grounded answer.
7. Maintains relevant conversation context.

#### FeedbackAgent

The Feedback Agent:

1. Analyzes the user's feedback.
2. Identifies the feedback type.
3. Uses previous conversation context.
4. Identifies the related product and attribute.
5. Calls the `save_feedback` Semantic Kernel function.
6. Persists the feedback in Redis.
7. Returns a confirmation response.

#### ProductDataService

Reads the local SKF JSON datasheets and retrieves requested attributes.

Supported product datasheets:

```text
Data/6205.json
Data/6205 N.json
```

The service returns `null` when the requested product or attribute is unavailable.

#### ConversationStateService

Maintains lightweight conversation state in memory.

The state includes:

* Conversation ID
* Last product designation
* Last attribute
* Last answer

This allows follow-up questions such as:

```text
User:
What is the width of SKF 6205?

Assistant:
The width of SKF 6205 is 15 mm.

User:
What about the bore diameter?

Assistant:
The bore diameter is 25 mm.
```

#### FeedbackStore

Stores feedback records in the configured Redis instance.

Redis keys use the following pattern:

```text
feedback:{conversationId}:{uniqueId}
```

Example:

```text
feedback:redis-test-003:b8c25f8c718541afa10d14579ca81a3a
```

---

## 3. Semantic Kernel Function Calling

The application uses Microsoft Semantic Kernel to expose application capabilities as functions.

### Product function

```text
get_product_attribute
```

Purpose:

```text
Retrieve an SKF product attribute from the local datasheet.
```

### Feedback function

```text
save_feedback
```

Purpose:

```text
Persist user feedback in Redis.
```

The agents allow the Azure OpenAI model to select the appropriate Semantic Kernel function through function calling.

---

## 4. Product Datasheets

Product information is stored locally under:

```text
Data/
├── 6205.json
└── 6205 N.json
```

The application does not use the LLM as the source of product facts.

Instead, the LLM identifies the requested information and the Semantic Kernel function retrieves the actual value from the SKF datasheet.

This helps keep product answers grounded in the supplied data.

---

## 5. Configuration

Sensitive configuration is stored outside the source code.

For local Azure Functions development, configuration is stored in:

```text
local.settings.json
```

Required configuration includes:

```text
AZURE_OPENAI_ENDPOINT
AZURE_OPENAI_API_KEY
AZURE_OPENAI_DEPLOYMENT
REDIS_CONNECTION_STRING
```

Example structure:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "AZURE_OPENAI_ENDPOINT": "<Azure OpenAI endpoint>",
    "AZURE_OPENAI_API_KEY": "<Azure OpenAI API key>",
    "AZURE_OPENAI_DEPLOYMENT": "<Azure OpenAI deployment>",
    "REDIS_CONNECTION_STRING": "<Redis connection string>"
  }
}
```

Actual credentials must not be committed to source control.

---

## 6. Running Locally

### Prerequisites

Install:

* .NET 8 SDK
* Azure Functions Core Tools
* Redis access
* Access to the configured Azure OpenAI deployment

Verify .NET:

```bash
dotnet --version
```

Verify Azure Functions Core Tools:

```bash
func --version
```

### Build

From the project directory:

```bash
dotnet build
```

### Start Azure Functions

On the development machine, the application can be started using:

```bash
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH func start
```

The HTTP endpoint will be available at:

```text
http://localhost:7071/api/ProductAssistant
```

---

## 7. API

### Endpoint

```text
POST /api/ProductAssistant
```

### Header

```text
Content-Type: application/json
```

### Request format

```json
{
  "message": "What is the width of SKF 6205?",
  "conversationId": "test-001"
}
```

---

## 8. Example: Product Question

Request:

```json
{
  "message": "What is the width of SKF 6205?",
  "conversationId": "test-001"
}
```

Response:

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

---

## 9. Example: Follow-up Question

First request:

```json
{
  "message": "What is the width of SKF 6205?",
  "conversationId": "test-001"
}
```

Follow-up:

```json
{
  "message": "What about the bore diameter?",
  "conversationId": "test-001"
}
```

The same conversation ID allows the application to use the previously identified product.

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

## 10. Example: Missing Attribute

Request:

```json
{
  "message": "What is the weight of SKF 6205?",
  "conversationId": "test-003"
}
```

If the attribute is not available in the supplied datasheet, the application does not invent a value.

Example response:

```json
{
  "ConversationId": "test-003",
  "Message": "Weight is not available in the SKF datasheet for product 6205.",
  "ProductDesignation": "6205",
  "Attribute": "Weight",
  "FeedbackType": null,
  "Feedback": null
}
```

---

## 11. Example: Feedback

Request:

```json
{
  "message": "The answer was helpful.",
  "conversationId": "redis-test-003"
}
```

Example response:

```json
{
  "ConversationId": "redis-test-003",
  "Message": "Feedback saved successfully.",
  "ProductDesignation": "6205",
  "Attribute": "Width",
  "FeedbackType": "helpful",
  "Feedback": "The answer was helpful."
}
```

The feedback is persisted in Redis.

Example Redis key:

```text
feedback:redis-test-003:<unique-id>
```

---

## 12. Feedback Types

The Feedback Agent recognizes the following categories:

```text
helpful
unhelpful
correction
general
```

For example:

```text
"The answer was helpful."
```

can be classified as:

```text
helpful
```

A correction such as:

```text
"The width answer is incorrect. The correct width is 16 mm."
```

can be classified as:

```text
correction
```

---

## 13. Redis

The application uses `StackExchange.Redis`.

Redis configuration is provided through:

```text
REDIS_CONNECTION_STRING
```

The application does not hardcode the Redis credentials.

The feedback flow is:

```text
FeedbackAgent
      |
      v
FeedbackPlugin
      |
      v
FeedbackStore
      |
      v
StackExchange.Redis
      |
      v
Remote Redis
```

Redis persistence was verified using the remote Redis instance.

Example key:

```text
feedback:redis-test-003:b8c25f8c718541afa10d14579ca81a3a
```

---

## 14. Project Structure

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
├── local.settings.json
├── .gitignore
└── README.md
```

---

## 15. Design Decisions

### Agent separation

Q&A and feedback responsibilities are separated into independent agents so that each agent has a focused responsibility.

### Lightweight orchestration

The orchestrator provides a simple routing layer between the HTTP endpoint and the specialized agents.

### Function calling

Application capabilities such as product lookup and feedback persistence are exposed through Semantic Kernel functions instead of allowing the model to directly access application resources.

### Grounded product answers

Product facts are retrieved from the supplied local datasheets rather than generated from general model knowledge.

### Conversation state

Minimal state is maintained to support contextual follow-up questions without passing the complete conversation history to every component.

### Redis persistence

Feedback is stored in Redis so feedback can persist independently of the in-memory application state.

### Dependency injection

Services and agents are registered through .NET dependency injection to improve separation of concerns and maintainability.

---

## 16. Error Handling

The HTTP function validates required request fields before processing.

Required fields:

```text
message
conversationId
```

The application returns a bad-request response when required information is missing.

Product lookup returns no value when the requested product or attribute is unavailable rather than inventing product information.

---

## 17. Current Validation

The application has been manually validated with:

* Product attribute lookup
* Follow-up questions
* Multiple SKF product designations
* Missing product attributes
* Feedback classification
* Feedback persistence
* Redis key verification
* Remote Redis TLS connectivity
* Azure Function HTTP endpoint
* Semantic Kernel function calling

Example validated product responses include:

```text
SKF 6205 width → 15 mm
SKF 6205 bore diameter → 25 mm
SKF 6205 N outside diameter → 52 mm
```

---

## 18. Limitations

This is a lightweight evaluation implementation.

Current limitations include:

* Conversation state is maintained in application memory.
* Conversation state is lost when the Function process restarts.
* Product datasheets are local JSON files.
* Redis is used for feedback persistence.
* The application currently supports the supplied SKF product datasheets.

These choices keep the implementation small while demonstrating the requested agent, function-calling, state, and persistence patterns.

---

## 19. Git / Secret Management

`local.settings.json` contains local configuration and must not be committed.

The `.gitignore` should include:

```text
local.settings.json
bin/
obj/
.vscode/
```

Never commit:

* Azure OpenAI API keys
* Redis passwords
* Connection strings containing credentials
* Other environment-specific secrets

---

## 20. Summary

SKF Product Assistant demonstrates a lightweight agent-based architecture using:

```text
C# / .NET 8
      +
Azure Functions
      +
Semantic Kernel
      +
Azure OpenAI
      +
Function Calling
      +
Local SKF Datasheets
      +
Conversation State
      +
Remote Redis
```

The implementation provides a single HTTP API for grounded SKF product Q&A and feedback handling while keeping application configuration and credentials outside the source code.
