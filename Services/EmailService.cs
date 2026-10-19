using AutoMapper;
using InboxIntelligence.Exceptions;
using InboxIntelligence.Models;
using InboxIntelligence.Models.DTOs;
using InboxIntelligence.Repositories;

namespace InboxIntelligence.Services
{
    public class EmailService : IEmailService
    {
        private readonly IEmailRepository _emailRepo;
        private readonly IEmbeddingService _embeddingService;
        private readonly IMapper _mapper;
        private readonly ILogger<EmailService> _logger;
        private const int DefaultTopK = 5;

        public EmailService(IEmailRepository emailRepo,
                            IEmbeddingService embeddingService,
                            IMapper mapper, 
                            ILogger<EmailService> logger) { 
            _emailRepo = emailRepo;
            _embeddingService = embeddingService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<int> ImportEmailsAsync(IEnumerable<BulkEmailImportDTO> emails)
        {
            ArgumentNullException.ThrowIfNull(emails);
            
            var emailDTOs = emails.ToList();
            
            _logger.LogInformation(
                "Email import started. Received {ReceivedCount} emails.",
                emailDTOs.Count);

            if (emailDTOs.Count == 0)
            {
                _logger.LogInformation(
                    "Email import completed. Batch was empty.");
                return 0;
            }
            
            if(emailDTOs.Any(e => string.IsNullOrWhiteSpace(e.MessageId)))
            {
                _logger.LogWarning(
                   "Email import rejected because an email has no external message ID.");
                throw new BadRequestException("Every email must have an external message id");
            }

            var incomingIds = emailDTOs.Select(e => e.MessageId)
                                       .Distinct() // Avoids duplicate in same batch
                                       .ToList();

            var existingIds = await _emailRepo.GetExistingMessageIdsAsync(incomingIds);
            var newEmailDTOs = emailDTOs.DistinctBy(e => e.MessageId)
                                        .Where(e => !existingIds.Contains(e.MessageId))
                                        .ToList();

            var skippedCount = emailDTOs.Count - newEmailDTOs.Count;

            _logger.LogInformation(
                "Email import deduplication completed. " +
                "Received {ReceivedCount}, unique IDs {UniqueIdCount}, " +
                "skipped {SkippedCount}.",
                emailDTOs.Count,
                incomingIds.Count,
                skippedCount);

            if (newEmailDTOs.Count == 0) return 0;

            var newEmails = _mapper.Map<List<Email>>(newEmailDTOs);

            await _emailRepo.AddRangeAsync(newEmails);

            _logger.LogInformation(
                "Email import completed successfully. " +
                "Imported {ImportedCount} emails.",
                newEmails.Count);

            return newEmails.Count;
        }
        public async Task<List<EmailResponseDTO>> GetSimilarEmailsAsync(string query)
        {
            var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(query);
            var emails = await _emailRepo.GetSimilarEmailsAsync(queryEmbedding, DefaultTopK);
            return _mapper.Map<List<EmailResponseDTO>>(emails);
        }
    }
}
