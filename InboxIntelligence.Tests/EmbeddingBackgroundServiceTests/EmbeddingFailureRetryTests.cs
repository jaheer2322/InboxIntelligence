using InboxIntelligence.Models;
using InboxIntelligence.Repositories;
using InboxIntelligence.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace InboxIntelligence.Tests.EmbeddingBackgroundServiceTests
{
    [TestClass]
    public class EmbeddingFailureRetryTests
    {
        [TestMethod]
        public async Task ProcessEmailAsync_WhenEmbeddingFails_IncrementsFailureCount()
        {
            // Arrange
            var emailRepoMock = new Mock<IEmailRepository>();
            var embeddingServiceMock = new Mock<IEmbeddingService>();
            var email = new Email
            {
                Id = Guid.NewGuid(),
                Subject = "Test email",
                Body = "Test body",
                EmbeddingFailureCount = 0
            };

            embeddingServiceMock
            .Setup(em => em.GenerateEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Simulated embedding failure"));
            
            emailRepoMock
            .Setup(r => r.IncrementEmbeddingFailureCountAsync(email))
            .Returns(() =>
            {
                email.EmbeddingFailureCount++;
                return Task.CompletedTask;
            });

            var service = new EmbeddingBackgroundService(
                Mock.Of<IServiceScopeFactory>(),
                Mock.Of<ILogger<EmbeddingBackgroundService>>());

            // Act
            await service.ProcessEmailAsync(
                email,
                emailRepoMock.Object,
                embeddingServiceMock.Object,
                CancellationToken.None);

            // Assert
            Assert.AreEqual(1, email.EmbeddingFailureCount);

            emailRepoMock.Verify(
                r => r.IncrementEmbeddingFailureCountAsync(email),
                Times.Once);

            emailRepoMock.Verify(
                r => r.SaveEmbeddingAsync(
                    It.IsAny<Email>(),
                    It.IsAny<float[]>()),
                Times.Never);
        }

        [TestMethod]
        public async Task ProcessEmailAsync_WhenCancelled_DoesNotIncrementFailureCount()
        {
            // Arrange
            var email = new Email
            {
                Id = Guid.NewGuid(),
                Subject = "Test email",
                Body = "Test body",
                EmbeddingFailureCount = 0
            };

            using var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            var repository = new Mock<IEmailRepository>();

            var embeddingService = new Mock<IEmbeddingService>();

            embeddingService
                .Setup(s => s.GenerateEmbeddingAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException(cancellationTokenSource.Token));

            var service = new EmbeddingBackgroundService(
                Mock.Of<IServiceScopeFactory>(),
                Mock.Of<ILogger<EmbeddingBackgroundService>>());

            // Act & Assert
            // Act & Assert
            try
            {
                await service.ProcessEmailAsync(
                    email,
                    repository.Object,
                    embeddingService.Object,
                    cancellationTokenSource.Token);

                Assert.Fail("Expected OperationCanceledException was not thrown.");
            }
            catch (OperationCanceledException)
            {
                // Expected: cancellation should propagate.
            }

            Assert.AreEqual(0, email.EmbeddingFailureCount);

            repository.Verify(
                r => r.IncrementEmbeddingFailureCountAsync(It.IsAny<Email>()),
                Times.Never);
        }
    }
}
