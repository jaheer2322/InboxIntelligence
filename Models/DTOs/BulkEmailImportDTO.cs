namespace InboxIntelligence.Models.DTOs
{
    public class BulkEmailImportDTO
    {
        public string MessageId { get; set; } = string.Empty;
        public string ConversationId { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Sender { get; set; } = string.Empty;
        public List<string> Recipients { get; set; } = [];
        public string Body { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }

    }
}
