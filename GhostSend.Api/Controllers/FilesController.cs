using GhostSend.Api.DTOs.Requests;
using GhostSend.Api.DTOs.Responses;
using GhostSend.Application.Files.Commands.DeleteFile;
using GhostSend.Application.Files.Commands.UploadFile;
using GhostSend.Application.Files.Queries.DownloadFile;
using GhostSend.Application.Files.Queries.GetFileMetadata;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using UploadFileResponse = GhostSend.Api.DTOs.Responses.UploadFileResponse;

namespace GhostSend.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class FilesController(IMediator mediator) : ControllerBase
{

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [DisableRequestSizeLimit]
    [EnableRateLimiting("fixed")]
    public async Task<IActionResult> UploadFile([FromForm] UploadFileRequest request, CancellationToken cancellationToken)
    {
        var command = request.ToCommand();
        var result = await mediator.Send(command, cancellationToken);

        var response = new UploadFileResponse(result.FileId, result.DeleteToken);

        return CreatedAtAction(nameof(GetMetadata), response);
    }

    [HttpGet("GetFile")]
    [EnableRateLimiting("fixed")]
    public async Task<IActionResult> GetFile([FromQuery] FileDownloadRequest request, CancellationToken cancellationToken)
    {
        var query = new DownloadFileQuery(request.Id);
        var result = await mediator.Send(query, cancellationToken);

        return File(result.Stream, result.ContentType, result.FileName);
    }

    [HttpGet("GetMetadata")]
    [EnableRateLimiting("fixed")]
    public async Task<IActionResult> GetMetadata([FromQuery] FileMetadataRequest request, CancellationToken cancellationToken)
    {
        var query = new GetFileMetadataQuery(request.Id);
        var result = await mediator.Send(query, cancellationToken);

        var response = new FileMetadataResponse(
        result.Id,
        result.FileName,
        result.ContentType,
        result.Size,
        result.MaxDownloads,
        result.CurrentDownloads,
        result.UploadDate,
        result.ExpirationDate,
        result.ExpirationDate.HasValue ? (result.ExpirationDate.Value - result.UploadDate).ToString(@"hh\:mm\:ss") : null
        );

        return Ok(response);
    }

    [HttpDelete("Delete")]
    [EnableRateLimiting("fixed")]
    public async Task<IActionResult> DeleteFile([FromQuery] FileDeleteRequest request, CancellationToken cancellationToken)
    {
        var command = new DeleteFileCommand(request.Id, request.DeleteToken);
        await mediator.Send(command, cancellationToken);

        return NoContent();
    }
}
