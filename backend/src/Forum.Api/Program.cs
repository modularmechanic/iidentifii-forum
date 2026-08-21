using Forum.Api.Errors;
using Forum.Api.RateLimiting;
using Forum.Api.Serialization;
using Forum.Application;
using Forum.Infrastructure;
using Forum.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddForumApplication();
builder.Services.AddForumInfrastructure(builder.Configuration);

// Payloads resolve through the source-generated context; see ForumJsonContext.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, ForumJsonContext.Default));
builder.Services.Configure<JsonOptions>(options =>
    options.JsonSerializerOptions.TypeInfoResolverChain.Insert(0, ForumJsonContext.Default));

builder.Services.AddProblemDetails();
builder.Services.AddValidationProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddForumRateLimiting();
builder.Services.AddOpenApi();

var app = builder.Build();

await app.InitialiseDatabaseAsync(
    seed: builder.Configuration.GetValue("Database:SeedOnStartup", defaultValue: false));

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options => options.WithTitle("iiDENTIFii Forum API"));
}

app.UseAuthorization();
app.MapControllers();

app.Run();

/// <summary>Exposed so the integration tests can host the application in memory.</summary>
public partial class Program;
