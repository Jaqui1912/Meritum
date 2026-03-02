using Microsoft.AspNetCore.Mvc;
using Meritum.Core.Entities;
using Meritum.Infrastructure.Services;

namespace Meritum.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CommentsController : ControllerBase
{
    private readonly CommentsService _commentsService;
    private readonly ProjectsService _projectsService;

    public CommentsController(CommentsService commentsService, ProjectsService projectsService)
    {
        _commentsService = commentsService;
        _projectsService = projectsService;
    }

    // POST: /api/Comments
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Comment newComment)
    {
        var project = await _projectsService.GetByIdAsync(newComment.ProjectId);
        if (project == null) return BadRequest(new { message = "El proyecto a comentar no existe." });

        // Aseguramos que se guarde con el timestamp exacto del servidor.
        newComment.CreatedAt = DateTime.UtcNow;

        await _commentsService.CreateAsync(newComment);

        return Ok(new { message = "Comentario registrado exitosamente.", comment = newComment });
    }

    // GET: /api/Comments/project/{projectId}
    [HttpGet("project/{projectId}")]
    public async Task<IActionResult> GetByProject(string projectId)
    {
        var project = await _projectsService.GetByIdAsync(projectId);
        if (project == null) return NotFound(new { message = "El proyecto no existe." });

        var comments = await _commentsService.GetByProjectIdAsync(projectId);
        return Ok(comments);
    }
}
