using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.Endpoints.Posts;

public static class PostEndpoints
{
    public static WebApplication MapPostsEndpoints(this WebApplication app)
    {
        app.MapGet("/blogs/{blogId:guid}/posts", async (Guid blogId, [FromQuery] string? tag, [FromQuery] bool? isPublished, ISender mediator) =>
        {
            var posts = await mediator.Send(new Application.Posts.GetPostsByBlogQuery(blogId, tag, isPublished));
            return Results.Ok(posts);
        })
        .WithName("GetPostsByBlog")
        .WithDisplayName("GetPostsByBlog")
        .Produces<List<Application.DTOs.PostResponse>>(StatusCodes.Status200OK)
        .WithTags(EndpointTags.Posts)
        .RequireAuthorization(Permissions.ReadPosts);

        app.MapGet("/blogs/{blogId:guid}/posts/{id:guid}", async (Guid blogId, Guid id, ISender mediator) =>
        {
            var post = await mediator.Send(new Application.Posts.GetPostByIdQuery(blogId, id));
            return post is not null ? Results.Ok(post) : Results.NotFound();
        })
        .WithName("GetPostById")
        .WithDisplayName("GetPostById")
        .Produces<Application.DTOs.PostResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .WithTags(EndpointTags.Posts)
        .RequireAuthorization(Permissions.ReadPosts);

        app.MapGet("/posts/slug/{slug}", async (string slug, ISender mediator) =>
        {
            var post = await mediator.Send(new Application.Posts.GetPostBySlugQuery(slug));
            return post is not null ? Results.Ok(post) : Results.NotFound();
        })
        .WithName("GetPostBySlug")
        .WithDisplayName("GetPostBySlug")
        .Produces<Application.DTOs.PostResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .WithTags(EndpointTags.Posts)
        .RequireAuthorization(Permissions.ReadPosts);

        app.MapPost("/blogs/{blogId:guid}/posts", async (Guid blogId, Application.Posts.CreatePostCommand command, ISender mediator) =>
        {
            var createCommand = command with { BlogId = blogId };
            var postId = await mediator.Send(createCommand);
            return Results.Created($"/blogs/{blogId}/posts/{postId}", new { Id = postId });
        })
        .WithName("CreatePost")
        .WithDisplayName("CreatePost")
        .Accepts<Application.Posts.CreatePostCommand>("application/json")
        .Produces<Guid>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .WithTags(EndpointTags.Posts)
        .RequireAuthorization(Permissions.WritePosts);

        app.MapPut("/blogs/{blogId:guid}/posts/{id:guid}", async (Guid blogId, Guid id, Application.Posts.UpdatePostCommand command, ISender mediator) =>
        {
            var updateCommand = command with { BlogId = blogId, Id = id };
            var result = await mediator.Send(updateCommand);
            return result ? Results.Ok() : Results.NotFound();
        })
        .WithName("UpdatePost")
        .WithDisplayName("UpdatePost")
        .Accepts<Application.Posts.UpdatePostCommand>("application/json")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .ProducesValidationProblem()
        .WithTags(EndpointTags.Posts)
        .RequireAuthorization(Permissions.WritePosts);

        app.MapDelete("/blogs/{blogId:guid}/posts/{id:guid}", async (Guid blogId, Guid id, ISender mediator) =>
        {
            var result = await mediator.Send(new Application.Posts.DeletePostCommand(blogId, id));
            return result ? Results.NoContent() : Results.NotFound();
        })
        .WithName("DeletePost")
        .WithDisplayName("DeletePost")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound)
        .WithTags(EndpointTags.Posts)
        .RequireAuthorization(Permissions.WritePosts);

        return app;
    }
}
