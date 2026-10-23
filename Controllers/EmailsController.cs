using InboxIntelligence.Models.DTOs;
using InboxIntelligence.Services;
using Microsoft.AspNetCore.Mvc;

namespace InboxIntelligence.Controllers
{
    [ApiController]
    [Route("api/[controller]/")]
    public class EmailsController : ControllerBase
    {
        private readonly IEmailService _emailService;
        public EmailsController(IEmailService emailService)
        {
            _emailService = emailService;
        }

        [HttpPost("bulk-import")]
        public async Task<ActionResult> BulkImportEmails([FromBody] List<BulkEmailImportDTO> emailDTOs)
        {
            var importedCount = await _emailService.ImportEmailsAsync(emailDTOs);
            return Ok(new { message = "Import successfull", importedCount });
        }

        [HttpPost("search")]
        public async Task<ActionResult> SearchEmails(
            [FromBody] EmailSearchRequestDTO request,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            if(string.IsNullOrWhiteSpace(request.Query))
            {
                return BadRequest(new { Message = "Query can't be empty" });
            }

            var similarEmails = await _emailService.GetSimilarEmailsAsync(request.Query, startDate, endDate);
            
            if (similarEmails.Count == 0) return Ok(new { Message = "Sorry! No relavant emails found!" });
            
            return Ok( new { similarEmails});   
        }

        [HttpGet("conversations/{conversationId}")]
        public async Task<IActionResult> GetConversationEmails(
            string conversationId,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] int limit = 50)
        {
            if (startDate.HasValue && endDate.HasValue &&
                startDate.Value > endDate.Value)
            {
                return BadRequest("startDate must be earlier than or equal to endDate.");
            }

            if (limit < 1 || limit > 100)
                return BadRequest("limit must be between 1 and 100.");

            var emails = await _emailService.GetConversationEmailsAsync(
                conversationId,
                startDate,
                endDate,
                limit);

            return Ok(emails);
        }
    }
}
