using Application;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Api.Posts;

public static class TagEndpoints
{
    public static WebApplication MapTagsEndpoints(this WebApplication app)
    {
        app.MapGet("/tags", async (ISender mediator) =>
        {
            var tags = await mediator.Send(new Application.Posts.GetTagsQuery());
            return Results.Ok(tags);
        })
        .WithName("GetTags")
        .WithDisplayName("GetTags")
        .Produces<List<Application.DTOs.TagResponse>>(StatusCodes.Status200OK)
        .WithTags(EndpointTags.Tags)
        .RequireAuthorization(Permissions.ReadPosts);

        return app;
    }
}
