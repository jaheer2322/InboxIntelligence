
using System.Text.Json;
using InboxIntelligence.Models.Agent;

namespace InboxIntelligence.Services;

public sealed class AgenticEmailService : IAgenticEmailService
{
    private readonly IGeminiAgentClient _geminiClient;
    private readonly EmailToolExecutor _toolExecutor;
    private readonly ILogger<AgenticEmailService> _logger;
    private readonly bool _debugLogs;

    private const string SystemInstruction =
        "You are Inbox Intelligence, an assistant for the user's email mailbox. " +
    "Only answer questions related to the user's emails, senders, " +
    "conversations, dates, and information contained in emails. " +
    "If a request is unrelated to email intelligence, politely decline " +
    "and explain that you can only help with mailbox-related questions. " +
    "Do not answer out-of-scope questions, even if you know the answer. " +
    "For in-scope questions, use the available tools to retrieve relevant " +
    "email evidence before answering. " +
    "Base factual answers about the mailbox only on retrieved evidence. " +
    "If the retrieved emails don't contain sufficient evidence, say so. " +
    "Do not claim to have searched emails unless a search tool was executed.";

    public AgenticEmailService(
        IGeminiAgentClient geminiClient,
        EmailToolExecutor toolExecutor,
        ILogger<AgenticEmailService> logger,
        IConfiguration configuration)
    {
        _geminiClient = geminiClient;
        _toolExecutor = toolExecutor;
        _logger = logger;
        _debugLogs = configuration.GetValue<bool>("DebugLogs");
    }

    public async Task<string> AskAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);

        if (_debugLogs)
        {
            _logger.LogInformation(
                "Agent: Request started.");
        }

        var requestBody = new
        {
            model = "gemini-3.1-flash-lite",
            input = query,
            system_instruction = SystemInstruction,
            tools = EmailToolDefinitions.Tools,
            store = true
        };

        if (_debugLogs)
        {
            _logger.LogInformation(
                "Agent: Sending query to Gemini.");
        }

        using var firstResponse =
            await _geminiClient.CreateInteractionAsync(
                requestBody, cancellationToken);

        var firstResult = AgentStepParser.Parse(firstResponse);

        if (_debugLogs)
        {
            _logger.LogInformation(
                "Agent: Gemini response parsed. IsToolCall={IsToolCall}, ToolName={ToolName}",
                firstResult.IsToolCall,
                firstResult.ToolName ?? "None");
        }

        // Gemini answered without requesting a tool.
        if (!firstResult.IsToolCall)
        {
            var ans = firstResult.Answer
                ?? throw new InvalidOperationException(
                    "Gemini returned an empty answer.");

            if (_debugLogs)
            {
                _logger.LogInformation(
                    "Agent: Request completed successfully without tool execution.");
            }

            return ans;
        }

        var interactionId = firstResponse.RootElement
            .GetProperty("id").GetString();

        if (string.IsNullOrWhiteSpace(interactionId))
        {
            throw new InvalidOperationException(
                "Gemini response did not contain an interaction ID.");
        }

        if (_debugLogs)
        {
            _logger.LogInformation(
                "Agent: Executing tool {ToolName}",
                firstResult.ToolName);
        }

        // Execute the requested, allowlisted tool in our backend.
        var toolResult = await _toolExecutor.ExecuteAsync(
            firstResult, cancellationToken);

        if (_debugLogs)
        {
            _logger.LogInformation(
                "Agent: Tool {ToolName} executed successfully.",
                firstResult.ToolName);
        }

        var serializedResult = JsonSerializer.Serialize(toolResult);

        if (_debugLogs)
        {
            _logger.LogInformation(
                "Agent: Sending tool result back to Gemini.");
        }

        // Return the tool result to Gemini, linked to the original call.
        var followUpRequest = new
        {
            model = "gemini-3.1-flash-lite",
            previous_interaction_id = interactionId,
            system_instruction = SystemInstruction,
            tools = EmailToolDefinitions.Tools,
            input = new[]
            {
                new
                {
                    type = "function_result",
                    name = firstResult.ToolName,
                    call_id = firstResult.ToolCallId,
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
                "Agent: Sending tool result back to Gemini.");
        }

        using var followUpResponse =
            await _geminiClient.CreateInteractionAsync(
                followUpRequest, cancellationToken);

        var finalResult = AgentStepParser.Parse(followUpResponse);

        if (_debugLogs)
        {
            _logger.LogInformation(
                "Agent: Follow-up response parsed. IsToolCall={IsToolCall}, ToolName={ToolName}",
                finalResult.IsToolCall,
                finalResult.ToolName ?? "None");
        }

        if (finalResult.IsToolCall)
        {
            throw new NotImplementedException(
                "Gemini requested another tool. Multi-round orchestration " +
                "will be implemented in the next step.");
        }

        var answer = finalResult.Answer
            ?? throw new InvalidOperationException(
                "Gemini returned an empty answer.");

        if (_debugLogs)
        {
            _logger.LogInformation(
                "Agent: Request completed successfully.");
        }

        return answer;
    }
}
