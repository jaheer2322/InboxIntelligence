
using InboxIntelligence.Models;
using InboxIntelligence.Repositories;
using Microsoft.Extensions.Logging;

namespace InboxIntelligence.Services
{
    public class EmbeddingBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<EmbeddingBackgroundService> _logger;
        private const int BatchSize = 5;
        private static readonly TimeSpan ApiDelay =
            TimeSpan.FromSeconds(2);

        public EmbeddingBackgroundService(IServiceScopeFactory scopeFactory, ILogger<EmbeddingBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }
        public async Task ProcessEmailAsync(
            Email email,
            IEmailRepository repository,
            IEmbeddingService embeddingService,
            CancellationToken stoppingToken)
        {
            try
            {
                var text = $"Subject: {email.Subject}\n\n{email.Body}";

                var embedding = await embeddingService.GenerateEmbeddingAsync(
                    text,
                    stoppingToken);

                await repository.SaveEmbeddingAsync(email, embedding);

            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                await repository.IncrementEmbeddingFailureCountAsync(email);

                _logger.LogError(
                    ex,
                    "Failed to generate embedding for email {EmailId}, failure count: {Count}.",
                    email.Id,
                    email.EmbeddingFailureCount);
            }
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();

                    var repository = scope.ServiceProvider
                        .GetRequiredService<IEmailRepository>();

                    var embeddingService = scope.ServiceProvider
                        .GetRequiredService<IEmbeddingService>();

                    var pendingEmails =
                        await repository.GetEmailsPendingEmbeddingAsync(
                            BatchSize);

                    foreach (var email in pendingEmails)
                    {
                        stoppingToken.ThrowIfCancellationRequested();

                        var previousFailureCount = email.EmbeddingFailureCount;

                        await ProcessEmailAsync(
                            email,
                            repository,
                            embeddingService,
                            stoppingToken);

                        if (email.EmbeddingFailureCount > previousFailureCount &&
                            email.EmbeddingFailureCount < 3)
                        {
                            await Task.Delay(
                                TimeSpan.FromSeconds(10),
                                stoppingToken);
                        }

                        await Task.Delay(ApiDelay, stoppingToken);
                    }

                    if (pendingEmails.Count == 0)
                    {
                        await Task.Delay(
                            TimeSpan.FromSeconds(5),
                            stoppingToken);
                    }
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Embedding worker encountered an unexpected error.");

                    await Task.Delay(
                        TimeSpan.FromSeconds(10),
                        stoppingToken);
                }
            }

        }
    }
}
