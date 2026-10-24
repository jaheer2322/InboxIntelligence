namespace InboxIntelligence.Models
{
    public class ContactEmail
    {
        public string EmailAddress { get; set; } = string.Empty;

        public Guid EmailId { get; set; }

        public Email Email { get; set; } = null!;
    }
}
