using FluentValidation;

namespace GhostSend.Application.Files.Queries.GetFileMetadata;

public class GetFileMetadataQueryValidator : AbstractValidator<GetFileMetadataQuery>
{
    public GetFileMetadataQueryValidator()
    {
        RuleFor(x => x.FileId)
            .NotEmpty()
            .WithMessage("File ID is required.");
    }
}
