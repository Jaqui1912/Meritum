using Microsoft.AspNetCore.Mvc;
using Meritum.Core.Entities;
using Meritum.Infrastructure.Services;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UsersService _usersService;

    public AuthController(UsersService usersService)
    {
        _usersService = usersService;
    }

    // 1. LOGIN (Auto-registro con Roles)
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        string rol = "Invitado";

        // Reglas de Roles
        if (request.Email.ToLower() == "director@ut.edu.mx") rol = "Administrador";
        else if (request.Email.EndsWith("@alumno.ut.edu.mx")) rol = "Alumno";
        else if (request.Email.EndsWith("@ut.edu.mx")) rol = "Docente";

        var user = await _usersService.GetByEmailAsync(request.Email);
        
        if (user == null)
        {
            user = new User { Email = request.Email, Role = rol };
            await _usersService.CreateAsync(user);
        }
        return Ok(user);
    }

    // 2. VER PERFIL (Por ID)
    [HttpGet("{id}")]
    public async Task<IActionResult> GetProfile(string id)
    {
        var user = await _usersService.GetByIdAsync(id);
        if (user == null) return NotFound("Usuario no encontrado");
        return Ok(user);
    }

    // 3. VER TODOS LOS USUARIOS (Para que no batalles buscando IDs)
    [HttpGet]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _usersService.GetAllAsync();
        return Ok(users);
    }
    
    // 4. ADMIN LOGIN (Para el panel web de administración)
    [HttpPost("admin-login")]
    public async Task<IActionResult> AdminLogin([FromBody] AdminLoginRequest request)
    {
        var user = await _usersService.GetByEmailAsync(request.Email);
        
        if (user == null)
            return Unauthorized(new { message = "Credenciales incorrectas." });
            
        if (user.Role != "Administrador")
            return StatusCode(403, new { message = "Acceso denegado. No tienes permisos de administrador." });
            
        // En un proyecto real aquí deberías usar BCrypt u otro hashing para comparar
        // Por ahora, se compara texto plano (ya que lo solicitaron rápido) o puedes hashearlo al crear el seed
        if (user.Password != request.Password)
            return Unauthorized(new { message = "Credenciales incorrectas." });
            
        return Ok(user);
    }
}

// Clases auxiliares para recibir el JSON del login
public class LoginRequest { public string Email { get; set; } }
public class AdminLoginRequest 
{ 
    public string Email { get; set; } 
    public string Password { get; set; } 
}