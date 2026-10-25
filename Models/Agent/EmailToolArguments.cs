
namespace InboxIntelligence.Models.Agent;

public sealed class SearchSimilarEmailsArguments
{
    public string Query { get; set; } = string.Empty;

    public string? Contact { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }
}

public sealed class GetConversationEmailsArguments
{
    public string ConversationId { get; set; } = string.Empty;

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public int Limit { get; set; } = 20;
}

public sealed class GetEmailsByContactArguments
{
    public string Contact { get; set; } = string.Empty;

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public int Limit { get; set; } = 20;
}
