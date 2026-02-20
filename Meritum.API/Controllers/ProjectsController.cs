using Microsoft.AspNetCore.Mvc;
using Meritum.Core.Entities;
using Meritum.Infrastructure.Services;

namespace Meritum.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProjectsController : ControllerBase
{
    private readonly ProjectsService _projectsService;
    private readonly CategoriesService _categoriesService;
    private readonly UsersService _usersService;
    private readonly FileStorageService _fileStorage;

    public ProjectsController(
        ProjectsService projectsService,
        CategoriesService categoriesService,
        UsersService usersService,
        FileStorageService fileStorage)
    {
        _projectsService = projectsService;
        _categoriesService = categoriesService;
        _usersService = usersService;
        _fileStorage = fileStorage;
    }

    // Listar todos o filtrar/buscar
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? categoryId, [FromQuery] string? searchTerm)
    {
        // Direccion de la api
        var baseUrl = $"{Request.Scheme}://{Request.Host}";

        // ¡Llamamos al servicio pasándole AMBOS filtros!
        var projects = await _projectsService.GetAllAsync(categoryId, searchTerm);

        // Transformamos las URLs (tu código que ya funciona perfecto)
        foreach (var p in projects)
        {
            if (!string.IsNullOrEmpty(p.ImageUrl)) p.ImageUrl = baseUrl + p.ImageUrl;
            if (!string.IsNullOrEmpty(p.VideoUrl)) p.VideoUrl = baseUrl + p.VideoUrl;
            if (p.DocumentUrls != null && p.DocumentUrls.Count > 0)
            {
                var fullLinks = new List<string>();
                foreach (var doc in p.DocumentUrls) fullLinks.Add(baseUrl + doc);
                p.DocumentUrls = fullLinks;
            }
        }

        return Ok(projects);
    }

    // GET: Por ID
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var project = await _projectsService.GetByIdAsync(id);
        if (project == null) return NotFound("Proyecto no encontrado.");

        // 1. Obtenemos la dirección base
        var baseUrl = $"{Request.Scheme}://{Request.Host}";

        // 2. Transformamos las URLs para este único proyecto
        if (!string.IsNullOrEmpty(project.ImageUrl))
            project.ImageUrl = baseUrl + project.ImageUrl;

        if (!string.IsNullOrEmpty(project.VideoUrl))
            project.VideoUrl = baseUrl + project.VideoUrl;

        if (project.DocumentUrls != null && project.DocumentUrls.Count > 0)
        {
            var fullLinks = new List<string>();
            foreach (var doc in project.DocumentUrls)
            {
                fullLinks.Add(baseUrl + doc);
            }
            project.DocumentUrls = fullLinks;
        }

        return Ok(project);
    }

    // POST: Crear Proyecto (CON IMAGEN, VIDEO Y DOCS)
    [HttpPost]
    public async Task<IActionResult> Create([FromForm] CreateProjectDto dto, [FromQuery] string adminId)
    {
        // A. VALIDACIÓN DE SEGURIDAD
        var user = await _usersService.GetByIdAsync(adminId);
        if (user == null || user.Role != "Administrador")
        {
            return StatusCode(403, new { message = "Acceso Denegado. Solo el Administrador puede crear proyectos." });
        }

        // B. VALIDACIONES DE NEGOCIO
        var categoryExists = await _categoriesService.GetByIdAsync(dto.CategoryId);
        if (categoryExists == null) return BadRequest(new { message = "Error: La categoría especificada no existe." });

        var existingProject = await _projectsService.GetByNameAsync(dto.Title);
        if (existingProject != null) return Conflict(new { message = $"El proyecto '{dto.Title}' ya está registrado." });

        // C. SUBIDA DE ARCHIVOS 📂

        // 1. IMAGEN DE PORTADA 📸
        string imageUrl = "";
        if (dto.ImageFile != null)
        {
            imageUrl = await _fileStorage.SaveFileAsync(dto.ImageFile, "images");
        }

        // 2. VIDEO
        string videoUrl = "";
        if (dto.VideoFile != null)
        {
            videoUrl = await _fileStorage.SaveFileAsync(dto.VideoFile, "videos");
        }

        // 3. DOCUMENTOS (Lista)
        List<string> uploadedDocs = new List<string>();
        if (dto.DocumentFiles != null && dto.DocumentFiles.Count > 0)
        {
            foreach (var doc in dto.DocumentFiles)
            {
                var url = await _fileStorage.SaveFileAsync(doc, "documents");
                uploadedDocs.Add(url);
            }
        }

        // D. CREAR OBJETO
        var newProject = new Project
        {
            CategoryId = dto.CategoryId,
            Title = dto.Title,
            Description = dto.Description,
            TeamMembers = dto.TeamMembers,

            // Asignamos las rutas
            ImageUrl = imageUrl,       // <--- Nueva Imagen
            VideoUrl = videoUrl,       // Video
            DocumentUrls = uploadedDocs // Documentos en plural
        }; // <--- ¡AQUÍ TE FALTABA CERRAR LA LLAVE ANTES!

        await _projectsService.CreateAsync(newProject);
        return Ok(new { message = "Proyecto creado exitosamente", id = newProject.Id });
    }

    // PUT: Editar Proyecto
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] Project updatedProject, [FromQuery] string adminId)
    {
        var user = await _usersService.GetByIdAsync(adminId);
        if (user == null || user.Role != "Administrador")
        {
            return StatusCode(403, new { message = "Acceso Denegado." });
        }

        var existing = await _projectsService.GetByIdAsync(id);
        if (existing == null) return NotFound("Proyecto no encontrado.");

        var categoryExists = await _categoriesService.GetByIdAsync(updatedProject.CategoryId);
        if (categoryExists == null) return BadRequest(new { message = "La categoría no existe." });

        updatedProject.Id = existing.Id;

        // Mantenemos los archivos viejos si no estamos editándolos
        updatedProject.ImageUrl = existing.ImageUrl;         // <--- Mantiene Imagen
        updatedProject.VideoUrl = existing.VideoUrl;         // <--- Mantiene Video
        updatedProject.DocumentUrls = existing.DocumentUrls; // <--- Mantiene Docs (PLURAL)

        await _projectsService.UpdateAsync(id, updatedProject);

        return Ok(new { message = "Proyecto actualizado correctamente." });
    }

    // DELETE: Eliminar
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, [FromQuery] string adminId)
    {
        var user = await _usersService.GetByIdAsync(adminId);
        if (user == null || user.Role != "Administrador")
        {
            return StatusCode(403, new { message = "Acceso Denegado." });
        }
        var existing = await _projectsService.GetByIdAsync(id);
        if (existing == null) return NotFound("Proyecto no encontrado.");

        await _projectsService.RemoveAsync(id);
        return Ok(new { message = "Proyecto eliminado." });
    }
}

