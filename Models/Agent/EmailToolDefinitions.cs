
namespace InboxIntelligence.Models.Agent;

public static class EmailToolDefinitions
{
    public static readonly object[] Tools =
    [
        new
        {
            type = "function",
            name = "SearchSimilarEmails",
            description =
                "Search emails by semantic relevance to a query. " +
                "Optionally filter by contact and date range. " +
                "Use for questions about topics, projects, decisions, or events.",
            parameters = new
            {
                type = "object",
                properties = new
                {
                    query = new
                    {
                        type = "string",
                        description = "The topic or question to search for."
                    },
                    contact = new
                    {
                        type = "string",
                        description = "Optional contact name or email address."
                    },
                    startDate = new
                    {
                        type = "string",
                        format = "date-time",
                        description = "Optional inclusive start date in ISO 8601 format."
                    },
                    endDate = new
                    {
                        type = "string",
                        format = "date-time",
                        description = "Optional inclusive end date in ISO 8601 format."
                    }
                },
                required = new[] { "query" }
            }
        },
        new
        {
            type = "function",
            name = "GetConversationEmails",
            description =
                "Retrieve emails from a specific conversation. " +
                "Use a conversation ID obtained from previous search results." +
                "Results are ordered by SentAt descending (newest first). " +
                "When limit is 1, the result is the most recent email from that contact.",
            parameters = new
            {
                type = "object",
                properties = new
                {
                    conversationId = new
                    {
                        type = "string",
                        description = "The exact conversation ID."
                    },
                    startDate = new
                    {
                        type = "string",
                        format = "date-time",
                        description = "Optional inclusive start date in ISO 8601 format."
                    },
                    endDate = new
                    {
                        type = "string",
                        format = "date-time",
                        description = "Optional inclusive end date in ISO 8601 format."
                    },
                    limit = new
                    {
                        type = "integer",
                        description = "Maximum number of emails to retrieve, from 1 to 50.",
                        minimum = 1,
                        maximum = 50
                    }
                },
                required = new[] { "conversationId" }
            }
        },
        new
        {
            type = "function",
            name = "GetEmailsByContact",
            description =
                "Retrieve emails associated with a specific contact. " +
                "Use when the user asks about emails involving a person or email address." +
                "Results are ordered by SentAt descending (newest first). " +
                "When limit is 1, the result is the most recent email from that contact.",
            parameters = new
            {
                type = "object",
                properties = new
                {
                    contact = new
                    {
                        type = "string",
                        description = "The contact name or email address."
                    },
                    startDate = new
                    {
                        type = "string",
                        format = "date-time",
                        description = "Optional inclusive start date in ISO 8601 format."
                    },
                    endDate = new
                    {
                        type = "string",
                        format = "date-time",
                        description = "Optional inclusive end date in ISO 8601 format."
                    },
                    limit = new
                    {
                        type = "integer",
                        description = "Maximum number of emails to retrieve, from 1 to 50.",
                        minimum = 1,
                        maximum = 50
                    }
                },
                required = new[] { "contact" }
            }
        }
    ];
}
