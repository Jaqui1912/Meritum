using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace Meritum.API.Controllers;

public class UpdateProjectDto
{
    public string CategoryId { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public List<string>? Technologies { get; set; }
    public string? TeamMembers { get; set; }

    public IFormFile? ImageFile { get; set; }
    public IFormFile? PreviewVideoFile { get; set; }  // Video corto de presentación
    public List<IFormFile>? VideoFiles { get; set; }
    public List<string>? ExternalVideoUrls { get; set; }
    public List<IFormFile>? DocumentFiles { get; set; } 

    // Banderas para borrar archivos existentes
    public bool RemoveExistingImage { get; set; }
    public bool RemoveExistingPreviewVideo { get; set; }
    public string? KeptVideoUrls { get; set; }    // URLs de videos a conservar, separadas por comas
    public string? KeptDocumentUrls { get; set; }  // URLs de docs a conservar, separadas por comas
}
