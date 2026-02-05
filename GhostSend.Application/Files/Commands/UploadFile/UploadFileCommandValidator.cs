using FluentValidation;
using GhostSend.Application.Common.Settings;
using Microsoft.Extensions.Options;

namespace GhostSend.Application.Files.Commands.UploadFile;

public class UploadFileCommandValidator : AbstractValidator<UploadFileCommand>
{
    public UploadFileCommandValidator(IOptions<FileSettings> fileSettings)
    {
        var settings = fileSettings.Value;

        RuleFor(x => x.Stream)
            .NotNull()
            .WithMessage("The file is required.");

        RuleFor(x => x.FileName)
            .NotEmpty()
            .WithMessage("The file name is required.");

        RuleFor(x => x.ContentType)
            .NotEmpty()
            .WithMessage("The content type is required.");

        RuleFor(x => x.Size)
            .GreaterThan(0)
            .WithMessage("The size must be greater than 0.");

        RuleFor(x => x.Size)
            .LessThanOrEqualTo(settings.MaxFileSizeInBytes)
            .WithMessage($"The size must be less than or equal to {settings.MaxFileSizeDescription}.");

        RuleFor(x => x.MaxDownloads)
            .GreaterThan(0)
            .WithMessage("The max downloads must be greater than 0.");

        RuleFor(x => x.LifeTime)
            .Must(lifeTime => lifeTime > TimeSpan.Zero)
            .When(x => x.LifeTime.HasValue)
            .WithMessage("The life time must be greater than 0.");
    }
}
