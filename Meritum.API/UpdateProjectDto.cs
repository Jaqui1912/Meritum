using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace Meritum.API.Controllers;

public class UpdateProjectDto
{
    public string CategoryId { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string? TeamMembers { get; set; }

    public IFormFile? ImageFile { get; set; }
    public IFormFile? VideoFile { get; set; } 
    public List<IFormFile>? DocumentFiles { get; set; } 

    // Banderas para borrar archivos existentes
    public bool RemoveExistingImage { get; set; }
    public bool RemoveExistingVideo { get; set; }
    public string? KeptDocumentUrls { get; set; } // URLs separadas por comas
}
