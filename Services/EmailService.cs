using AutoMapper;
using InboxIntelligence.Models;
using InboxIntelligence.Models.DTOs;
using InboxIntelligence.Repositories;

namespace InboxIntelligence.Services
{
    public class EmailService : IEmailService
    {
        private readonly IEmailRepository _emailRepo;
        private readonly IMapper _mapper;

        public EmailService(IEmailRepository emailRepo, IMapper mapper) { 
            _emailRepo = emailRepo;
            _mapper = mapper;
        }
        public async Task<int> ImportEmailsAsync(IEnumerable<BulkEmailImportDTO> emails)
        {
            var emailDTOs = emails.ToList();

            if (emailDTOs.Count == 0) return 0;

            var incomingIds = emailDTOs.Select(e => e.ExternalMessageId).ToList();
            var existingIds = await _emailRepo.GetExistingMessageIdsAsync(incomingIds);
            var newEmailDTOs = emails.Select(e => !existingIds.Contains(e.ExternalMessageId)).ToList();

            if(newEmailDTOs.Count == 0) return 0;

            var newEmails = _mapper.Map<List<Email>>(newEmailDTOs);

            await _emailRepo.AddRangeAsync(newEmails);

            return newEmails.Count;
        }
    }
}
