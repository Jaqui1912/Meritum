using Microsoft.AspNetCore.Mvc;
using Meritum.Core.Entities;

[ApiController]
[Route("api/[controller]")]
public class ProjectsController : ControllerBase
{
    private readonly ProjectsService _projectsService;

    public ProjectsController(ProjectsService projectsService)
    {
        _projectsService = projectsService;
    }

    // GET: api/projects?categoryId=XYZ (Para la lista en la App)
    [HttpGet]
    public async Task<IActionResult> GetProjects([FromQuery] string categoryId)
    {
        var projects = await _projectsService.GetByCategoryAsync(categoryId);
        return Ok(projects);
    }

    // GET: api/projects/{id} (Para el detalle y rúbrica)
    [HttpGet("{id}")]
    public async Task<IActionResult> GetProjectDetail(string id)
    {
        var project = await _projectsService.GetAsync(id);
        if (project == null) return NotFound();
        return Ok(project);
    }

    // POST: api/projects (ESTA ES TU HERRAMIENTA DE ADMIN)
    // Úsalo desde Swagger para subir los proyectos.
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Project newProject)
    {
        await _projectsService.CreateAsync(newProject);
        return Ok(new { message = "Proyecto subido correctamente", id = newProject.Id });
    }
}