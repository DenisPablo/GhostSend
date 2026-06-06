using System.Text.RegularExpressions;
using FluentValidation;
using GhostSend.Application.Common.Settings;
using GhostSend.Domain.Errors;
using Microsoft.Extensions.Options;

namespace GhostSend.Application.Files.Commands.UploadFile;

public partial class UploadFileCommandValidator : AbstractValidator<UploadFileCommand>
{
    private static readonly Regex InvalidFileNameChars = InvalidFileNameRegex();
    private static readonly HashSet<string> AllowedContentTypes =
    [
        "application/pdf",
        "application/zip",
        "application/gzip",
        "application/x-rar-compressed",
        "application/x-7z-compressed",
        "application/x-tar",
        "application/json",
        "application/xml",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        "image/jpeg",
        "image/png",
        "image/gif",
        "image/webp",
        "image/svg+xml",
        "video/mp4",
        "video/webm",
        "video/x-matroska",
        "audio/mpeg",
        "audio/ogg",
        "audio/wav",
        "text/plain",
        "text/csv",
    ];

    [GeneratedRegex(@"[<>:/\\|?*\x00-\x1f]")]
    private static partial Regex InvalidFileNameRegex();

    public UploadFileCommandValidator(IOptions<FileSettings> fileSettings)
    {
        var settings = fileSettings.Value;

        RuleFor(x => x.Stream)
            .Must(s => s != null && s != Stream.Null)
            .WithMessage("The file is required.");

        RuleFor(x => x.FileName)
            .NotEmpty()
            .WithMessage("The file name is required.")
            .MaximumLength(255)
            .WithMessage("The file name must not exceed 255 characters.")
            .Must(name => !InvalidFileNameChars.IsMatch(name ?? string.Empty))
            .WithMessage(DomainErrors.StoredFile.FileNameInvalidCharacters);

        RuleFor(x => x.ContentType)
            .NotEmpty()
            .WithMessage("The content type is required.")
            .Must(ct => AllowedContentTypes.Contains(ct ?? string.Empty))
            .When(_ => false) // Disabled: E2EE means the real content type is unknown; user-declared type is advisory
            .WithMessage(DomainErrors.StoredFile.ContentTypeNotAllowed);

        RuleFor(x => x.Size)
            .GreaterThan(0)
            .WithMessage("The size must be greater than 0.");

        RuleFor(x => x.Size)
            .LessThanOrEqualTo(settings.MaxFileSizeInBytes)
            .WithMessage($"The size must be less than or equal to {settings.MaxFileSizeDescription}.");

        RuleFor(x => x.MaxDownloads)
            .GreaterThan(0).WithMessage("The max downloads must be greater than 0.")
            .LessThanOrEqualTo(10_000).WithMessage("The max downloads cannot exceed 10,000.")
            .When(x => x.MaxDownloads.HasValue);

        RuleFor(x => x.LifeTime)
            .Must(lifeTime => lifeTime > TimeSpan.Zero)
            .When(x => x.LifeTime.HasValue)
            .WithMessage("The life time must be greater than 0.");

        RuleFor(x => x.LifeTime)
            .Must(lifeTime => lifeTime!.Value.TotalHours <= settings.MaxLifetimeInHours)
            .When(x => x.LifeTime.HasValue)
            .WithMessage($"The life time must not exceed {settings.MaxLifetimeInHours} hours.");
    }
}
