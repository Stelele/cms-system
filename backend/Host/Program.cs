using Api;
using Application;
using Host.Middleware;
using Infrastructure;
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

app.UseHttpsRedirection();
app.UseMiddleware<GlobalExceptionMiddleware>();

app
    .MapApi()
    .MapApplication()
    .MapInfrastructure();

app.Run();
