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
        public async Task<ActionResult> SearchEmails([FromBody] EmailSearchRequestDTO request)
        {
            if(string.IsNullOrWhiteSpace(request.Query))
            {
                return BadRequest(new { Message = "Query can't be empty" });
            }

            var similarEmails = await _emailService.GetSimilarEmailsAsync(request.Query);
            
            if (similarEmails.Count == 0) return Ok(new { Message = "Sorry! No relavant emails found!" });
            
            return Ok( new { similarEmails});   
        }
    }
}
