using Microsoft.AspNetCore.Mvc;
using Meritum.Core.Entities;
using Meritum.Infrastructure.Services;

[ApiController]
[Route("api/[controller]")]
public class ProjectsController : ControllerBase
{
    private readonly ProjectsService _projectsService;
    private readonly CategoriesService _categoriesService; 

    private readonly UsersService _usersService;

    // Pedimos los servicios en el constructor
    public ProjectsController(ProjectsService projectsService, CategoriesService categoriesService, UsersService usersService)
    {
        _projectsService = projectsService;
        _categoriesService = categoriesService;
        _usersService = usersService;
    }

    // filtrado de id por categoria
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? categoryId)
    {
        if (!string.IsNullOrEmpty(categoryId))
        {
            var filteredProjects = await _projectsService.GetByCategoryAsync(categoryId);
            return Ok(filteredProjects);
        }

        var allProjects = await _projectsService.GetAllAsync();
        return Ok(allProjects);
    }

    // GET: api/Projects/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var project = await _projectsService.GetByIdAsync(id);
        if (project == null) return NotFound("Proyecto no encontrado.");
        return Ok(project);
    }

    // POST: Crear Proyecto
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Project newProject, [FromQuery] string adminId)
    {
        var user = await _usersService.GetByIdAsync(adminId);

        // Si el usuario no existe O no es Administrador...
        if (user == null || user.Role != "Administrador")
        {
            return StatusCode(403, new { message = "Acceso Denegado. Solo el Administrador puede crear proyectos." });
        }
        // ¿Existe la categoría ?
        var categoryExists = await _categoriesService.GetByIdAsync(newProject.CategoryId);
        if (categoryExists == null)
        {
            return BadRequest(new { message = "Error: La categoría especificada no existe. No se pueden crear proyectos huérfanos." });
        }

        // ¿Ya existe el proyecto con ese nombre?
        var existingProject = await _projectsService.GetByNameAsync(newProject.Title);
        if (existingProject != null)
        {
            return Conflict(new { message = $"El proyecto '{newProject.Title}' ya está registrado." });
        }

        await _projectsService.CreateAsync(newProject);
        return Ok(new { message = "Proyecto creado exitosamente", id = newProject.Id });
    }

    // editar Proyecto
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] Project updatedProject, [FromQuery] string adminId)
    {
        var user = await _usersService.GetByIdAsync(adminId);

        // Si el usuario no existe O no es Administrador...
        if (user == null || user.Role != "Administrador")
        {
            return StatusCode(403, new { message = "Acceso Denegado. Solo el Administrador puede editar proyectos." });
        }
        var existing = await _projectsService.GetByIdAsync(id);
        if (existing == null) return NotFound("Proyecto no encontrado.");

        // Validar si cambiaron la categoría, que la nueva exista
        var categoryExists = await _categoriesService.GetByIdAsync(updatedProject.CategoryId);
        if (categoryExists == null)
        {
            return BadRequest(new { message = "La categoría asignada no existe." });
        }

        updatedProject.Id = existing.Id; // Proteger el ID original
        await _projectsService.UpdateAsync(id, updatedProject);
        
        return Ok(new { message = "Proyecto actualizado correctamente." });
    }

    //eliminar Proyecto
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, [FromQuery] string adminId)
    {
        var user = await _usersService.GetByIdAsync(adminId);

        // Si el usuario no existe O no es Administrador...
        if (user == null || user.Role != "Administrador")
        {
            return StatusCode(403, new { message = "Acceso Denegado. Solo el Administrador puede eliminar proyectos." });
        }
        var existing = await _projectsService.GetByIdAsync(id);
        if (existing == null) return NotFound("Proyecto no encontrado.");

        await _projectsService.RemoveAsync(id);
        return Ok(new { message = "Proyecto eliminado." });
    }
}