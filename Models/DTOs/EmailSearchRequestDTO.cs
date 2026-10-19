using System.ComponentModel.DataAnnotations;

namespace InboxIntelligence.Models.DTOs
{
    public class EmailSearchRequestDTO
    {
        [Required]
        public string Query { set; get; } = string.Empty;
    }
}
