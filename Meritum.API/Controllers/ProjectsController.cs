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
            p.MigrateVideoUrl(); // Backward compat: videoUrl -> videoUrls
            if (!string.IsNullOrEmpty(p.ImageUrl)) p.ImageUrl = baseUrl + p.ImageUrl;

            // Multi-video: transformar cada URL de video
            if (p.VideoUrls != null && p.VideoUrls.Count > 0)
            {
                var fullVideos = new List<string>();
                foreach (var v in p.VideoUrls)
                {
                    if (v.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || v.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                        fullVideos.Add(v);
                    else
                        fullVideos.Add(baseUrl + v);
                }
                p.VideoUrls = fullVideos;
            }

            if (p.DocumentUrls != null && p.DocumentUrls.Count > 0)
            {
                var fullLinks = new List<string>();
                foreach (var doc in p.DocumentUrls) fullLinks.Add(baseUrl + doc);
                p.DocumentUrls = fullLinks;
            }

            if (!string.IsNullOrEmpty(p.PreviewVideoUrl)) p.PreviewVideoUrl = baseUrl + p.PreviewVideoUrl;
        }

        return Ok(projects);
    }

    // GET: Por ID
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var project = await _projectsService.GetByIdAsync(id);
        if (project == null) return NotFound("Proyecto no encontrado.");

        project.MigrateVideoUrl(); // Backward compat

        // 1. Obtenemos la dirección base
        var baseUrl = $"{Request.Scheme}://{Request.Host}";

        // 2. Transformamos las URLs para este único proyecto
        if (!string.IsNullOrEmpty(project.ImageUrl))
            project.ImageUrl = baseUrl + project.ImageUrl;

        // Multi-video
        if (project.VideoUrls != null && project.VideoUrls.Count > 0)
        {
            var fullVideos = new List<string>();
            foreach (var v in project.VideoUrls)
            {
                if (v.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || v.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    fullVideos.Add(v);
                else
                    fullVideos.Add(baseUrl + v);
            }
            project.VideoUrls = fullVideos;
        }

        if (project.DocumentUrls != null && project.DocumentUrls.Count > 0)
        {
            var fullLinks = new List<string>();
            foreach (var doc in project.DocumentUrls)
            {
                fullLinks.Add(baseUrl + doc);
            }
            project.DocumentUrls = fullLinks;
        }

        if (!string.IsNullOrEmpty(project.PreviewVideoUrl))
            project.PreviewVideoUrl = baseUrl + project.PreviewVideoUrl;

        return Ok(project);
    }

    // POST: Crear Proyecto (CON IMAGEN, VIDEOS Y DOCS)
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

        // 2. VIDEOS (Lista - soporta múltiples)
        List<string> uploadedVideos = new List<string>();
        if (dto.VideoFiles != null && dto.VideoFiles.Count > 0)
        {
            foreach (var video in dto.VideoFiles)
            {
                var url = await _fileStorage.SaveFileAsync(video, "videos");
                uploadedVideos.Add(url);
            }
        }

        // Agregar videos externos de YouTube/Drive
        if (dto.ExternalVideoUrls != null && dto.ExternalVideoUrls.Count > 0)
        {
            uploadedVideos.AddRange(dto.ExternalVideoUrls);
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

        // 4. VIDEO DE PRESENTACIÓN (~10s preview)
        string previewVideoUrl = "";
        if (dto.PreviewVideoFile != null)
        {
            previewVideoUrl = await _fileStorage.SaveFileAsync(dto.PreviewVideoFile, "previews");
        }

        // D. CREAR OBJETO
        var newProject = new Project
        {
            CategoryId = dto.CategoryId,
            Title = dto.Title,
            Description = dto.Description ?? "",
            TeamMembers = dto.TeamMembers ?? "",

            // Asignamos las rutas
            ImageUrl = imageUrl,
            PreviewVideoUrl = previewVideoUrl,
            VideoUrls = uploadedVideos,    // Multi-video
            DocumentUrls = uploadedDocs
        };

        await _projectsService.CreateAsync(newProject);
        return Ok(new { message = "Proyecto creado exitosamente", id = newProject.Id });
    }

    // PUT: Editar Proyecto
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromForm] UpdateProjectDto dto, [FromQuery] string adminId)
    {
        var user = await _usersService.GetByIdAsync(adminId);
        if (user == null || user.Role != "Administrador")
        {
            return StatusCode(403, new { message = "Acceso Denegado." });
        }

        var existing = await _projectsService.GetByIdAsync(id);
        if (existing == null) return NotFound("Proyecto no encontrado.");

        var categoryExists = await _categoriesService.GetByIdAsync(dto.CategoryId);
        if (categoryExists == null) return BadRequest(new { message = "La categoría no existe." });

        existing.Title = dto.Title;
        existing.CategoryId = dto.CategoryId;
        existing.Description = dto.Description ?? "";
        existing.TeamMembers = dto.TeamMembers ?? "";

        if (dto.ImageFile != null)
        {
            existing.ImageUrl = await _fileStorage.SaveFileAsync(dto.ImageFile, "images");
        }
        else if (dto.RemoveExistingImage)
        {
            existing.ImageUrl = "";
        }

        // Preview video
        if (dto.PreviewVideoFile != null)
        {
            existing.PreviewVideoUrl = await _fileStorage.SaveFileAsync(dto.PreviewVideoFile, "previews");
        }
        else if (dto.RemoveExistingPreviewVideo)
        {
            existing.PreviewVideoUrl = "";
        }

        // Multi-video: conservar videos existentes + agregar nuevos
        var newVideos = new List<string>();

        if (existing.VideoUrls != null && !string.IsNullOrEmpty(dto.KeptVideoUrls))
        {
            var kept = dto.KeptVideoUrls.Split(',').Select(k => k.Trim()).ToList();
            foreach (var vid in existing.VideoUrls)
            {
                if (kept.Any(k => k.EndsWith(vid) || vid.EndsWith(k)))
                {
                    newVideos.Add(vid);
                }
            }
        }

        if (dto.VideoFiles != null && dto.VideoFiles.Count > 0)
        {
            foreach (var video in dto.VideoFiles)
            {
                var url = await _fileStorage.SaveFileAsync(video, "videos");
                newVideos.Add(url);
            }
        }

        // Agregar videos externos nuevos en Update
        if (dto.ExternalVideoUrls != null && dto.ExternalVideoUrls.Count > 0)
        {
            newVideos.AddRange(dto.ExternalVideoUrls);
        }

        existing.VideoUrls = newVideos;

        // Documentos: conservar existentes + agregar nuevos
        var newDocs = new List<string>();

        if (existing.DocumentUrls != null && !string.IsNullOrEmpty(dto.KeptDocumentUrls))
        {
            var kept = dto.KeptDocumentUrls.Split(',').Select(k => k.Trim()).ToList();
            foreach (var doc in existing.DocumentUrls)
            {
                if (kept.Any(k => k.EndsWith(doc) || doc.EndsWith(k))) 
                {
                    newDocs.Add(doc);
                }
            }
        }

        if (dto.DocumentFiles != null && dto.DocumentFiles.Count > 0)
        {
            foreach (var doc in dto.DocumentFiles)
            {
                var url = await _fileStorage.SaveFileAsync(doc, "documents");
                newDocs.Add(url);
            }
        }

        existing.DocumentUrls = newDocs;

        await _projectsService.UpdateAsync(id, existing);

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

