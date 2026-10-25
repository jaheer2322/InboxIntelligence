namespace InboxIntelligence.Services
{
    public interface IAgenticEmailService
    {
        Task<string> AskAsync(
            string query, 
            CancellationToken cancellationToken = default);
    }
}
