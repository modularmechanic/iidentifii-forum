using Forum.Api.Errors;
using Forum.Api.RateLimiting;
using Forum.Api.Serialization;
using Microsoft.AspNetCore.Mvc;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Payload serialisation is generated at compile time; see ForumJsonContext.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, ForumJsonContext.Default));
builder.Services.Configure<JsonOptions>(options =>
    options.JsonSerializerOptions.TypeInfoResolverChain.Insert(0, ForumJsonContext.Default));

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddForumRateLimiting();
builder.Services.AddOpenApi();

var app = builder.Build();

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
