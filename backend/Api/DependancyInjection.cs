using Api.Authentication;
using Api.Endpoints.Blogs;
using Api.Endpoints.Posts;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Api;

public static class DependancyInjection
{
    public static WebApplication MapApi(this WebApplication app)
    {
        app.UseAuthentication();
        app.UseAuthorization();

        app
            .MapBlogsEndpoints()
            .MapPostsEndpoints()
            .MapTagsEndpoints();

        app.UseCors("AllowFrontend");

        return app;
    }

    public static WebApplicationBuilder AddApi(this WebApplicationBuilder builder)
    {
        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, c =>
            {
                c.Authority = $"https://{builder.Configuration["Auth0:Domain"]}/";
                c.Audience = builder.Configuration["Auth0:Audience"];
            });

        builder.Services.AddAuthorizationBuilder()
            .AddPermission(Permissions.ReadBlogs)
            .AddPermission(Permissions.WriteBlogs)
            .AddPermission(Permissions.ReadPosts)
            .AddPermission(Permissions.WritePosts);

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

    private static AuthorizationBuilder AddPermission(this AuthorizationBuilder builder, string permission)
    {
        return builder.AddPolicy(permission, p => p.RequireAuthenticatedUser().AddRequirements(new HasScopeRequirement(permission)));
    }
}
