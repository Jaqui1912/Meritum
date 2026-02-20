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

    private readonly CategoriesService _categoriesService; 
    public EvaluationsController(
        EvaluationsService evaluationsService,
        ProjectsService projectsService,
        UsersService usersService,
        CategoriesService categoriesService)
    {
        _evaluationsService = evaluationsService;
        _projectsService = projectsService;
        _usersService = usersService;
        _categoriesService = categoriesService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Evaluation newEvaluation)
    {
        // 1. ¿Existe el Proyecto?
        var project = await _projectsService.GetByIdAsync(newEvaluation.ProjectId);
        if (project == null) return BadRequest(new { message = "El proyecto a evaluar no existe." });

        // 2. ¿Existe el Usuario (Juez)?
        var user = await _usersService.GetByIdAsync(newEvaluation.UserId);
        if (user == null) return BadRequest(new { message = "El usuario evaluador no existe." });

        // 3. ¿Ya lo evaluó antes? (Evitar duplicados)
        var existingEvaluation = await _evaluationsService.GetByProjectAndUserAsync(newEvaluation.ProjectId, newEvaluation.UserId);
        if (existingEvaluation != null) return BadRequest(new { message = "Ya has evaluado este proyecto anteriormente." });

        // Calculamos el promedio exacto 
        double sumatoria = 
            newEvaluation.Funcionalidad +
            newEvaluation.Rendimiento +
            newEvaluation.Arquitectura +
            newEvaluation.UXUI +
            newEvaluation.MVP +
            newEvaluation.AnálisisMercado +
            newEvaluation.ObjetivosInteligentes +
            newEvaluation.Innovación;

        // Lo dividimos entre los 8 criterios
        newEvaluation.FinalScore = sumatoria / 8.0;

        // Lo redondeamos a 1 decimal (Ejemplo: 8.625 se convierte en 8.6)
        newEvaluation.FinalScore = Math.Round(newEvaluation.FinalScore, 1);

        // 5. Guardamos en la Base de Datos
        await _evaluationsService.CreateAsync(newEvaluation);
        
        return Ok(new 
        { 
            message = "¡Evaluación registrada con éxito!", 
            finalScore = newEvaluation.FinalScore 
        });
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

    // GET: api/Evaluations/progress/{userId}
    // Este endpoint da los datos para las tarjetas de avance
    [HttpGet("progress/{userId}")]
    public async Task<IActionResult> GetUserProgress(string userId)
    {
        // 1. Verificamos que el juez exista
        var user = await _usersService.GetByIdAsync(userId);
        if (user == null) return NotFound(new { message = "Usuario no encontrado." });

        // 2. Traemos todos los datos crudos de Mongo
        var allCategories = await _categoriesService.GetAllAsync();
        var allProjects = await _projectsService.GetAllAsync();
        var userEvaluations = await _evaluationsService.GetByUserIdAsync(userId);

        // 3. Armamos la lista que se enviará a la app móvil
        var progressList = new List<object>();

        foreach (var category in allCategories)
        {
            // A) ¿Cuántos proyectos existen en esta categoría en total?
            var projectsInCategory = allProjects.Where(p => p.CategoryId == category.Id).ToList();
            int totalProjects = projectsInCategory.Count;

            // B) De esos proyectos, ¿cuántos ya evaluó el maestro?
            int evaluatedCount = 0;
            if (userEvaluations != null && totalProjects > 0)
            {
                var projectIdsInCategory = projectsInCategory.Select(p => p.Id).ToList();
                evaluatedCount = userEvaluations.Count(e => projectIdsInCategory.Contains(e.ProjectId));
            }

            // C) Calculamos el porcentaje (Ej: 2 de 4 = 50%)
            double percentage = 0;
            if (totalProjects > 0)
            {
                percentage = ((double)evaluatedCount / totalProjects) * 100;
            }

            // D) Empacamos los datos para la tarjeta
            progressList.Add(new
            {
                categoryId = category.Id,
                categoryName = category.Name,
                totalProjects = totalProjects,
                evaluatedProjects = evaluatedCount,
                isCompleted = evaluatedCount == totalProjects && totalProjects > 0, // ¿Ya terminó esta categoría? (true/false)
                progressPercentage = Math.Round(percentage, 1)
            });
        }

        return Ok(progressList);
    }
    // GET: api/Evaluations/leaderboard
    // Genera la tabla de posiciones promediando todas las calificaciones
    [HttpGet("leaderboard")]
    public async Task<IActionResult> GetLeaderboard([FromQuery] double? minScore, [FromQuery] double? maxScore)
    {
        // 1. Obtenemos la dirección base para las imágenes
        var baseUrl = $"{Request.Scheme}://{Request.Host}";

        // 2. Traemos todas las evaluaciones y todos los proyectos
        var allEvaluations = await _evaluationsService.GetAllAsync();
        var allProjects = await _projectsService.GetAllAsync();

        // 3. LA MAGIA: Agrupamos por Proyecto y sacamos el promedio global
        var rankedProjects = allEvaluations
            .GroupBy(e => e.ProjectId) // Agrupamos todas las evaluaciones que sean del mismo proyecto
            .Select(group => new
            {
                ProjectId = group.Key,
                // Promediamos el FinalScore de todos los jueces y redondeamos a 1 decimal
                AverageScore = Math.Round(group.Average(e => e.FinalScore), 1), 
                EvaluationCount = group.Count() // Cuántos jueces lo han calificado
            })
            .ToList();

        // 4. Armamos la lista cruzando el promedio con los datos visuales del proyecto
        var leaderboard = new List<object>();
        foreach (var rank in rankedProjects)
        {
            var project = allProjects.FirstOrDefault(p => p.Id == rank.ProjectId);
            if (project != null)
            {
                // Arreglamos el link de la foto
                string fullImageUrl = string.IsNullOrEmpty(project.ImageUrl) ? "" : baseUrl + project.ImageUrl;

                leaderboard.Add(new
                {
                    id = project.Id,
                    title = project.Title,
                    teamMembers = project.TeamMembers,
                    imageUrl = fullImageUrl,
                    score = rank.AverageScore, // El promedio global calculado
                    totalEvaluators = rank.EvaluationCount // Ejemplo: "Calificado por 3 jueces"
                });
            }
        }

        // 5. FILTROS (Para tus botones de 9.0 - 10.0, 8.0 - 8.9, etc.)
        var query = leaderboard.AsEnumerable();
        
        if (minScore.HasValue) 
        {
            query = query.Where(p => (double)((dynamic)p).score >= minScore.Value);
        }
        if (maxScore.HasValue) 
        {
            query = query.Where(p => (double)((dynamic)p).score <= maxScore.Value);
        }

        // 6. ORDENAMOS DEL MEJOR AL PEOR (Descendente)
        var finalResult = query.OrderByDescending(p => (double)((dynamic)p).score).ToList();

        return Ok(finalResult);
    }

}