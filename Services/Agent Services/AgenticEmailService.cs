
using System.Text.Json;
using System.Diagnostics;
using InboxIntelligence.Models.Agent;

namespace InboxIntelligence.Services;

public sealed class AgenticEmailService : IAgenticEmailService
{
    private readonly IGeminiAgentClient _geminiClient;
    private readonly EmailToolExecutor _toolExecutor;
    private readonly ILogger<AgenticEmailService> _logger;
    private readonly GeminiInteractionState _interactionState;
    private readonly bool _debugLogs;
    private readonly string MODEL = "gemini-3.5-flash-lite";

    private const string SystemInstruction =
        "You are Inbox Intelligence, an assistant for the user's email mailbox. " +

        "Only answer questions about the user's emails, senders, recipients, " +
        "conversations, dates, decisions, and information contained in emails. " +
        "If a request is unrelated to email intelligence, politely decline. " +

        "Evaluate whether the user's request can be answered reliably using " +
        "the available tools. If it is too broad, explain the limitation and " +
        "ask a clarifying question or suggest a narrower query. " +

        "Before answering, determine which available tool can retrieve the " +
        "required evidence. Use SearchSimilarEmails to find relevant emails. " +
        "If the user asks whether a person worked on other issues, or asks " +
        "about their broader involvement, you MUST call GetEmailsByContact " +
        "after the initial topic search, even if the initial results appear " +
        "relevant. If the user asks how a discussion developed or how an " +
        "issue was resolved, use GetConversationEmails when a relevant " +
        "conversation ID is available. " +

        "For multi-part questions, verify that the retrieved evidence answers " +
        "each part. After every tool call, reassess what information is still " +
        "missing and use another relevant tool if it can provide distinct " +
        "evidence. Do not make unnecessary or repetitive tool calls. " +

        "Base factual answers only on emails retrieved for the current question. " +
        "Never invent emails, people, dates, decisions, or events. Do not use " +
        "chat history or previous answers as a substitute for email retrieval. " +
        "If evidence is insufficient, clearly state what cannot be established. " +

        "Only use exact conversation IDs and contact identifiers grounded in " +
        "the user's request or retrieved emails. " +

        "Answer clearly and concisely. Identify relevant people, dates, " +
        "decisions, and supporting email evidence when useful. " +

        "Display external message IDs as source citations, for example [MSG-1001].";


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
            _logger.LogInformation("========== INBOX INTELLIGENCE: AGENT TRACE ==========");
            _logger.LogInformation("USER -> AGENT: {UserQuery}", query);
            _logger.LogInformation("AGENT -> GEMINI: Sending query for analysis...");
        }

        var previousInteractionId = _interactionState.LastInteractionId;

        var requestBody = new Dictionary<string, object?>
        {
            ["model"] = MODEL,
            ["input"] = query,
            ["system_instruction"] = SystemInstruction,
            ["tools"] = EmailToolDefinitions.Tools,
            ["store"] = true
        };

        if (previousInteractionId is not null)
        {
            requestBody["previous_interaction_id"] = previousInteractionId;
        }

        var geminiTimer = Stopwatch.StartNew();

        using var response =
            await _geminiClient.CreateInteractionAsync(
                requestBody, cancellationToken);

        geminiTimer.Stop();

        if (_debugLogs)
        {
            _logger.LogInformation(
                "GEMINI -> AGENT: Response received in {ElapsedMs} ms.",
                geminiTimer.ElapsedMilliseconds);
        }

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
                if (currentResult.IsToolCall)
                {
                    _logger.LogInformation(
                        "GEMINI -> AGENT: Tool requested: {ToolName}",
                        currentResult.ToolName);

                    // Log complete arguments generated by Gemini.
                    var toolArgumentsJson = JsonSerializer.Serialize(
                        currentResult.ToolArguments,
                        new JsonSerializerOptions
                        {
                            WriteIndented = true
                        });

                    _logger.LogInformation(
                        "GEMINI -> AGENT: Tool arguments:\n{ToolArguments}",
                        toolArgumentsJson);

                    // Keep the convenient search-query log.
                    var rewrittenQuery = GetSearchQuery(currentResult.ToolArguments);

                    if (!string.IsNullOrWhiteSpace(rewrittenQuery))
                    {
                        _logger.LogInformation(
                            "AGENT SEARCH QUERY: {SearchQuery}",
                            rewrittenQuery);
                    }
                }
                else
                {
                    _logger.LogInformation(
                        "GEMINI -> AGENT: Final answer received. No further tool call requested.");
                }
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
                    _logger.LogInformation("AGENT -> USER: Returning final answer.");
                    _logger.LogInformation("========== AGENT TRACE COMPLETED ==========");
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
                    "AGENT -> TOOL: Executing {ToolName} ({ToolNumber}/{MaxTools})...",
                    currentResult.ToolName,
                    toolCount + 1,
                    maxToolExecutions);
            }

            // Execute the requested allowlisted tool.
            var toolTimer = Stopwatch.StartNew();

            var toolResult = await _toolExecutor.ExecuteAsync(
                currentResult, cancellationToken);

            toolTimer.Stop();

            if (_debugLogs)
            {
                var resultCount = toolResult is System.Collections.ICollection collection
                    ? collection.Count.ToString()
                    : "not available";

                _logger.LogInformation(
                    "TOOL -> AGENT: {ToolName} completed | Results: {ResultCount} | Duration: {ElapsedMs} ms",
                    currentResult.ToolName,
                    resultCount,
                    toolTimer.ElapsedMilliseconds);
            }

            var serializedResult = JsonSerializer.Serialize(toolResult);

            var followUpRequest = new
            {
                model = MODEL,
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

            if (_debugLogs)
            {
                _logger.LogInformation(
                    "AGENT -> GEMINI: Sending tool results for further analysis...");
            }

            var followUpTimer = Stopwatch.StartNew();

            using var followUpResponse =
                await _geminiClient.CreateInteractionAsync(
                    followUpRequest, cancellationToken);

            followUpTimer.Stop();

            if (_debugLogs)
            {
                _logger.LogInformation(
                    "GEMINI -> AGENT: Follow-up response received in {ElapsedMs} ms.",
                    followUpTimer.ElapsedMilliseconds);
            }

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

    private static string? GetSearchQuery(object? toolArguments)
    {
        if (toolArguments is not JsonElement arguments ||
            arguments.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        foreach (var property in arguments.EnumerateObject())
        {
            if (string.Equals(
                property.Name,
                "query",
                StringComparison.OrdinalIgnoreCase))
            {
                return property.Value.ValueKind == JsonValueKind.String
                    ? property.Value.GetString()
                    : property.Value.ToString();
            }
        }

        return null;
    }
}
