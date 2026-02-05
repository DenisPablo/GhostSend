using FluentValidation;

namespace GhostSend.Application.Files.Commands.DeleteFile;

public class DeleteFileCommandValidator : AbstractValidator<DeleteFileCommand>
{
    public DeleteFileCommandValidator()
    {
        RuleFor(x => x.FileId)
            .NotEmpty()
            .WithMessage("File ID is required.");

        RuleFor(x => x.DeleteToken)
            .NotEmpty()
            .WithMessage("Delete token is required.");
    }
}
