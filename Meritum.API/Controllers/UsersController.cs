using Microsoft.AspNetCore.Mvc;
using Meritum.Core.Entities;
using Meritum.Infrastructure.Services;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly UsersService _usersService;
    private readonly EvaluationsService _evaluationsService;

    // Inyectamos ambos servicios porque necesitamos datos de usuario y de sus votos
    public UsersController(UsersService usersService, EvaluationsService evaluationsService)
    {
        _usersService = usersService;
        _evaluationsService = evaluationsService;
    }

    // GET: api/users/profile/juan@ut.edu.mx
    // Este endpoint llena los numeritos del perfil (25 Evaluados, 9.2 Promedio)
    [HttpGet("profile/{email}")]
    public async Task<IActionResult> GetUserProfile(string email)
    {
        // 1. Buscamos al usuario
        var user = await _usersService.GetByEmailAsync(email);
        if (user == null) return NotFound("Usuario no encontrado");

        // 2. Buscamos su historial de evaluaciones
        var history = await _evaluationsService.GetByUserAsync(user.Id);

        // 3. Calculamos las estadísticas (LÓGICA DE NEGOCIO EN BACKEND)
        var totalEvaluated = history.Count;
        
        // Calcular promedio de promedios (si tiene evaluaciones)
        double averageScore = totalEvaluated > 0 
            ? history.Average(h => h.FinalScore) 
            : 0;

        // Contar grupos distintos (Esto requiere un truco extra o hacerlo simple por ahora)
        // Por ahora devolvemos datos básicos
        
        var profileStats = new 
        {
            UserName = user.Email, // O nombre si lo tuvieras
            Role = user.Role,
            EvaluatedProjects = totalEvaluated,
            AverageScore = Math.Round(averageScore, 1), // Redondeado a 1 decimal (ej: 9.2)
            History = history // Mandamos la lista completa para la pantalla "Historial"
        };

        return Ok(profileStats);
    }
}