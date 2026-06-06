using FluentValidation;
using GhostSend.Api.Middleware;
using GhostSend.Application;
using GhostSend.Application.Common.Settings;
using GhostSend.Infrastructure;
using GhostSend.Infrastructure.BackgroundJobs;
using GhostSend.Domain.Interfaces;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Amazon.S3;
using MediatR;
using GhostSend.Api.DTOs.Requests;
using GhostSend.Api.DTOs.Responses;
using GhostSend.Application.Files.Queries.DownloadFile;
using GhostSend.Domain.Exceptions;
using Microsoft.AspNetCore.Http; // For IHeaderDictionary extension

var builder = WebApplication.CreateBuilder(args);

// Map flat environment variables to ASP.NET Core configuration paths to support standalone Docker deployments in Dokploy
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL")?.Trim(' ', '"');
if (!string.IsNullOrEmpty(databaseUrl))
{
    builder.Configuration["ConnectionStrings:DefaultConnection"] = databaseUrl;
}

var maxFileSize = Environment.GetEnvironmentVariable("MAX_FILE_SIZE")?.Trim(' ', '"');
if (!string.IsNullOrEmpty(maxFileSize))
{
    builder.Configuration["FileSettings:MaxFileSizeInBytes"] = maxFileSize;
}

var minioUrl = Environment.GetEnvironmentVariable("MINIO_SERVICE_URL")?.Trim(' ', '"');
if (!string.IsNullOrEmpty(minioUrl))
{
    builder.Configuration["MinioSettings:ServiceURL"] = minioUrl;
}

var minioAccess = Environment.GetEnvironmentVariable("MINIO_ACCESS_KEY")?.Trim(' ', '"');
if (!string.IsNullOrEmpty(minioAccess))
{
    builder.Configuration["MinioSettings:AccessKey"] = minioAccess;
}

var minioSecret = Environment.GetEnvironmentVariable("MINIO_SECRET_KEY")?.Trim(' ', '"');
if (!string.IsNullOrEmpty(minioSecret))
{
    builder.Configuration["MinioSettings:SecretKey"] = minioSecret;
}

var minioBucket = Environment.GetEnvironmentVariable("MINIO_BUCKET_NAME")?.Trim(' ', '"');
if (!string.IsNullOrEmpty(minioBucket))
{
    builder.Configuration["MinioSettings:BucketName"] = minioBucket;
}

var corsOrigins = Environment.GetEnvironmentVariable("CORS_ALLOWED_ORIGINS")?.Trim(' ', '"');
if (!string.IsNullOrEmpty(corsOrigins))
{
    builder.Configuration["CorsSettings:AllowedOrigins:0"] = corsOrigins;
}

var maxLifeTime = Environment.GetEnvironmentVariable("MAX_LIFETIME_HOURS")?.Trim(' ', '"');
if (!string.IsNullOrEmpty(maxLifeTime))
{
    builder.Configuration["FileSettings:MaxLifetimeInHours"] = maxLifeTime;
}

var allowedHosts = Environment.GetEnvironmentVariable("ALLOWED_HOSTS")?.Trim(' ', '"');
if (!string.IsNullOrEmpty(allowedHosts))
{
    builder.Configuration["AllowedHosts"] = allowedHosts;
}

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
        options.KeyLengthLimit = 256;
        options.ValueCountLimit = 1024;
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
        if (corsSettings?.AllowedOrigins != null && corsSettings.AllowedOrigins.Length > 0)
        {
            var origins = corsSettings.AllowedOrigins
                .SelectMany(o => o.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .Select(o => o.Trim(' ', '"')) // Clean double quotes inside individual split items!
                .ToArray();

            policy.WithOrigins(origins)
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
        else if (builder.Environment.IsDevelopment())
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
    });
});

// Configure Amazon S3 client for MinIO
builder.Services.AddSingleton<IAmazonS3>(sp =>
{
    var minioConfig = builder.Configuration.GetSection("MinioSettings");
    var rawServiceUrl = minioConfig["ServiceURL"] ?? "http://localhost:9000";
    var serviceUrl = rawServiceUrl.StartsWith("http://") || rawServiceUrl.StartsWith("https://")
        ? rawServiceUrl
        : $"http://{rawServiceUrl}";
    // Default to port 9000 for MinIO if no explicit port is specified
    if (Uri.TryCreate(serviceUrl, UriKind.Absolute, out var uri) && uri.IsDefaultPort)
    {
        serviceUrl = $"{uri.Scheme}://{uri.Host}:9000";
    }
    var accessKey = minioConfig["AccessKey"] ?? "minioadmin";
    var secretKey = minioConfig["SecretKey"] ?? "minioadmin";
    var forcePathStyle = !bool.TryParse(minioConfig["ForcePathStyle"], out var parsed) || parsed;

    var s3Config = new AmazonS3Config
    {
        ServiceURL = serviceUrl,
        ForcePathStyle = forcePathStyle
    };

    return new AmazonS3Client(accessKey, secretKey, s3Config);
});

// Infrastructure DI
builder.Services.AddInfrastructure(builder.Configuration);


// Application DI (MediatR, Validators, Behaviors)
builder.Services.AddApplication();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddHostedService<FileCleanWorker>();
builder.Services.AddHostedService<StorageReconciliationWorker>();

builder.Services.AddRateLimiter(options =>
{
    // Strict policy for file uploads: each request can be very resource-intensive.
    options.AddFixedWindowLimiter(policyName: "upload", options =>
    {
        options.PermitLimit = 3;
        options.Window = TimeSpan.FromMinutes(1);
        options.QueueLimit = 0;
    });

    // More permissive policy for read/metadata/delete operations.
    options.AddFixedWindowLimiter(policyName: "read", options =>
    {
        options.PermitLimit = 20;
        options.Window = TimeSpan.FromMinutes(1);
        options.QueueLimit = 0;
    });

    // Dedicated policy for delete operations.
    options.AddFixedWindowLimiter(policyName: "delete", options =>
    {
        options.PermitLimit = 5;
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
                Detail = "Rate limit exceeded. Please try again later.",
                Layer = "API"
            },
            cancellationToken);
    };
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<GhostSend.Infrastructure.Persistence.ApplicationDbContext>();
    db.Database.Migrate();
}

// Warn if default MinIO credentials are used in production
if (!app.Environment.IsDevelopment())
{
    var minioConfig = app.Configuration.GetSection("MinioSettings");
    var accessKey = minioConfig["AccessKey"];
    var secretKey = minioConfig["SecretKey"];
    if (accessKey == "minioadmin" || secretKey == "minioadmin")
    {
        app.Logger.LogWarning("Default MinIO credentials detected in production. Please change them immediately.");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("DefaultCors");
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseRateLimiter();

// app.UseHttpsRedirection();

// Security headers
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("Referrer-Policy", "no-referrer");
    context.Response.Headers.Append("X-Permitted-Cross-Domain-Policies", "none");
    await next();
});

app.UseAuthorization();

app.MapControllers();

// POST /api/upload - Secure upload endpoint (anonymized, streams directly to MinIO)
app.MapPost("/api/upload", async (
    HttpRequest httpRequest,
    IMediator mediator,
    CancellationToken cancellationToken) =>
{
    if (!httpRequest.HasFormContentType)
    {
        return Results.BadRequest(new { Detail = "Multipart form-data content required" });
    }

    var form = await httpRequest.ReadFormAsync(cancellationToken);
    var file = form.Files.GetFile("File");
    if (file == null)
    {
        return Results.BadRequest(new { Detail = "File is required" });
    }

    var request = new UploadFileRequest
    {
        File = file,
        MaxDownloads = int.TryParse(form["MaxDownloads"], out var maxD) ? maxD : null,
        LifeTime = form["LifeTime"]
    };

    var command = request.ToCommand();
    var result = await mediator.Send(command, cancellationToken);

    var response = new UploadFileResponse(result.FileId, result.DeleteToken);
    return Results.Created($"/api/v1/Files/GetMetadata?Id={result.FileId}", response);
})
.RequireRateLimiting("upload");

// GET /api/download/{id} - Secure proxy download endpoint (streams directly from MinIO using Results.Stream)
app.MapGet("/api/download/{id:guid}", async (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
{
    try
    {
        var query = new DownloadFileQuery(id);
        var result = await mediator.Send(query, cancellationToken);
        return Results.Stream(result.Stream, result.ContentType, result.FileName);
    }
    catch (NotFoundException)
    {
        return Results.NotFound(new { Detail = "File not found or expired" });
    }
    catch (GhostSend.Domain.Exceptions.ValidationException ex)
    {
        return Results.BadRequest(new { Detail = ex.Message, Errors = ex.Errors });
    }
})
.RequireRateLimiting("read");

app.Run();

namespace GhostSend.Api
{
    public partial class Program { }
}
