using Microsoft.AspNetCore.Mvc;
using Meritum.Core.Entities;
using Meritum.Infrastructure.Services;
using Meritum.API.Controllers;
[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly CategoriesService _categoriesService;
    private readonly UsersService _usersService;

    public CategoriesController(CategoriesService categoriesService, UsersService usersService)
    {
        _categoriesService = categoriesService;
        _usersService = usersService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var categories = await _categoriesService.GetAllAsync();
        return Ok(categories); //lista de categorias
    }

    [HttpPost]
    // Agregamos de vuelta el adminId en la URL
    public async Task<IActionResult> Create([FromForm] CreateCategoryDto newCategoryDto, [FromQuery] string adminId)
    {
        //VALIDACIÓN DE SEGURIDAD 
        var user = await _usersService.GetByIdAsync(adminId);

        // Si el usuario no existe O no es Administrador, lo rebotamos
        if (user == null || user.Role != "Administrador")
        {
            return StatusCode(403, new { message = "Acceso Denegado. Solo el Administrador puede crear categorías." });
        }

        string iconUrl = string.Empty;

    // Lógica para guardar la imagen
        if (newCategoryDto.IconFile != null)
        {
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "categories");

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = Guid.NewGuid().ToString() + "_" + newCategoryDto.IconFile.FileName;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await newCategoryDto.IconFile.CopyToAsync(stream);
            }

            iconUrl = $"/uploads/categories/{uniqueFileName}";
        }

        // Armamos la Categoría
        var categoryToSave = new Category
        {
            Name = newCategoryDto.Name,
            IconUrl = iconUrl
        };

        //Guardamos en la base de datos
        await _categoriesService.CreateAsync(categoryToSave);

        return Ok(new { message = "¡Categoría creada con éxito!", category = categoryToSave });
    }

    // editar
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] Category updatedCategory, [FromQuery] string adminId)
    {
        var user = await _usersService.GetByIdAsync(adminId);

        // Si el usuario no existe O no es Administrador...
        if (user == null || user.Role != "Administrador")
        {
            return StatusCode(403, new { message = "Acceso Denegado. Solo el Administrador puede editar categorías." });
        }
        var existing = await _categoriesService.GetByIdAsync(id);
        if (existing == null) return NotFound("La categoría no existe.");

        updatedCategory.Id = existing.Id; // Asegurar que el ID no cambie

        await _categoriesService.UpdateAsync(id, updatedCategory);
        return Ok(new { message = "Categoría actualizada correctamente." });
    }

    // eliminar
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, [FromQuery] string adminId)
    {
        var user = await _usersService.GetByIdAsync(adminId);

        // Si el usuario no existe O no es Administrador...
        if (user == null || user.Role != "Administrador")
        {
            return StatusCode(403, new { message = "Acceso Denegado. Solo el Administrador puede eliminar categorías." });
        }
        var existing = await _categoriesService.GetByIdAsync(id);
        if (existing == null) return NotFound("La categoría no existe.");

        await _categoriesService.RemoveAsync(id);
        return Ok(new { message = "Categoría eliminada." });
    }
}