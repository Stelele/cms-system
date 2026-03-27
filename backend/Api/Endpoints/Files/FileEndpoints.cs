using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Application.Files;

namespace Api.Endpoints.Files;

public static class FileEndpoints
{
    public static WebApplication MapFileEndpoints(this WebApplication app)
    {
        app.MapPost("/files/upload", async (IFormFile file, ISender mediator, CancellationToken ct) =>
        {
            var command = new UploadFileCommand(file);
            var result = await mediator.Send(command, ct);
            return Results.Created($"/files/{result.Id}", result);
        })
        .DisableAntiforgery()
        .WithName("UploadFile")
        .WithDisplayName("UploadFile")
        .Produces<FileResponse>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .WithTags(EndpointTags.Files)
        .RequireAuthorization(Permissions.WriteFiles);

        app.MapGet("/files/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetFileByIdQuery(id), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        })
        .WithName("GetFileById")
        .WithDisplayName("GetFileById")
        .Produces<FileResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .WithTags(EndpointTags.Files)
        .RequireAuthorization(Permissions.ReadFiles);

        app.MapDelete("/files/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new DeleteFileCommand(id), ct);
            return result ? Results.NoContent() : Results.BadRequest(new { error = "File is in use or not found." });
        })
        .WithName("DeleteFile")
        .WithDisplayName("DeleteFile")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound)
        .WithTags(EndpointTags.Files)
        .RequireAuthorization(Permissions.WriteFiles);

        app.MapPost("/files/{fileId:guid}/posts/{postId:guid}", async (Guid fileId, Guid postId, ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new AssociateFileWithPostCommand(fileId, postId), ct);
            return result ? Results.NoContent() : Results.NotFound();
        })
        .WithName("AssociateFileWithPost")
        .WithDisplayName("AssociateFileWithPost")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound)
        .WithTags(EndpointTags.Files)
        .RequireAuthorization(Permissions.WriteFiles);

        app.MapGet("/posts/{postId:guid}/files", async (Guid postId, ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetFilesByPostQuery(postId), ct);
            return Results.Ok(result);
        })
        .WithName("GetFilesByPost")
        .WithDisplayName("GetFilesByPost")
        .Produces<List<FileResponse>>(StatusCodes.Status200OK)
        .WithTags(EndpointTags.Files)
        .RequireAuthorization(Permissions.ReadFiles);

        return app;
    }
}
