using MediatR;

namespace GhostSend.Application.Files.Commands.DeleteFile;

public record DeleteFileCommand(Guid FileId, string DeleteToken) : IRequest;