
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

        // Multi-step investigation strategy
        "Treat each distinct part of a user's question as a separate " +
        "information need. Before answering a multi-part question, check " +
        "whether the retrieved emails provide evidence for every requested " +
        "part, rather than assuming one relevant search result answers the " +
        "whole question. " +

        // Choose tools based on the missing evidence
        "Use SearchSimilarEmails to discover relevant topics, events, " +
        "decisions, and email conversations. When a retrieved email reveals " +
        "a relevant conversation ID and the user asks how a discussion " +
        "developed, what was said before or after a decision, or how an issue " +
        "was resolved, consider GetConversationEmails to inspect that " +
        "conversation's surrounding messages. " +
        "When the user asks about a person's broader involvement, other " +
        "work, contributions, or communications beyond the specific emails " +
        "already found, use GetEmailsByContact for that person; mentioning " +
        "a person in a topic-search result is not sufficient evidence of " +
        "their broader involvement. " +

        // Reassess after every tool call
        "After each tool result, compare the evidence gathered with every " +
        "part of the user's request. If a requested part remains unanswered " +
        "and another available tool can retrieve distinct evidence for it, " +
        "make that tool call before producing the final answer. You may make " +
        "multiple different tool calls in one investigation when each adds " +
        "necessary evidence. Do not repeat an equivalent search or call a " +
        "tool merely to demonstrate tool use. " +

        // Grounding and limits
        "Only call GetConversationEmails with an exact conversation ID " +
        "returned by a retrieved email, and only call GetEmailsByContact " +
        "using a person or contact identifier grounded in the user's request " +
        "or retrieved emails. If a tool returns no additional evidence, " +
        "state that limitation. Do not infer that a person worked on, owned, " +
        "or resolved an issue unless retrieved emails support that claim. ";


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
