using Microsoft.AspNetCore.Mvc;
using Meritum.Core.Entities;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UsersService _usersService;

    public AuthController(UsersService usersService)
    {
        _usersService = usersService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        // 1. Validar Dominio (Tu regla de negocio)
        string rol = "Invitado";
        if (request.Email.EndsWith("@alumno.ut.edu.mx")) rol = "Alumno";
        else if (request.Email.EndsWith("@ut.edu.mx")) rol = "Docente";

        // 2. Buscar o Crear usuario en BD (Simplificado)
        var user = await _usersService.GetByEmailAsync(request.Email);
        if (user == null)
        {
            user = new User 
            { 
                // Recuerda que en Mongo el Id se genera solo o se deja nulo al crear
                Email = request.Email, 
                Role = rol 
            };
            
            // CAMBIO 2: Usamos el método asíncrono del servicio
            await _usersService.CreateAsync(user);
        }

        // 3. Retornar el objeto (En el futuro aquí devolverías un JWT Token)
        return Ok(user);
    }
}

public class LoginRequest { public string Email { get; set; } }