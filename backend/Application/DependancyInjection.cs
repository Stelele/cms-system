using Application.PipelineBehaviours;
using Application.Posts;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Application;

public static class DependancyInjection
{
    public static WebApplication MapApplication(this WebApplication app)
    {
        return app;
    }

    public static WebApplicationBuilder AddApplication(this WebApplicationBuilder builder)
    {
        var mediatRLicenseKey = builder.Configuration["MediatR:LicenseKey"];

        builder.Services.AddMediatR(cfg =>
        {
            cfg.LicenseKey = mediatRLicenseKey;
            cfg.RegisterServicesFromAssembly(typeof(CreatePostCommand).Assembly);
        });
        builder.Services.AddValidatorsFromAssembly(typeof(CreatePostCommand).Assembly);
        builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PipelineBehaviours.ValidationBehaviour<,>));

        return builder;
    }
}
