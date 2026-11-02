namespace InboxIntelligence.Tests.EvaluationTests;

public static class RetrievalMetrics
{
    public static double RecallAtK(
        IReadOnlyList<string> retrievedIds,
        IReadOnlyCollection<string> relevantIds,
        int k)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(k);

        if (relevantIds.Count == 0)
            throw new ArgumentException(
                "Relevant IDs cannot be empty.",
                nameof(relevantIds));

        var retrievedTopK = retrievedIds
            .Take(k)
            .ToHashSet(StringComparer.Ordinal);

        var relevantRetrieved = relevantIds
            .Distinct(StringComparer.Ordinal)
            .Count(retrievedTopK.Contains);

        var distinctRelevantCount = relevantIds
            .Distinct(StringComparer.Ordinal)
            .Count();

        return (double)relevantRetrieved / distinctRelevantCount;
    }

    public static double ReciprocalRank(
        IReadOnlyList<string> retrievedIds,
        IReadOnlyCollection<string> relevantIds)
    {
        if (relevantIds.Count == 0)
            throw new ArgumentException(
                "Relevant IDs cannot be empty.",
                nameof(relevantIds));

        var relevantSet = relevantIds.ToHashSet(StringComparer.Ordinal);

        for (var i = 0; i < retrievedIds.Count; i++)
        {
            if (relevantSet.Contains(retrievedIds[i]))
                return 1.0 / (i + 1);
        }

        return 0.0;
    }

    public static double MeanReciprocalRank(
        IEnumerable<(IReadOnlyList<string> RetrievedIds,
            IReadOnlyCollection<string> RelevantIds)> queries)
    {
        var answerableQueries = queries
            .Where(q => q.RelevantIds.Count > 0)
            .ToList();

        if (answerableQueries.Count == 0)
            throw new ArgumentException(
                "At least one answerable query is required.",
                nameof(queries));

        return answerableQueries.Average(q =>
            ReciprocalRank(q.RetrievedIds, q.RelevantIds));
    }
}