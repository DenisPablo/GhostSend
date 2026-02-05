using FluentValidation;

namespace GhostSend.Application.Files.Queries.DownloadFile;

public class DownloadFileQueryValidator : AbstractValidator<DownloadFileQuery>
{
    public DownloadFileQueryValidator()
    {
        RuleFor(x => x.FileId)
            .NotEmpty()
            .WithMessage("File ID is required.");
    }
}
