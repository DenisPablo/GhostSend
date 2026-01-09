using GhostSend.Api.DTOs;
using GhostSend.Application.Files.Commands.UploadFile;
using GhostSend.Application.Files.Queries.GetFile;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GhostSend.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class FilesController(IMediator mediator) : ControllerBase
{

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadFile([FromForm] UploadFileRequest request, CancellationToken cancellationToken)
    {
        var command = request.ToCommand();
        var result = await mediator.Send(command, cancellationToken);

        return Ok(new { Id = result });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetFile(Guid id, CancellationToken cancellationToken)
    {
        var query = new DownloadFileQuery(id);
        var result = await mediator.Send(query, cancellationToken);

        return File(result.Stream, result.ContentType, result.FileName);
    }
}