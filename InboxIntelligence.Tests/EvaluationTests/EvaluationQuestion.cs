namespace InboxIntelligence.Tests.EvaluationTests;

public class EvaluationQuestion
{
    public string Id { get; set; } = string.Empty;

    public string Question { get; set; } = string.Empty;

    public List<string> SupportingMessageIds { get; set; } = [];
}