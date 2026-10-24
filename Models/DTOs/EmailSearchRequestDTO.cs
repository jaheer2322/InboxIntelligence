using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;

namespace InboxIntelligence.Models.DTOs
{
    public class EmailSearchRequestDTO
    {
        [Required]
        public string Query { set; get; } = string.Empty;
        public string? Contact { set; get; } = string.Empty;
    }
}
