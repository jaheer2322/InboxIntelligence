
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
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
            "Embedding background service started.");

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

                    _logger.LogInformation(
                        "Found {PendingCount} emails pending embeddings.",
                        pendingEmails.Count);

                    foreach (var email in pendingEmails)
                    {
                        stoppingToken.ThrowIfCancellationRequested();

                        try
                        {
                            var text = $"Subject: {email.Subject}\n\n{email.Body}";

                            var embedding =
                                await embeddingService.GenerateEmbeddingAsync(
                                    text,
                                    stoppingToken);

                            await repository.SaveEmbeddingAsync(
                                email,
                                embedding);

                            _logger.LogInformation(
                                "Embedding generated for email {EmailId}.",
                                email.Id);
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
                                "Failed to generate embedding for email {EmailId}, failure count: {count}.",
                                email.Id, email.EmbeddingFailureCount);

                            if (email.EmbeddingFailureCount >= 3)
                            {
                                _logger.LogWarning(
                                    "Email {EmailId} reached the maximum embedding failure count and will be skipped automatically.",
                                    email.Id);
                            }
                            else
                            {
                                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                            }
                        }

                        // Space out consecutive API calls.
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

            _logger.LogInformation(
                "Embedding background service stopped.");
        }
    }
}
