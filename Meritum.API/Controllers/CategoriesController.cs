using Microsoft.AspNetCore.Mvc;
using Meritum.Core.Entities;


[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly CategoriesService _categoriesService;

    public CategoriesController(CategoriesService categoriesService)
    {
        _categoriesService = categoriesService;
    }

    // GET: api/categories (Para que la App muestre las tarjetas)
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var categories = await _categoriesService.GetAllAsync();
        return Ok(categories);
    }

    // POST: api/categories (SOLO PARA TI/ADMIN - Para crear los grupos desde Swagger)
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Category newCategory)
    {
        await _categoriesService.CreateAsync(newCategory);
        return Ok(new { message = "Categoría creada con éxito", id = newCategory.Id });
    }
}