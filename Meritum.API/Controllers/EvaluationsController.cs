using Microsoft.AspNetCore.Mvc;
using Meritum.Core.Entities;
using Meritum.Infrastructure.Services;

[ApiController]
[Route("api/[controller]")]
public class EvaluationsController : ControllerBase
{
    private readonly EvaluationsService _evaluationsService;
    private readonly ProjectsService _projectsService; // Para verificar que el proyecto exista
    private readonly UsersService _usersService;       // Para verificar que el juez exista

    public EvaluationsController(
        EvaluationsService evaluationsService,
        ProjectsService projectsService,
        UsersService usersService)
    {
        _evaluationsService = evaluationsService;
        _projectsService = projectsService;
        _usersService = usersService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Evaluation newEvaluation)
    {
        // ¿Existe el Proyecto?
        var project = await _projectsService.GetByIdAsync(newEvaluation.ProjectId);
        if (project == null) return BadRequest(new { message = "El proyecto a evaluar no existe." });

        // ¿Existe el Usuario (Juez)?
        var user = await _usersService.GetByIdAsync(newEvaluation.UserId);
        if (user == null) return BadRequest(new { message = "El usuario evaluador no existe." });

        //  ¿Ya lo evaluó antes? (Evitar duplicados)
        var existingEvaluation = await _evaluationsService.GetByProjectAndUserAsync(newEvaluation.ProjectId, newEvaluation.UserId);
        if (existingEvaluation != null)
        {
            return Conflict(new { message = "Este usuario ya evaluó este proyecto. Usa PUT para editar la calificación." });
        }

        // Rango de calificación (Opcional pero recomendado)
        if (newEvaluation.FinalScore < 0 || newEvaluation.FinalScore > 10)
        {
            return BadRequest(new { message = "La calificación debe estar entre 0 y 10." });
        }

        await _evaluationsService.CreateAsync(newEvaluation);
        return Ok(new { message = "Evaluación registrada con éxito", id = newEvaluation.Id });
    }

    // GET: api/Evaluations/user/{userId}
    // Este endpoint llena la pantalla de "Mi Historial"
    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetHistoryByUser(string userId)
    {
        // 1. Obtener todas las evaluaciones hechas por este usuario
        // (Necesitas agregar este método GetByUserIdAsync en tu Service primero)
        var historial = await _evaluationsService.GetByUserIdAsync(userId);

        if (historial == null || historial.Count == 0)
        {
            return Ok(new
            {
                message = "No has hecho evaluaciones aún.",
                count = 0,
                averageGiven = 0
            });
        }

        // 2. Calcular Estadísticas al vuelo (Backend Power!) ⚡
        int totalEvaluaciones = historial.Count;
        double promedioOtorgado = historial.Average(x => x.FinalScore);

        // 3. Devolver todo junto: Historial + Estadísticas
        return Ok(new
        {
            stats = new
            {
                totalEvaluations = totalEvaluaciones,
                averageGiven = Math.Round(promedioOtorgado, 1) // Ej: 8.5
            },
            history = historial // La lista de proyectos que evaluó
        });
    }
    // GET: api/Evaluations (¡NUEVO! Ver todas las calificaciones del sistema)
    [HttpGet]
    public async Task<IActionResult> GetAllEvaluations()
    {
        var evaluations = await _evaluationsService.GetAllAsync();
        return Ok(evaluations);
    }
}