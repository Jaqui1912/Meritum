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

    // 1. LOGIN (Estricto - No auto-registra)
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _usersService.GetByEmailAsync(request.Email);
        
        if (user == null)
            return Unauthorized(new { message = "El usuario no existe." });

        // Si la contraseña fue proporcionada en la cuenta, exigir que coincida
        if (!string.IsNullOrEmpty(user.Password))
        {
            if (string.IsNullOrEmpty(request.Password) || user.Password != request.Password)
                return Unauthorized(new { message = "Contraseña incorrecta." });
        }
        
        return Ok(user);
    }

    // 1.5 REGISTRO (Nuevo)
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "El nombre es obligatorio." });

        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(new { message = "El correo es obligatorio." });

        var existingUser = await _usersService.GetByEmailAsync(request.Email);
        if (existingUser != null)
            return BadRequest(new { message = "El usuario ya existe." });

        string rol = "Invitado";

        // Reglas de Roles
        if (request.Email.ToLower() == "director@ut.edu.mx") rol = "Administrador";
        else if (request.Email.EndsWith("@alumno.ut.edu.mx")) rol = "Alumno";
        else if (request.Email.EndsWith("@ut.edu.mx")) rol = "Docente";

        var newUser = new User 
        { 
            Email = request.Email, 
            Role = rol,
            Name = request.Name,
            Password = request.Password // Opcional, pero recomendado
        };

        await _usersService.CreateAsync(newUser);
        return Ok(newUser);
    }

    // 2. VER PERFIL (Por ID)
    [HttpGet("{id}")]
    public async Task<IActionResult> GetProfile(string id)
    {
        var user = await _usersService.GetByIdAsync(id);
        if (user == null) return NotFound("Usuario no encontrado");
        return Ok(user);
    }

    // 2.5 ACTUALIZAR PERFIL (Por ID) - Nuevo req móvil
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProfile(string id, [FromBody] UpdateProfileRequest request)
    {
        var user = await _usersService.GetByIdAsync(id);
        if (user == null) return NotFound(new { message = "Usuario no encontrado" });

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            user.Name = request.Name;
        }

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var existingUser = await _usersService.GetByEmailAsync(request.Email);
            if (existingUser != null && existingUser.Id != id)
                return BadRequest(new { message = "Este correo ya le pertenece a otro usuario." });
            
            user.Email = request.Email;
        }

        await _usersService.UpdateAsync(id, user);

        return Ok(new { message = "Perfil actualizado exitosamente", user });
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
public class LoginRequest 
{ 
    public string Email { get; set; } = null!; 
    public string? Password { get; set; }
}

public class RegisterRequest 
{ 
    public string Email { get; set; } = null!; 
    public string Name { get; set; } = null!;
    public string? Password { get; set; }
}

public class UpdateProfileRequest
{
    public string? Name { get; set; }
    public string? Email { get; set; }
}

public class AdminLoginRequest 
{ 
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
}