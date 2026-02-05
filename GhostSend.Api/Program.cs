using GhostSend.Api.Middleware;
using GhostSend.Application;
using GhostSend.Application.Common.Settings;
using GhostSend.Infrastructure;
using GhostSend.Infrastructure.BackgroundJobs;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Bind Settings
builder.Services.Configure<FileSettings>(builder.Configuration.GetSection(FileSettings.SectionName));
builder.Services.Configure<CorsSettings>(builder.Configuration.GetSection(CorsSettings.SectionName));

// Configure Limits from Settings
var fileSettings = builder.Configuration.GetSection(FileSettings.SectionName).Get<FileSettings>();
if (fileSettings != null)
{
    builder.Services.Configure<FormOptions>(options =>
    {
        options.MultipartBodyLengthLimit = fileSettings.MaxFileSizeInBytes;
    });

    builder.WebHost.ConfigureKestrel(options =>
    {
        options.Limits.MaxRequestBodySize = fileSettings.MaxFileSizeInBytes;
    });
}

// CORS
var corsSettings = builder.Configuration.GetSection(CorsSettings.SectionName).Get<CorsSettings>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCors", policy =>
    {
        if (corsSettings != null && corsSettings.AllowedOrigins.Length > 0)
        {
            policy.WithOrigins(corsSettings.AllowedOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
    });
});

// Infrastructure DI
builder.Services.AddInfrastructure(builder.Configuration);


// Application DI (MediatR, Validators, Behaviors)
builder.Services.AddApplication();

builder.Services.AddHostedService<FileCleanWorker>();

builder.Services.AddRateLimiter(options =>
{

    options.AddFixedWindowLimiter(policyName: "fixed", options =>
    {
        options.PermitLimit = 10;
        options.Window = TimeSpan.FromMinutes(1);
        options.QueueLimit = 0;
    });

    options.OnRejected = async (context, cancellationToken) =>
    {
        await context.HttpContext.Response.WriteAsJsonAsync(
            new
            {
                Title = "Too many requests",
                Status = StatusCodes.Status429TooManyRequests,
                Detail = "Queue exceeded. Please try again later.",
                Layer = "API"
            },
            cancellationToken);
    };
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("DefaultCors");
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

namespace GhostSend.Api
{
    public partial class Program { }
}
