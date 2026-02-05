namespace GhostSend.Api.DTOs.Responses;

public record UploadFileResponse(Guid FileId, string DeleteToken);