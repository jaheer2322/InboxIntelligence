using System.Text.Json;

namespace InboxIntelligence.Tests.EvaluationTests;

public static class EvaluationDatasetLoader
{
    public static async Task<List<EvaluationQuestion>> LoadAsync()
    {
        var filePath = Path.Combine(
            AppContext.BaseDirectory,
            "EvaluationTests",
            "evaluationQuestions.json");

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException(
                "Evaluation dataset was not found.",
                filePath);
        }

        var json = await File.ReadAllTextAsync(filePath);

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var questions =
            JsonSerializer.Deserialize<List<EvaluationQuestion>>(
                json, options);

        return questions
            ?? throw new InvalidOperationException(
                "Failed to deserialize evaluation dataset.");
    }
}