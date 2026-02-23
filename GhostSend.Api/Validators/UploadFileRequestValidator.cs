using FluentValidation;
using GhostSend.Api.DTOs.Requests;
using GhostSend.Application.Common.Settings;
using Microsoft.Extensions.Options;

namespace GhostSend.Api.Validators;

public class UploadFileRequestValidator : AbstractValidator<UploadFileRequest>
{
    public UploadFileRequestValidator(IOptions<FileSettings> fileSettings)
    {
        var settings = fileSettings.Value;

        RuleFor(x => x.File)
            .NotNull().WithMessage("The file is required.");

        When(x => x.File != null, () =>
        {
            RuleFor(x => x.File.Length)
                .GreaterThan(0).WithMessage("The file cannot be empty.")
                .LessThanOrEqualTo(settings.MaxFileSizeInBytes)
                .WithMessage($"The file size exceeds the allowed limit of {settings.MaxFileSizeDescription}.");

            RuleFor(x => x.File.FileName)
                .NotEmpty().WithMessage("The file name is required.");
        });

        RuleFor(x => x.MaxDownloads)
            .GreaterThan(0).When(x => x.MaxDownloads.HasValue)
            .WithMessage("Max downloads must be greater than 0.");

        RuleFor(x => x.LifeTime)
            .Must(lifeTime => TimeSpan.TryParse(lifeTime, out var ts) && ts > TimeSpan.Zero)
            .When(x => !string.IsNullOrWhiteSpace(x.LifeTime))
            .WithMessage("LifeTime must be a valid time format (e.g., '1.00:00:00' for 1 day) and greater than 0.");
    }
}
