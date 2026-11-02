namespace InboxIntelligence.Tests.EvaluationTests;

[TestClass]
public class EvaluationDatasetLoaderTests
{
    [TestMethod]
    public async Task LoadAsync_ReturnsAllEvaluationQuestions()
    {
        // Act
        var questions = await EvaluationDatasetLoader.LoadAsync();

        // Assert
        Assert.AreEqual(12, questions.Count);

        Assert.IsTrue(
            questions.All(q =>
                !string.IsNullOrWhiteSpace(q.Id) &&
                !string.IsNullOrWhiteSpace(q.Question)));

        Assert.IsTrue(
            questions.All(q => q.SupportingMessageIds != null));
    }
}