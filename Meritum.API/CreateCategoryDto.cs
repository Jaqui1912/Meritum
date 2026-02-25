using Microsoft.AspNetCore.Http;

namespace Meritum.API.Controllers; 
public class CreateCategoryDto
{
    public string Name { get; set; } = null!;
    
    // Campo para subir el archivo real desde la compu/celular
    public IFormFile? IconFile { get; set; } 
}