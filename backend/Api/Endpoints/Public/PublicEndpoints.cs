using Application.Blogs;
using Application.DTOs;
using Application.Posts;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.Endpoints.Public;

public static class PublicEndpoints
{
    /// <summary>
    /// Anonymous read surface for published content. Every endpoint here returns
    /// a Public* DTO, which structurally cannot carry IsPublished, CreatedOn or
    /// UpdatedOn.
    ///
    /// The two post queries filter IsPublished in their Where clause as a
    /// constant rather than a parameter, so a draft cannot reach an anonymous
    /// caller without a code change. GetPublicBlogsQuery has no such filter and
    /// does not need one: Blog has no publish state, so every blog is public.
    /// </summary>
    public static WebApplication MapPublicEndpoints(this WebApplication app)
    {
        app.MapGet("/public/blogs", async (IMediator mediator, [FromQuery] string[]? slugs = null) =>
        {
            var blogs = await mediator.Send(new GetPublicBlogsQuery(slugs));
            return Results.Ok(blogs);
        })
        .WithName("GetPublicBlogs")
        .WithDisplayName("GetPublicBlogs")
        .Produces<List<PublicBlogResponse>>(StatusCodes.Status200OK)
        .WithTags(EndpointTags.Public)
        .AllowAnonymous();

        app.MapGet("/public/blogs/{id:guid}/posts", async (Guid id, IMediator mediator) =>
        {
            var posts = await mediator.Send(new GetPublicPostsByBlogQuery(id));
            return Results.Ok(posts);
        })
        .WithName("GetPublicPostsByBlog")
        .WithDisplayName("GetPublicPostsByBlog")
        .Produces<List<PublicPostResponse>>(StatusCodes.Status200OK)
        .WithTags(EndpointTags.Public)
        .AllowAnonymous();

        app.MapGet("/public/posts/slug/{slug}", async (string slug, IMediator mediator) =>
        {
            var post = await mediator.Send(new GetPublicPostBySlugQuery(slug));
            return post is not null ? Results.Ok(post) : Results.NotFound();
        })
        .WithName("GetPublicPostBySlug")
        .WithDisplayName("GetPublicPostBySlug")
        .Produces<PublicPostResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .WithTags(EndpointTags.Public)
        .AllowAnonymous();

        return app;
    }
}
