namespace InboxIntelligence.Models
{
    public class Email
    {
        public Guid Id { get; set; }
        public string ExternalMessageId { get; set; } = string.Empty;
        public string ConversationId { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Sender { get; set; } = string.Empty;
        public string Recipients { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
    }
}
