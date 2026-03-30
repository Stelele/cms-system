using Api;
using Application.Summarize;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Api.Endpoints.Summarize;

public static class SummarizeEndpoints
{
    public static WebApplication MapSummarizeEndpoints(this WebApplication app)
    {
        app.MapPost("/summarize", async (SummarizeCommand command, ISender mediator) =>
        {
            var summary = await mediator.Send(command);
            return Results.Ok(new { summary });
        })
        .WithName("Summarize")
        .WithDisplayName("Summarize")
        .Produces<SummaryResponse>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .WithTags(EndpointTags.Summarize)
        .RequireAuthorization(Permissions.SummarizeArticles);

        return app;
    }
}

public record SummaryResponse(string summary);
