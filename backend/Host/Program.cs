using Api;
using Application;
using Host.Middleware;
using Infrastructure;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder
    .AddApi()
    .AddApplication()
    .AddInfrastructure();

var app = builder.Build();

app.MapOpenApi();
app.MapScalarApiReference();

var uploadsPath = Path.Combine(app.Environment.ContentRootPath, "uploads");
if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
}

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads",
    ContentTypeProvider = new FileExtensionContentTypeProvider
    {
        Mappings = 
        { 
            [".png"] = "image/png", 
            [".jpg"] = "image/jpeg", 
            [".jpeg"] = "image/jpeg", 
            [".gif"] = "image/gif", 
            [".webp"] = "image/webp",
            [".svg"] = "image/svg+xml",
            [".bmp"] = "image/bmp",
            [".tiff"] = "image/tiff",
            [".tif"] = "image/tiff",
            [".ico"] = "image/x-icon",
            [".apng"] = "image/apng",
            [".avif"] = "image/avif",
            [".xbm"] = "image/x-xbitmap",
        },
    },
});

app.UseHttpsRedirection();
app.UseMiddleware<GlobalExceptionMiddleware>();

app
    .MapApi()
    .MapApplication()
    .MapInfrastructure();

app.Run();
