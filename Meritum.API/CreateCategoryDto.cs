using Microsoft.AspNetCore.Http;

namespace Meritum.API.Controllers; 
public class CreateCategoryDto
{
    public string Name { get; set; } = null!;
    
    // Campo para guardar la clase de BoxIcons desde la web
    public string? IconUrl { get; set; }

    // Campo para subir el archivo real desde la compu/celular
    public IFormFile? IconFile { get; set; } 
}