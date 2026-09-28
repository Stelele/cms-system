using Application.Abstractions;
using Domain.Blogs;
using Domain.Posts;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.Projects;

/// <summary>
/// Takes the context only: there is no file-reference reconciliation on this
/// path - the post delete is the one that calls <c>MarkOrphanedFilesAsync</c>.
///
/// <para>
/// The command carries <c>BlogId</c> for symmetry with <see cref="Application.Posts.DeletePostCommand"/>,
/// but the documented route (<c>DELETE /projects/{id}</c>) only knows the row
/// id, so an empty <c>BlogId</c> means "not supplied" and the row's own blog is
/// used instead. A non-empty <c>BlogId</c> that disagrees with the row makes the
/// delete a 404 rather than a silent success.
/// </para>
/// </summary>
public class DeleteProjectCommandHandler(CmsDbContext db) : ICommandHandler<DeleteProjectCommand, bool>
{
    public async Task<bool> Handle(DeleteProjectCommand request, CancellationToken cancellationToken)
    {
        var post = await db.Posts
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (post is null) return false;
        if (request.BlogId != Guid.Empty && request.BlogId != post.BlogId) return false;

        // Not a project, or a project in a blog that is not a project blog:
        // both are "this delete does not apply", so 404 rather than a throw.
        if (post is not Project) return false;

        var blog = await db.Blogs.FirstOrDefaultAsync(b => b.Id == post.BlogId, cancellationToken);
        if (blog is null || blog.Kind != BlogKind.Project) return false;

        db.Posts.Remove(post);
        await db.SaveChangesAsync(cancellationToken);

        return true;
    }
}
