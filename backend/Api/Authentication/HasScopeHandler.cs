using Microsoft.AspNetCore.Authorization;

namespace Api.Authentication;

public class HasScopeRequirement(string scope, string issuer) : IAuthorizationRequirement
{
    public string Scope { get; } = scope;
    public string Issuer { get; } = issuer;
}

public class HasScopeHandler : AuthorizationHandler<HasScopeRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        HasScopeRequirement requirement)
    {
        var scopeClaim = context.User.FindFirst(c =>
            c.Type == "scope" && c.Issuer == requirement.Issuer);

        if (scopeClaim is null)
            return Task.CompletedTask;

        var scopes = scopeClaim.Value.Split(' ');

        if (scopes.Contains(requirement.Scope))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
