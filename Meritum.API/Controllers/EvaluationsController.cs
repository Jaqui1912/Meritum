using Microsoft.AspNetCore.Mvc;
using Meritum.Core.Entities;


[ApiController]
[Route("api/[controller]")]
public class EvaluationsController : ControllerBase
{
    private readonly EvaluationsService _evaluationsService;

    public EvaluationsController(EvaluationsService evaluationsService)
    {
        _evaluationsService = evaluationsService;
    }

    // POST: api/evaluations (Cuando el alumno da clic en "Enviar")
    [HttpPost]
    public async Task<IActionResult> SubmitVote([FromBody] Evaluation vote)
    {
        // Validación básica: ¿Ya votó este usuario por este proyecto?
        bool alreadyVoted = await _evaluationsService.HasUserVotedAsync(vote.UserId, vote.ProjectId);
        if (alreadyVoted)
        {
            return BadRequest("Ya has evaluado este proyecto anteriormente.");
        }

        await _evaluationsService.CreateAsync(vote);
        return Ok(new { message = "Evaluación guardada exitosamente" });
    }
}