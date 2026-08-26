using Application.Files.Commands;
using Application.Files.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FilesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<FilesController> _logger;

    public FilesController(IMediator mediator, ILogger<FilesController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpPost("upload")]
    public async Task<ActionResult<UploadFileResponse>> UploadFile(IFormFile file, CancellationToken cancellationToken)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded");
            }

            _logger.LogInformation("Uploading file: {FileName}, Size: {FileSize}", file.FileName, file.Length);

            // Read file content
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream, cancellationToken);
            var fileContent = memoryStream.ToArray();
            
            _logger.LogInformation("File content read: {ContentLength} bytes, ContentType: {ContentType}", 
                fileContent.Length, file.ContentType);

            var command = new UploadFileCommand
            {
                FileName = file.FileName,
                ContentType = file.ContentType ?? "application/octet-stream",
                FileSize = file.Length,
                FileContent = fileContent
            };

            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet]
    public async Task<ActionResult<GetFilesResponse>> GetFiles(CancellationToken cancellationToken)
    {
        try
        {
            var query = new GetFilesQuery();
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting files");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<GetFileByIdResponse>> GetFileById(
        Guid id, 
        CancellationToken cancellationToken)
    {
        try
        {
            var query = new GetFileByIdQuery { FileId = id };
            var result = await _mediator.Send(query, cancellationToken);
            
            if (result == null)
                return NotFound(new { error = $"File with ID {id} not found" });
                
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting file {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteFile(
        Guid id, 
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new DeleteFileCommand { FileId = id };
            await _mediator.Send(command, cancellationToken);
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "File not found for deletion: {Id}", id);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }
}
