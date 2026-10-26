
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
                "Search the mailbox for emails relevant to a topic, question, " +
                "project, decision, event, or other subject. Uses both semantic " +
                "similarity and keyword matching to find relevant emails. " +
                "Optionally filter by contact and sent-date range. " +
                "Returns up to 5 emails ranked by combined relevance, not by " +
                "recency. Use this tool when the question requires finding " +
                "evidence about a subject. Do not assume the first result is " +
                "the newest email. For the latest emails from a person, use " +
                "GetEmailsByContact; for messages in a known conversation, " +
                "use GetConversationEmails.",
            parameters = new
            {
                type = "object",
                properties = new
                {
                    query = new
                    {
                        type = "string",
                        description =
                            "The subject, topic, event, decision, or question " +
                            "to find evidence about in email content."
                    },
                    contact = new
                    {
                        type = "string",
                        description =
                            "Optional sender or recipient name/email to filter " +
                            "results. Use a contact identifier grounded in the " +
                            "user's question or conversation context."
                    },
                    startDate = new
                    {
                        type = "string",
                        format = "date-time",
                        description =
                            "Optional inclusive lower bound for the email " +
                            "SentAt timestamp, in ISO 8601 date-time format."
                    },
                    endDate = new
                    {
                        type = "string",
                        format = "date-time",
                        description =
                            "Optional inclusive upper bound for the email " +
                            "SentAt timestamp, in ISO 8601 date-time format."
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
                "Retrieve emails belonging to a specific conversation. Use " +
                "the exact conversationId obtained from previously retrieved " +
                "email results; do not invent or infer a conversation ID. " +
                "Results are ordered by SentAt descending (newest first), " +
                "and the limit is applied after sorting. When limit is 1, " +
                "returns the most recent matching email in that conversation " +
                "within the specified date range. Use this tool to inspect a " +
                "conversation's messages, timeline, replies, or latest update. " +
                "Results are newest first; reorder them chronologically when " +
                "explaining how a discussion developed over time.",
            parameters = new
            {
                type = "object",
                properties = new
                {
                    conversationId = new
                    {
                        type = "string",
                        description =
                            "The exact conversation ID from a previously " +
                            "retrieved email. Required; never fabricate one."
                    },
                    startDate = new
                    {
                        type = "string",
                        format = "date-time",
                        description =
                            "Optional inclusive lower bound for the email " +
                            "SentAt timestamp, in ISO 8601 date-time format."
                    },
                    endDate = new
                    {
                        type = "string",
                        format = "date-time",
                        description =
                            "Optional inclusive upper bound for the email " +
                            "SentAt timestamp, in ISO 8601 date-time format."
                    },
                    limit = new
                    {
                        type = "integer",
                        description =
                            "Maximum number of emails to return, from 1 to 50. " +
                            "Results are newest first; use 1 for the latest " +
                            "matching email.",
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
                "Retrieve emails associated with a specific contact, whether " +
                "the contact is the sender or a recipient. Use this tool for " +
                "questions about emails involving a person. Results are " +
                "ordered by SentAt descending (newest first), and the limit " +
                "is applied after sorting. When limit is 1, returns the most " +
                "recent matching email within the specified date range. Use " +
                "this tool for latest-email questions; use SearchSimilarEmails " +
                "when you need to find emails by topic or meaning.",
            parameters = new
            {
                type = "object",
                properties = new
                {
                    contact = new
                    {
                        type = "string",
                        description =
                            "A sender or recipient name or email address to " +
                            "match against contact email addresses associated " +
                            "with emails. Use the most specific identifier " +
                            "available from the user's question or context."
                    },
                    startDate = new
                    {
                        type = "string",
                        format = "date-time",
                        description =
                            "Optional inclusive lower bound for the email " +
                            "SentAt timestamp, in ISO 8601 date-time format."
                    },
                    endDate = new
                    {
                        type = "string",
                        format = "date-time",
                        description =
                            "Optional inclusive upper bound for the email " +
                            "SentAt timestamp, in ISO 8601 date-time format."
                    },
                    limit = new
                    {
                        type = "integer",
                        description =
                            "Maximum number of matching emails to return, " +
                            "from 1 to 50. Results are newest first; use 1 " +
                            "when only the latest matching email is needed.",
                        minimum = 1,
                        maximum = 50
                    }
                },
                required = new[] { "contact" }
            }
        }
    ];
}