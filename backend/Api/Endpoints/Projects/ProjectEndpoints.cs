using Application.DTOs;
using Application.Projects;
using Domain.Posts;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.Endpoints.Projects;

/// <summary>
/// The project endpoint family: the authenticated CRUD surface plus its two
/// anonymous read mirrors. Permissions are deliberately <c>read:posts</c> and
/// <c>write:posts</c> - a project-specific scope would be more precise but needs
/// an Auth0 API configuration change, and an unconfigured scope fails as a
/// confusing 403 at runtime.
/// </summary>
public static class ProjectEndpoints
{
    public static WebApplication MapProjectsEndpoints(this WebApplication app)
    {
        app.MapGet("/projects", async (ISender mediator, [FromQuery] string? category = null) =>
        {
            var filter = ParseCategory(category, out var error);
            return error is not null
                ? Results.BadRequest(error)
                : Results.Ok(await mediator.Send(new GetProjectsQuery(filter)));
        })
        .WithName("GetProjects")
        .WithDisplayName("GetProjects")
        .Produces<List<ProjectSummaryResponse>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .WithTags(EndpointTags.Projects)
        .RequireAuthorization(Permissions.ReadPosts);

        app.MapGet("/projects/{slug}", async (string slug, ISender mediator) =>
        {
            var project = await mediator.Send(new GetProjectBySlugQuery(slug));
            return project is not null ? Results.Ok(project) : Results.NotFound();
        })
        .WithName("GetProjectBySlug")
        .WithDisplayName("GetProjectBySlug")
        .Produces<ProjectResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .WithTags(EndpointTags.Projects)
        .RequireAuthorization(Permissions.ReadPosts);

        app.MapPost("/projects", async (CreateProjectCommand command, ISender mediator) =>
        {
            var projectId = await mediator.Send(command);
            return Results.Created($"/projects/{projectId}", new { Id = projectId });
        })
        .WithName("CreateProject")
        .WithDisplayName("CreateProject")
        .Accepts<CreateProjectCommand>("application/json")
        .Produces<Guid>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .WithTags(EndpointTags.Projects)
        .RequireAuthorization(Permissions.WritePosts);

        app.MapPut("/projects/{id:guid}", async (Guid id, UpdateProjectCommand command, ISender mediator) =>
        {
            var updatedCommand = command with { Id = id };
            var result = await mediator.Send(updatedCommand);
            return result ? Results.Ok() : Results.NotFound();
        })
        .WithName("UpdateProject")
        .WithDisplayName("UpdateProject")
        .Accepts<UpdateProjectCommand>("application/json")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .ProducesValidationProblem()
        .WithTags(EndpointTags.Projects)
        .RequireAuthorization(Permissions.WritePosts);

        app.MapDelete("/projects/{id:guid}", async (Guid id, ISender mediator) =>
        {
            // The route carries only the row id. DeleteProjectCommand also takes
            // the blog id for symmetry with the post delete, and Guid.Empty is
            // its "not supplied" value - the handler resolves the blog from the
            // row itself.
            var result = await mediator.Send(new DeleteProjectCommand(Guid.Empty, id));
            return result ? Results.NoContent() : Results.NotFound();
        })
        .WithName("DeleteProject")
        .WithDisplayName("DeleteProject")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound)
        .WithTags(EndpointTags.Projects)
        .RequireAuthorization(Permissions.WritePosts);

        // Anonymous mirrors. The queries behind them hard-filter IsPublished as
        // a constant, so a draft cannot reach an anonymous caller without a code
        // change.
        app.MapGet("/public/projects", async (ISender mediator, [FromQuery] string? category = null) =>
        {
            var filter = ParseCategory(category, out var error);
            return error is not null
                ? Results.BadRequest(error)
                : Results.Ok(await mediator.Send(new GetPublicProjectsQuery(filter)));
        })
        .WithName("GetPublicProjects")
        .WithDisplayName("GetPublicProjects")
        .Produces<List<ProjectSummaryResponse>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .WithTags(EndpointTags.Projects)
        .AllowAnonymous();

        app.MapGet("/public/projects/{slug}", async (string slug, ISender mediator) =>
        {
            var project = await mediator.Send(new GetPublicProjectBySlugQuery(slug));
            return project is not null ? Results.Ok(project) : Results.NotFound();
        })
        .WithName("GetPublicProjectBySlug")
        .WithDisplayName("GetPublicProjectBySlug")
        .Produces<ProjectResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .WithTags(EndpointTags.Projects)
        .AllowAnonymous();

        return app;
    }

    /// <summary>
    /// Maps <c>?category=game-dev</c> to the enum through the registry that owns
    /// the mapping, rather than relying on the enum's member names, so the query
    /// string and <c>ProjectBlogs</c> cannot drift apart. An unknown slug is an
    /// error, not an absent filter: silently returning every project for a typo
    /// would be worse than saying so.
    /// </summary>
    private static ProjectCategory? ParseCategory(string? category, out string? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(category))
            return null;

        var definition = ProjectBlogs.ForSlug(category);
        if (definition is null)
        {
            error = $"Unknown project category '{category}'.";
            return null;
        }

        return definition.Category;
    }
}
