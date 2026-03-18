using Microsoft.AspNetCore.Http;

namespace Meritum.API.Controllers;

public class CreateProjectDto
{
    public string CategoryId { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string? TeamMembers { get; set; }
    public string? Technologies { get; set; }

    // 📸 1. NUEVO: El archivo de la imagen de portada
    public IFormFile? ImageFile { get; set; }

    // 🎬 Video corto de presentación (~10 segundos)
    public IFormFile? PreviewVideoFile { get; set; }

    // Los videos (Lista - soporta múltiples videos)
    public List<IFormFile>? VideoFiles { get; set; } 
    public List<string>? ExternalVideoUrls { get; set; }

    // Los documentos (Lista)
    public List<IFormFile>? DocumentFiles { get; set; } 
}