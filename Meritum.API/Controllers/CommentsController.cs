using Microsoft.AspNetCore.Mvc;
using Meritum.Core.Entities;
using Meritum.Infrastructure.Services;

namespace Meritum.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CommentsController : ControllerBase
{
    private readonly CommentsService _commentsService;

    public CommentsController(CommentsService commentsService)
    {
        _commentsService = commentsService;
    }

    [HttpGet("project/{projectId}")]
    public async Task<IActionResult> GetByProject(string projectId)
    {
        var comments = await _commentsService.GetByProjectIdAsync(projectId);
        return Ok(comments);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Comment newComment)
    {
        if (string.IsNullOrEmpty(newComment.ProjectId) || string.IsNullOrEmpty(newComment.UserId))
        {
            return BadRequest(new { message = "ProjectId y UserId son obligatorios." });
        }

        newComment.CreatedAt = DateTime.UtcNow;
        await _commentsService.CreateAsync(newComment);

        return Ok(newComment);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        await _commentsService.RemoveAsync(id);
        return Ok(new { message = "Comentario eliminado." });
    }
}
