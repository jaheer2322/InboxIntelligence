
using System.Text.Json;
using InboxIntelligence.Models.Agent;
using InboxIntelligence.Services;

namespace InboxIntelligence.Services;

public sealed class EmailToolExecutor
{
    private readonly IEmailService _emailService;

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    public EmailToolExecutor(IEmailService emailService)
    {
        _emailService = emailService;
    }

    public async Task<object> ExecuteAsync(
        AgentStepResult step,
        CancellationToken cancellationToken = default)
    {
        if (!step.IsToolCall)
        {
            throw new ArgumentException(
                "The supplied step is not a tool call.",
                nameof(step));
        }

        if (step.ToolArguments is not JsonElement arguments ||
            arguments.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException(
                "Tool arguments must be a JSON object.");
        }

        switch (step.ToolName)
        {
            case "SearchSimilarEmails":
                {
                    var args = Deserialize<SearchSimilarEmailsArguments>(
                        arguments);

                    if (string.IsNullOrWhiteSpace(args.Query))
                        throw new ArgumentException(
                            "A search query is required.");

                    if (args.StartDate.HasValue &&
                        args.EndDate.HasValue &&
                        args.StartDate > args.EndDate)
                    {
                        throw new ArgumentException(
                            "StartDate must not be after EndDate.");
                    }

                    return await _emailService.GetSimilarEmailsAsync(
                        args.Query,
                        args.Contact,
                        args.StartDate,
                        args.EndDate,
                        cancellationToken);
                }

            case "GetConversationEmails":
                {
                    var args = Deserialize<GetConversationEmailsArguments>(
                        arguments);

                    if (string.IsNullOrWhiteSpace(args.ConversationId))
                        throw new ArgumentException(
                            "A conversation ID is required.");

                    ValidateLimit(args.Limit);

                    return await _emailService.GetConversationEmailsAsync(
                        args.ConversationId,
                        args.StartDate,
                        args.EndDate,
                        args.Limit,
                        cancellationToken);
                }

            case "GetEmailsByContact":
                {
                    var args = Deserialize<GetEmailsByContactArguments>(
                        arguments);

                    if (string.IsNullOrWhiteSpace(args.Contact))
                        throw new ArgumentException(
                            "A contact is required.");

                    ValidateLimit(args.Limit);

                    return await _emailService.GetEmailsByContactAsync(
                        args.Contact,
                        args.StartDate,
                        args.EndDate,
                        args.Limit,
                        cancellationToken);
                }

            default:
                throw new ArgumentException(
                    $"Unknown tool: {step.ToolName}");
        }
    }

    private static T Deserialize<T>(JsonElement arguments)
    {
        return arguments.Deserialize<T>(JsonOptions)
            ?? throw new InvalidOperationException(
                "Could not deserialize tool arguments.");
    }

    private static void ValidateLimit(int limit)
    {
        if (limit is < 1 or > 50)
        {
            throw new ArgumentOutOfRangeException(
                nameof(limit),
                "Limit must be between 1 and 50.");
        }
    }
}
