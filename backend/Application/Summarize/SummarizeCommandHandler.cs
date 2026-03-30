using Application.Abstractions;
using Infrastructure.Services;

namespace Application.Summarize;

public class SummarizeCommandHandler(IGroqService groqService) : ICommandHandler<SummarizeCommand, string>
{
    public async Task<string> Handle(SummarizeCommand request, CancellationToken cancellationToken)
    {
        return await groqService.GenerateSummaryAsync(request.Content, cancellationToken);
    }
}
