
using System.Text.Json;
using InboxIntelligence.Models.Agent;

namespace InboxIntelligence.Services;

public sealed class AgenticEmailService : IAgenticEmailService
{
    private readonly IGeminiAgentClient _geminiClient;
    private readonly EmailToolExecutor _toolExecutor;
    private readonly ILogger<AgenticEmailService> _logger;
    private readonly GeminiInteractionState _interactionState;
    private readonly bool _debugLogs;

    private const string SystemInstruction =
        "You are Inbox Intelligence, an assistant for the user's email mailbox. " +

        // Scope restriction
        "Only answer questions about the user's emails, senders, recipients, " +
        "conversations, dates, decisions, and information contained in emails. " +
        "If a request is unrelated to email intelligence, politely decline. " +

        // Broad requests
        "Evaluate whether the user's request can be answered reliably using " +
        "the available tools and their retrieval limits. If a request is too " +
        "broad to answer accurately with the available tools, do not present " +
        "a partial result as a complete answer. Explain the limitation and " +
        "ask a clarifying question or suggest a narrower query. " +

        // Tool selection
        "Before answering an in-scope question, determine which available tool, " +
        "if any, can retrieve the evidence needed to answer it. Use a tool only " +
        "when its purpose matches the user's question. If no available tool can " +
        "retrieve the required evidence, explain the limitation instead of " +
        "guessing or using an unrelated tool. " +

        // Evidence requirements
        "Base factual mailbox answers only on relevant emails retrieved for " +
        "the current question; never use chat history or prior answers as a " +
        "substitute for retrieval. If no relevant evidence is retrieved, say so. " +
        "Do not invent emails, people, dates, decisions, or events. " +
        "Never claim to have searched emails unless a search tool was executed. " +

        // Accuracy and completeness
        "Distinguish between findings from retrieved emails and conclusions " +
        "that cannot be established from the available evidence. " +
        "Do not claim to summarize all emails unless the retrieval results " +
        "actually cover the complete requested set. " +
        "When answering questions about the latest email, respect the tool's " +
        "sorting and date-filter behavior. " +

        // Response style
        "Answer clearly and concisely. When useful, identify the relevant " +
        "people, dates, decisions, and supporting email evidence." +

        // Source citation
        "Display external-messageids as source citation of emails, for example if " +
        "an email with external message id 'MSG-1001' is used to answer the query " +
        "display citation as [MSG-1001]" +

        // Multi-step retrieval
        "For questions involving multiple related facts, decisions, " +
        "people, or changes over time, evaluate whether the current " +
        "retrieved evidence covers every part of the question. If " +
        "important evidence is missing, make another appropriate tool " +
        "call before answering. Use additional calls when they can " +
        "retrieve distinct or more complete evidence, such as the " +
        "surrounding conversation or messages from a relevant contact. " +
        "Do not make unnecessary calls when the available evidence " +
        "already supports a complete answer. ";


    public AgenticEmailService(
        IGeminiAgentClient geminiClient,
        EmailToolExecutor toolExecutor,
        ILogger<AgenticEmailService> logger,
        GeminiInteractionState interactionState,
        IConfiguration configuration)
    {
        _geminiClient = geminiClient;
        _toolExecutor = toolExecutor;
        _logger = logger;
        _interactionState = interactionState;
        _debugLogs = configuration.GetValue<bool>("DebugLogs");
    }


    public async Task<string> AskAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);

        const int maxToolExecutions = 3;

        if (_debugLogs)
        {
            _logger.LogInformation("Agent: Request started.");
        }

        var previousInteractionId = _interactionState.LastInteractionId;

        var requestBody = new Dictionary<string, object?>
        {
            ["model"] = "gemini-3.1-flash-lite",
            ["input"] = query,
            ["system_instruction"] = SystemInstruction,
            ["tools"] = EmailToolDefinitions.Tools,
            ["store"] = true
        };

        if (previousInteractionId is not null)
        {
            requestBody["previous_interaction_id"] = previousInteractionId;
        }

        using var response =
            await _geminiClient.CreateInteractionAsync(
                requestBody, cancellationToken);

        var currentResult = AgentStepParser.Parse(response);

        var currentInteractionId = response.RootElement
            .GetProperty("id")
            .GetString();

        if (string.IsNullOrWhiteSpace(currentInteractionId))
        {
            throw new InvalidOperationException(
                "Gemini response did not contain an interaction ID.");
        }

        for (var toolCount = 0; ; toolCount++)
        {
            if (_debugLogs)
            {
                _logger.LogInformation(
                    "Agent: Gemini response parsed. IsToolCall={IsToolCall}, " +
                    "ToolName={ToolName}, ToolCount={ToolCount}, Arguments = {Arguments}",
                    currentResult.IsToolCall,
                    currentResult.ToolName ?? "None",
                    toolCount,
                    currentResult.ToolArguments);
            }

            // Gemini has finished and returned its answer.
            if (!currentResult.IsToolCall)
            {
                var answer = currentResult.Answer
                    ?? throw new InvalidOperationException(
                        "Gemini returned an empty answer.");

                _interactionState.LastInteractionId = currentInteractionId;

                if (_debugLogs)
                {
                    _logger.LogInformation(
                        "Agent: Request completed successfully.");
                }

                return answer;
            }

            // Stop before executing a fourth tool.
            if (toolCount >= maxToolExecutions)
            {
                throw new InvalidOperationException(
                    $"Gemini exceeded the maximum of {maxToolExecutions} " +
                    "tool executions for this request.");
            }

            if (_debugLogs)
            {
                _logger.LogInformation(
                    "Agent: Executing tool {ToolName} ({ToolNumber}/{MaxTools}).",
                    currentResult.ToolName,
                    toolCount + 1,
                    maxToolExecutions);
            }

            // Execute the requested allowlisted tool.
            var toolResult = await _toolExecutor.ExecuteAsync(
                currentResult, cancellationToken);

            var serializedResult = JsonSerializer.Serialize(toolResult);

            var followUpRequest = new
            {
                model = "gemini-3.1-flash-lite",
                previous_interaction_id = currentInteractionId,
                system_instruction = SystemInstruction,
                tools = EmailToolDefinitions.Tools,
                input = new[]
                {
                new
                {
                    type = "function_result",
                    name = currentResult.ToolName,
                    call_id = currentResult.ToolCallId,
                    result = new[]
                    {
                        new
                        {
                            type = "text",
                            text = serializedResult
                        }
                    }
                }
            }
            };

            using var followUpResponse =
                await _geminiClient.CreateInteractionAsync(
                    followUpRequest, cancellationToken);

            // Advance the interaction chain before processing the next step.
            currentInteractionId = followUpResponse.RootElement
                .GetProperty("id")
                .GetString();

            if (string.IsNullOrWhiteSpace(currentInteractionId))
            {
                throw new InvalidOperationException(
                    "Gemini response did not contain an interaction ID.");
            }

            currentResult = AgentStepParser.Parse(followUpResponse);
        }
    }
}
