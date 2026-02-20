namespace Meritum.Infrastructure.Services;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.IO;       // Necesario para Path y FileStream
using System.Threading.Tasks; // Necesario para Task

public class FileStorageService
{
    private readonly IWebHostEnvironment _env;

    public FileStorageService(IWebHostEnvironment env)
    {
        _env = env;
    }

    public async Task<string> SaveFileAsync(IFormFile file, string folderName)
    {
        if (file == null || file.Length == 0) return null;

        // Lógica blindada para encontrar la carpeta wwwroot
        string webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        
        // Define la ruta: wwwroot/uploads/videos (o images, o documents)
        string uploadsFolder = Path.Combine(webRoot, "uploads", folderName);
        
        // Crea la carpeta si no existe
        if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

        // Genera nombre único
        string uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

        // Guarda el archivo
        using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(fileStream);
        }

        // Retorna la URL para la base de datos
        return $"/uploads/{folderName}/{uniqueFileName}";
    }
}