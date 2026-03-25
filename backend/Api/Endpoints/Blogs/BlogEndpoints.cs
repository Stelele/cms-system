using Application.Blogs;
using Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Api.Endpoints.Blogs;

public static class BlogEndpoints
{
    public static WebApplication MapBlogsEndpoints(this WebApplication app)
    {
        app.MapGet("/blogs", async (IMediator mediator) =>
        {
            var blogs = await mediator.Send(new GetBlogsQuery());
            return Results.Ok(blogs);
        })
        .WithName("GetBlogs")
        .WithDisplayName("GetBlogs")
        .Produces<List<BlogResponse>>(StatusCodes.Status200OK)
        .WithTags(EndpointTags.Blogs)
        .RequireAuthorization(Permissions.ReadBlogs);

        app.MapGet("/blogs/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var blog = await mediator.Send(new GetBlogByIdQuery(id));
            return blog is not null ? Results.Ok(blog) : Results.NotFound();
        })
        .WithName("GetBlogById")
        .WithDisplayName("GetBlogById")
        .Produces<BlogResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .WithTags(EndpointTags.Blogs)
        .RequireAuthorization(Permissions.ReadBlogs);

        app.MapPost("/blogs", async (CreateBlogCommand command, IMediator mediator) =>
        {
            var blogId = await mediator.Send(command);
            return Results.Created($"/blogs/{blogId}", new { Id = blogId });
        })
        .WithName("CreateBlog")
        .WithDisplayName("CreateBlog")
        .Accepts<CreateBlogCommand>("application/json")
        .Produces<Guid>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .WithTags(EndpointTags.Blogs)
        .RequireAuthorization(Permissions.WriteBlogs);

        app.MapPut("/blogs/{id:guid}", async (Guid id, UpdateBlogCommand command, IMediator mediator) =>
        {
            var updatedCommand = command with { Id = id };
            var result = await mediator.Send(updatedCommand);
            return result ? Results.Ok() : Results.NotFound();
        })
        .WithName("UpdateBlog")
        .WithDisplayName("UpdateBlog")
        .Accepts<UpdateBlogCommand>("application/json")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .ProducesValidationProblem()
        .WithTags(EndpointTags.Blogs)
        .RequireAuthorization(Permissions.WriteBlogs);

        app.MapDelete("/blogs/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var result = await mediator.Send(new DeleteBlogCommand(id));
            return result ? Results.NoContent() : Results.NotFound();
        })
        .WithName("DeleteBlog")
        .WithDisplayName("DeleteBlog")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound)
        .WithTags(EndpointTags.Blogs)
        .RequireAuthorization(Permissions.WriteBlogs);

        return app;
    }
}
