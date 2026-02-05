namespace GhostSend.Api.DTOs.Requests;

public record FileDeleteRequest(
  Guid Id,
  string DeleteToken
);