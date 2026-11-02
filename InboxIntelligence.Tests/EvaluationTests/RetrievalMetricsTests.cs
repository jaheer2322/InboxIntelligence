namespace InboxIntelligence.Tests.EvaluationTests;

[TestClass]
public class RetrievalMetricsTests
{
    [TestMethod]
    public void RecallAtK_AllRelevantEmailsRetrieved_ReturnsOne()
    {
        var retrieved = new[]
        {
            "MSG-1001", "MSG-1002", "MSG-1003"
        };

        var relevant = new[]
        {
            "MSG-1001", "MSG-1002", "MSG-1003"
        };

        Assert.AreEqual(
            1.0,
            RetrievalMetrics.RecallAtK(retrieved, relevant, 3));
    }

    [TestMethod]
    public void RecallAtK_OnlyOneOfTwoRelevantEmailsRetrieved_ReturnsHalf()
    {
        var retrieved = new[]
        {
            "MSG-1001", "MSG-2000", "MSG-1003"
        };

        var relevant = new[]
        {
            "MSG-1001", "MSG-1002"
        };

        Assert.AreEqual(
            0.5,
            RetrievalMetrics.RecallAtK(retrieved, relevant, 3));
    }

    [TestMethod]
    public void RecallAtK_RelevantEmailOutsideK_IsNotCounted()
    {
        var retrieved = new[]
        {
            "MSG-2000", "MSG-2001", "MSG-1001"
        };

        var relevant = new[] { "MSG-1001" };

        Assert.AreEqual(
            0.0,
            RetrievalMetrics.RecallAtK(retrieved, relevant, 2));
    }

    [TestMethod]
    public void ReciprocalRank_FirstResultIsRelevant_ReturnsOne()
    {
        var retrieved = new[]
        {
            "MSG-1001", "MSG-2000", "MSG-1002"
        };

        var relevant = new[] { "MSG-1001", "MSG-1002" };

        Assert.AreEqual(
            1.0,
            RetrievalMetrics.ReciprocalRank(retrieved, relevant));
    }

    [TestMethod]
    public void ReciprocalRank_FirstRelevantEmailIsThird_ReturnsOneThird()
    {
        var retrieved = new[]
        {
            "MSG-2000", "MSG-2001", "MSG-1001"
        };

        var relevant = new[] { "MSG-1001" };

        Assert.AreEqual(
            1.0 / 3.0,
            RetrievalMetrics.ReciprocalRank(retrieved, relevant),
            0.000001);
    }

    [TestMethod]
    public void ReciprocalRank_NoRelevantEmailRetrieved_ReturnsZero()
    {
        var retrieved = new[] { "MSG-2000", "MSG-2001" };
        var relevant = new[] { "MSG-1001" };

        Assert.AreEqual(
            0.0,
            RetrievalMetrics.ReciprocalRank(retrieved, relevant));
    }

    [TestMethod]
    public void MeanReciprocalRank_ExcludesUnanswerableQueries()
    {
        var queries = new[]
        {
            (
                (IReadOnlyList<string>)new[] { "MSG-1001", "MSG-2000" },
                (IReadOnlyCollection<string>)new[] { "MSG-1001" }
            ),
            (
                (IReadOnlyList<string>)new[] { "MSG-2000", "MSG-1002" },
                (IReadOnlyCollection<string>)new[] { "MSG-1002" }
            ),
            (
                (IReadOnlyList<string>)Array.Empty<string>(),
                (IReadOnlyCollection<string>)Array.Empty<string>()
            )
        };

        Assert.AreEqual(
            0.75,
            RetrievalMetrics.MeanReciprocalRank(queries),
            0.000001);
    }
}