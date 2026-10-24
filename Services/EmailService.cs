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
        private readonly IGeminiAnswerService _answerService;
        private readonly IMapper _mapper;
        private readonly ILogger<EmailService> _logger;
        private const int DefaultTopK = 5;

        public EmailService(IEmailRepository emailRepo,
                            IEmbeddingService embeddingService,
                            IGeminiAnswerService gAnswerService,
                            IMapper mapper, 
                            ILogger<EmailService> logger) { 
            _emailRepo = emailRepo;
            _embeddingService = embeddingService;
            _answerService = gAnswerService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<string> AskAsync(
                                    string query,
                                    string? contact,
                                    DateTime? startDate,
                                    DateTime? endDate,
                                    CancellationToken cancellationToken = default)
        {
            if (startDate.HasValue)
            {
                startDate = DateTime.SpecifyKind(
                    startDate.Value, DateTimeKind.Utc);
            }

            if (endDate.HasValue)
            {
                endDate = DateTime.SpecifyKind(
                    endDate.Value, DateTimeKind.Utc);
            }

            var emails = await GetSimilarEmailsAsync(query,
                                                     contact,
                                                     startDate,
                                                     endDate);

            if (emails.Count == 0)
                return "I couldn't find relevant emails to answer this question.";

            //_logger.LogInformation(
            //    "For query {0},\n retrieved emails are {1}", 
            //    query, 
            //    emails.Select(e => $"MessageId: {e.Id}, Subject: {e.Subject}\n").ToList());
            
            var emailContext = string.Join(
                "\n\n--- EMAIL ---\n\n",
                emails.Select(e => $"""
                    ExternalMessageId: {e.ExternalMessageId}
                    ConversationId: {e.ConversationId}
                    Subject: {e.Subject}
                    Sender: {e.Sender}
                    SentAt: {e.SentAt:O}
                    Body:
                    {e.Body}
                    """));

            return await _answerService.GenerateAnswerAsync(
                query, emailContext, cancellationToken);
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

            var contactMappings = new List<ContactEmail>();

            for (var i = 0; i < newEmails.Count; i++)
            {
                var email = newEmails[i];
                var dto = newEmailDTOs[i];

                var addresses = dto.Recipients
                    .Append(dto.Sender)
                    .Where(address => !string.IsNullOrWhiteSpace(address))
                    .Select(address => address.Trim().ToLowerInvariant())
                    .Distinct(StringComparer.Ordinal);

                foreach (var address in addresses)
                {
                    contactMappings.Add(new ContactEmail
                    {
                        EmailAddress = address,
                        EmailId = email.Id,
                        Email = email
                    });
                }
            }

            await _emailRepo.AddRangeAsync(newEmails, contactMappings);

            _logger.LogInformation(
                "Email import completed successfully. " +
                "Imported {ImportedCount} emails.",
                newEmails.Count);

            return newEmails.Count;
        }
        public async Task<List<EmailResponseDTO>> GetSimilarEmailsAsync(
                                                    string query, 
                                                    string? contact,
                                                    DateTime? startDate, 
                                                    DateTime? endDate)
        {
            const int topK = 5;
            const int rrfK = 60;

            var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(query);

            if (startDate.HasValue)
            {
                startDate = DateTime.SpecifyKind(
                    startDate.Value, DateTimeKind.Utc);
            }

            if (endDate.HasValue)
            {
                endDate = DateTime.SpecifyKind(
                    endDate.Value, DateTimeKind.Utc);
            }

            var semanticResults = await _emailRepo.GetSimilarEmailsAsync(
                queryEmbedding,
                contact,
                startDate, endDate,
                topK);

            var keywordResults = await _emailRepo.GetEmailsByKeywordAsync(
                query,
                contact,
                startDate, endDate,
                topK);

            var rrfScores = new Dictionary<Guid, double>();

            void AddScores(List<Email> results)
            {
                for (var i = 0; i < results.Count; i++)
                {
                    var email = results[i];
                    var rank = i + 1;

                    rrfScores[email.Id] =
                        rrfScores.GetValueOrDefault(email.Id) +
                        1.0 / (rrfK + rank);
                }
            }

            AddScores(semanticResults);
            AddScores(keywordResults);

            var emailsById = semanticResults
                .Concat(keywordResults)
                .DistinctBy(email => email.Id)
                .ToDictionary(email => email.Id);

            var rankedEmails = rrfScores
                .OrderByDescending(entry => entry.Value)
                .Select(entry => emailsById[entry.Key])
                .ToList();

            _logger.LogInformation(
                "Semantic results: {EmailIds}",
                string.Join(", ", semanticResults.Select(e => e.ExternalMessageId)));

            _logger.LogInformation(
                "Keyword results: {EmailIds}",
                string.Join(", ", keywordResults.Select(e => e.ExternalMessageId)));

            foreach (var entry in rrfScores.OrderByDescending(e => e.Value))
            {
                _logger.LogInformation(
                    "Email {EmailId} received RRF score {RrfScore}",
                    entry.Key,
                    entry.Value);
            }

            return _mapper.Map<List<EmailResponseDTO>>(rankedEmails);
        }

        public async Task<List<EmailResponseDTO>> GetConversationEmailsAsync(string conversationId, 
                                                                             DateTime? startDate, 
                                                                             DateTime? endDate,
                                                                             int limit)
        {
            if (startDate.HasValue)
            {
                startDate = DateTime.SpecifyKind(
                    startDate.Value, DateTimeKind.Utc);
            }

            if (endDate.HasValue)
            {
                endDate = DateTime.SpecifyKind(
                    endDate.Value, DateTimeKind.Utc);
            }
            var emails = await _emailRepo.GetConversationEmailsAsync(conversationId, startDate, endDate, limit);
            return _mapper.Map<List<EmailResponseDTO>>(emails);
        }

        public async Task<List<EmailResponseDTO>> GetEmailsByContactAsync(string contact, DateTime? startDate, DateTime? endDate, int limit = 50)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(contact);

            if (startDate.HasValue &&
                endDate.HasValue &&
                startDate.Value > endDate.Value)
            {
                throw new ArgumentException(
                    "startDate must be earlier than or equal to endDate.");
            }

            if (limit < 1 || limit > 100)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(limit),
                    "Limit must be between 1 and 100.");
            }

            if (startDate.HasValue)
            {
                startDate = DateTime.SpecifyKind(
                    startDate.Value, DateTimeKind.Utc);
            }

            if (endDate.HasValue)
            {
                endDate = DateTime.SpecifyKind(
                    endDate.Value, DateTimeKind.Utc);
            }

            var emails = await _emailRepo.GetEmailsByContactAsync(contact, startDate, endDate, limit);
            return _mapper.Map<List<EmailResponseDTO>>(emails);
        }
    }
}
