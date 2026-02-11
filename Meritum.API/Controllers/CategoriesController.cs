using Microsoft.AspNetCore.Mvc;
using Meritum.Core.Entities;
using Meritum.Infrastructure.Services;

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
    public async Task<IActionResult> Create([FromBody] Category newCategory, [FromQuery] string adminId)
    {
        var user = await _usersService.GetByIdAsync(adminId);
        // Si el usuario no existe O no es Administrador...
        if (user == null || user.Role != "Administrador")
        {
            return StatusCode(403, new { message = "Acceso Denegado. Solo el Administrador puede crear categorías." });
        }
        // validacion
        var existing = await _categoriesService.GetByNameAsync(newCategory.Name);
        if (existing != null)
        {
            return Conflict(new { message = $"¡La categoría '{newCategory.Name}' ya existe. No puedes duplicarla.!" });
        }

        await _categoriesService.CreateAsync(newCategory);
        return Ok(new { message = "Categoría creada con éxito", id = newCategory.Id });
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