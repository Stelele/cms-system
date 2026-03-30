namespace Infrastructure.Services;

public interface IGroqService
{
    Task<string> GenerateSummaryAsync(string content, CancellationToken cancellationToken = default);
}
