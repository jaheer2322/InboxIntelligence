using System.Text.Json;

namespace InboxIntelligence.Models.Agent;

public sealed record AgentStepResult
{
    public string? Answer { get; init; }

    public string? ToolName { get; init; }

    public string? ToolCallId { get; init; }

    public JsonElement? ToolArguments { get; init; }

    public bool IsToolCall => ToolName is not null;
}