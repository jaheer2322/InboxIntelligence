using System.Text.Json;

namespace InboxIntelligence.Models.Agent;

public static class AgentStepParser
{
    public static AgentStepResult Parse(JsonDocument document)
    {
        var root = document.RootElement;

        if (!root.TryGetProperty("steps", out var steps) ||
            steps.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException(
                "Gemini response does not contain a valid steps array.");
        }

        // Look at the steps in reverse order, so we inspect
        // the latest model output or tool call first.
        foreach (var step in steps.EnumerateArray().Reverse())
        {
            if (!step.TryGetProperty("type", out var type))
                continue;

            switch (type.GetString())
            {
                case "function_call":
                    if (!step.TryGetProperty("name", out var name) ||
                        !step.TryGetProperty("id", out var id) ||
                        !step.TryGetProperty("arguments", out var arguments))
                    {
                        throw new InvalidOperationException(
                            "Gemini returned an incomplete function call.");
                    }

                    return new AgentStepResult
                    {
                        ToolName = name.GetString(),
                        ToolCallId = id.GetString(),
                        ToolArguments = arguments.Clone()
                    };

                case "model_output":
                    if (!step.TryGetProperty("content", out var content) ||
                        content.ValueKind != JsonValueKind.Array)
                    {
                        continue;
                    }

                    var answer = string.Join(
                        "\n",
                        content.EnumerateArray()
                            .Where(part =>
                                part.TryGetProperty("type", out var partType) &&
                                partType.GetString() == "text")
                            .Select(part =>
                                part.TryGetProperty("text", out var text)
                                    ? text.GetString()
                                    : null)
                            .Where(text =>
                                !string.IsNullOrWhiteSpace(text)));

                    if (!string.IsNullOrWhiteSpace(answer))
                    {
                        return new AgentStepResult
                        {
                            Answer = answer
                        };
                    }

                    break;
            }
        }

        throw new InvalidOperationException(
            "Gemini returned neither a usable function call nor a text answer.");
    }
}