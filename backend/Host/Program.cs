using Api;
using Application;
using Host.Middleware;
using Host.OpenApi;
using Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
});

builder
    .AddApi()
    .AddApplication()
    .AddInfrastructure();

var app = builder.Build();

app.MapOpenApi();
app.MapScalarApiReference();

app.UseHttpsRedirection();
app.UseMiddleware<GlobalExceptionMiddleware>();

app
    .MapApi()
    .MapApplication()
    .MapInfrastructure();

app.Run();
