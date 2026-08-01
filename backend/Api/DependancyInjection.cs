using Api.Authentication;
using Api.Endpoints.Blogs;
using Api.Endpoints.Files;
using Api.Endpoints.Posts;
using Api.Endpoints.Summarize;
using Auth0.AspNetCore.Authentication.Api;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Api;

public static class DependancyInjection
{
    public static WebApplication MapApi(this WebApplication app)
    {
        app.UseCors("AllowFrontend");

        app.UseAuthentication();
        app.UseAuthorization();

        app
            .MapBlogsEndpoints()
            .MapPostsEndpoints()
            .MapTagsEndpoints()
            .MapFileEndpoints()
            .MapSummarizeEndpoints();

        return app;
    }

    public static WebApplicationBuilder AddApi(this WebApplicationBuilder builder)
    {
        var auth0Domain = builder.Configuration["Auth0:Domain"]
            ?? throw new InvalidOperationException("Auth0:Domain configuration is required");
        var auth0Audience = builder.Configuration["Auth0:Audience"]
            ?? throw new InvalidOperationException("Auth0:Audience configuration is required");
        var issuer = $"https://{auth0Domain}/";

        builder.Services.AddAuth0ApiAuthentication(auth0Options =>
        {
            auth0Options.Domain = auth0Domain;
        }, jwtBearerOptions =>
        {
            jwtBearerOptions.Audience = auth0Audience;
            jwtBearerOptions.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    var logger = context.HttpContext.RequestServices
                        .GetRequiredService<ILogger<JwtBearerEvents>>();
                    logger.LogWarning(
                        context.Exception,
                        "Auth0 authentication failed: {Message}",
                        context.Exception.Message);
                    return Task.CompletedTask;
                },
                OnTokenValidated = context =>
                {
                    var logger = context.HttpContext.RequestServices
                        .GetRequiredService<ILogger<JwtBearerEvents>>();
                    var userId = context.Principal?.FindFirst("sub")?.Value;
                    var scopes = context.Principal?.FindFirst("scope")?.Value;
                    logger.LogInformation(
                        "Auth0 token validated. UserId: {UserId}, Scopes: {Scopes}",
                        userId ?? "unknown",
                        scopes ?? "none");
                    return Task.CompletedTask;
                },
                    OnChallenge = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILogger<JwtBearerEvents>>();
                        logger.LogWarning(
                            "Auth0 authentication challenge issued. Error: {Error}, Description: {Description}",
                            context.Error,
                            context.ErrorDescription);
                        return Task.CompletedTask;
                    }
                };
            });

        builder.Services.AddAuthorizationBuilder()
            .AddPermission(Permissions.ReadBlogs, issuer)
            .AddPermission(Permissions.WriteBlogs, issuer)
            .AddPermission(Permissions.ReadPosts, issuer)
            .AddPermission(Permissions.WritePosts, issuer)
            .AddPermission(Permissions.ReadFiles, issuer)
            .AddPermission(Permissions.WriteFiles, issuer)
            .AddPermission(Permissions.SummarizeArticles, issuer);

        builder.Services.AddTransient<IAuthorizationHandler, HasScopeHandler>();

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowFrontend", policy =>
            {
                policy
                    .WithOrigins("*")
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        return builder;
    }

    private static AuthorizationBuilder AddPermission(
        this AuthorizationBuilder builder,
        string permission,
        string issuer)
    {
        return builder.AddPolicy(permission, p =>
            p.RequireAuthenticatedUser()
             .AddRequirements(new HasScopeRequirement(permission, issuer)));
    }
}
