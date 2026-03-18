namespace Meritum.Infrastructure.Services;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using System;
using System.IO;
using System.Threading.Tasks;

public class FileStorageService
{
    private readonly IWebHostEnvironment _env;
    private readonly Cloudinary? _cloudinary;

    public FileStorageService(IWebHostEnvironment env, IConfiguration config)
    {
        _env = env;
        
        // Configurar Cloudinary
        var cloudName = config["Cloudinary:CloudName"];
        var apiKey = config["Cloudinary:ApiKey"];
        var apiSecret = config["Cloudinary:ApiSecret"];

        if (!string.IsNullOrEmpty(cloudName) && !string.IsNullOrEmpty(apiKey) && !string.IsNullOrEmpty(apiSecret))
        {
            var account = new Account(cloudName, apiKey, apiSecret);
            _cloudinary = new Cloudinary(account);
        }
    }

    public async Task<string> SaveFileAsync(IFormFile file, string folderName)
    {
        if (file == null || file.Length == 0) return string.Empty;

        // Si Cloudinary está configurado, subir allí
        if (_cloudinary != null)
        {
            using var stream = file.OpenReadStream();
            
            // Decidir tipo de archivo según la carpeta (videos, images, documents, previews)
            bool isVideo = folderName.ToLower().Contains("video") || folderName.ToLower().Contains("preview");
            bool isImage = folderName.ToLower().Contains("image");
            
            if (isVideo)
            {
                var uploadParams = new VideoUploadParams()
                {
                    File = new FileDescription(file.FileName, stream),
                    Folder = $"meritum_uploads/{folderName}"
                };
                var uploadResult = await _cloudinary.UploadAsync(uploadParams);
                return uploadResult.SecureUrl?.AbsoluteUri ?? string.Empty;
            }
            else if (isImage)
            {
                var uploadParams = new ImageUploadParams()
                {
                    File = new FileDescription(file.FileName, stream),
                    Folder = $"meritum_uploads/{folderName}"
                };
                var uploadResult = await _cloudinary.UploadAsync(uploadParams);
                return uploadResult.SecureUrl?.AbsoluteUri ?? string.Empty;
            }
            else
            {
                // Documentos u otros
                var uploadParams = new RawUploadParams()
                {
                    File = new FileDescription(file.FileName, stream),
                    Folder = $"meritum_uploads/{folderName}"
                };
                var uploadResult = await _cloudinary.UploadAsync(uploadParams);
                return uploadResult.SecureUrl?.AbsoluteUri ?? string.Empty;
            }
        }

        // Lógica de fallback para almacenamiento local
        string webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        string uploadsFolder = Path.Combine(webRoot, "uploads", folderName);
        if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

        string uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

        using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(fileStream);
        }

        return $"/uploads/{folderName}/{uniqueFileName}";
    }
}