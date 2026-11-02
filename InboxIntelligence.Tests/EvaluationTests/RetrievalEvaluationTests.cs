using AutoMapper;
using InboxIntelligence.Data;
using InboxIntelligence.Mappings;
using InboxIntelligence.Repositories;
using InboxIntelligence.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace InboxIntelligence.Tests.EvaluationTests;

[TestClass]
public class RetrievalEvaluationTests
{
    [TestMethod]
    public async Task EvaluateRetrieval_GoldenDataset_ReportsMetrics()
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<RetrievalEvaluationTests>()
            .Build();

        var connectionString =
            configuration.GetConnectionString("InboxIntelligence");

        var geminiApiKey = configuration["Gemini:ApiKey"];

        if (string.IsNullOrWhiteSpace(connectionString) ||
            string.IsNullOrWhiteSpace(geminiApiKey))
        {
            Assert.Inconclusive(
                "Set ConnectionStrings__InboxIntelligence and Gemini__ApiKey " +
                "environment variables to run retrieval evaluation.");
        }


        var options = new DbContextOptionsBuilder<InboxIntelligenceDbContext>()
        .UseNpgsql(
            connectionString,
            npgsqlOptions => npgsqlOptions.UseVector())
        .Options;

        await using var dbContext =
            new InboxIntelligenceDbContext(options);

        var repository = new EmailRepository(dbContext);

        using var httpClient = new HttpClient();

        var embeddingService =
            new GeminiEmbeddingService(httpClient, configuration);

        var mapperConfiguration = new MapperConfiguration(
            cfg => cfg.AddProfile<MappingProfile>(),
            NullLoggerFactory.Instance);

        var mapper = mapperConfiguration.CreateMapper();

        var answerServiceMock = new Mock<IGeminiAnswerService>();

        var emailService = new EmailService(
            repository,
            embeddingService,
            answerServiceMock.Object,
            mapper,
            NullLogger<EmailService>.Instance);

        var questions = await EvaluationDatasetLoader.LoadAsync();

        var recallScores = new List<double>();

        var reciprocalRanks = new List<double>();

        // Act: evaluate each answerable question.
        foreach (var question in questions)
        {
            if (question.SupportingMessageIds.Count == 0)
            {
                TestContext.WriteLine(
                    $"{question.Id}: Skipped retrieval metrics (no expected supporting IDs).");

                continue;
            }

            var results = await emailService.GetSimilarEmailsAsync(
                question.Question,
                contact: null,
                startDate: null,
                endDate: null);

            var retrievedIds = results
                .Take(5)
                .Select(email => email.ExternalMessageId)
                .ToList();

            var recall = RetrievalMetrics.RecallAtK(
                retrievedIds,
                question.SupportingMessageIds,
                5);

            var reciprocalRank = RetrievalMetrics.ReciprocalRank(
                retrievedIds,
                question.SupportingMessageIds);

            recallScores.Add(recall);
            reciprocalRanks.Add(reciprocalRank);

            TestContext.WriteLine(
                $"{question.Id} | Recall@5: {recall:F2} | MRR: {reciprocalRank:F2}");

            TestContext.WriteLine(
                $"Retrieved: {string.Join(", ", retrievedIds)}");
        }

        // Assert: ensure the evaluation actually ran.
        Assert.IsTrue(
            recallScores.Count > 0,
            "No answerable evaluation questions were executed.");

        // Report aggregate metrics.
        var meanRecall = recallScores.Average();
        var meanReciprocalRank = reciprocalRanks.Average();

        TestContext.WriteLine("------------------------------------");
        TestContext.WriteLine($"Evaluated questions: {recallScores.Count}");
        TestContext.WriteLine($"Mean Recall@5: {meanRecall:F4}");
        TestContext.WriteLine($"Mean Reciprocal Rank: {meanReciprocalRank:F4}");
        TestContext.WriteLine("------------------------------------");
    }

    public TestContext TestContext { get; set; } = null!;
}