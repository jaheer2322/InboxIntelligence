namespace InboxIntelligence.Services
{
    public interface IEmbeddingService
    {
        public Task<float[]> GenerateEmbeddingAsync(
            string text,
            CancellationToken cancellationToken = default);
    }
}
