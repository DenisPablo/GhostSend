namespace GhostSend.Api.DTOs;

public record FileDeleteRequest(
  Guid Id,
  string DeleteToken
);