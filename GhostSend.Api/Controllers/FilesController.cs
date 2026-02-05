using GhostSend.Api.DTOs;
using GhostSend.Application.Files.Commands.DeleteFile;
using GhostSend.Application.Files.Commands.UploadFile;
using GhostSend.Application.Files.Queries.DownloadFile;
using GhostSend.Application.Files.Queries.GetFileMetadata;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GhostSend.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class FilesController(IMediator mediator) : ControllerBase
{

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [DisableRequestSizeLimit]
    public async Task<IActionResult> UploadFile([FromForm] UploadFileRequest request, CancellationToken cancellationToken)
    {
        var command = request.ToCommand();
        var result = await mediator.Send(command, cancellationToken);

        return Ok(new { result.FileId, result.DeleteToken });
    }

    [HttpGet("GetFile")]
    public async Task<IActionResult> GetFile(FileDownloadRequest request, CancellationToken cancellationToken)
    {
        var query = new DownloadFileQuery(request.Id);
        var result = await mediator.Send(query, cancellationToken);

        return File(result.Stream, result.ContentType, result.FileName);
    }

    [HttpGet("GetMetadata")]
    public async Task<IActionResult> GetMetadata(FileMetadataRequest request, CancellationToken cancellationToken)
    {
        var query = new GetFileMetadataQuery(request.Id);
        var result = await mediator.Send(query, cancellationToken);

        var response = new FileMetadataResponse(
           result.Id,
           result.FileName,
           result.ContentType,
           result.CurrentDownloads,
           result.UploadDate,
           result.ExpirationDate,
           result.MaxDownloads
        );

        return Ok(response);
    }

    [HttpDelete("Delete")]
    public async Task<IActionResult> DeleteFile(FileDeleteRequest request, CancellationToken cancellationToken)
    {
        var command = new DeleteFileCommand(request.Id, request.DeleteToken);
        await mediator.Send(command, cancellationToken);

        return NoContent();
    }
}