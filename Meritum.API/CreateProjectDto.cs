using Microsoft.AspNetCore.Http;

namespace Meritum.API.Controllers;

public class CreateProjectDto
{
    public string CategoryId { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string? TeamMembers { get; set; }

    // 📸 1. NUEVO: El archivo de la imagen de portada
    public IFormFile? ImageFile { get; set; }

    // El video
    public IFormFile? VideoFile { get; set; } 

    // Los documentos (Lista)
    public List<IFormFile>? DocumentFiles { get; set; } 
}