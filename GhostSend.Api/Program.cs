using GhostSend.Api.Middleware;
using GhostSend.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Infrastructure DI
builder.Services.AddInfrastructure(builder.Configuration);

// Application DI (MediatR)
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(GhostSend.Application.Files.Commands.UploadFile.UploadFileCommand).Assembly));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
