using Microsoft.AspNetCore.Mvc;
using Meritum.Core.Entities;
using Meritum.Infrastructure.Services;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UsersService _usersService;
    private readonly EmailService _emailService;

    public AuthController(UsersService usersService, EmailService emailService)
    {
        _usersService = usersService;
        _emailService = emailService;
    }

    // 1. LOGIN (Estricto - No auto-registra)
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _usersService.GetByEmailAsync(request.Email);
        
        if (user == null)
            return Unauthorized(new { message = "El usuario no existe." });

        if (!user.IsVerified)
            return StatusCode(403, new { message = "Debes verificar tu correo electrónico antes de ingresar." });

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

        var verificationToken = Guid.NewGuid().ToString("N");

        var newUser = new User 
        { 
            Email = request.Email, 
            Role = rol,
            Name = request.Name,
            Password = request.Password, // Opcional, pero recomendado
            IsVerified = false,
            VerificationToken = verificationToken
        };

        await _usersService.CreateAsync(newUser);

        // Construir URL real dependiendo de donde esté alojado (para Koyeb/Render)
        // Obtener el host actual de la solicitud
        var requestUrl = $"{Request.Scheme}://{Request.Host}";
        var verificationUrl = $"{requestUrl}/api/auth/verify?token={verificationToken}";

        // Enviar correo de confirmación de manera asíncrona (sin bloquear al usuario)
        _ = _emailService.SendVerificationEmailAsync(newUser.Email, newUser.Name ?? "Usuario", verificationUrl);

        return Ok(newUser);
    }

    [HttpGet("verify")]
    public async Task<IActionResult> VerifyEmail([FromQuery] string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return BadRequest("Token inválido.");

        var user = await _usersService.GetByVerificationTokenAsync(token);

        if (user == null)
            return Content("<html><body style='font-family: sans-serif; text-align: center; padding: 50px;'><h2>El enlace de verificación es inválido o ha expirado.</h2></body></html>", "text/html");

        if (user.IsVerified)
            return Content("<html><body style='font-family: sans-serif; text-align: center; padding: 50px;'><h2>El correo ya había sido verificado anteriormente. Puedes iniciar sesión en Meritum.</h2></body></html>", "text/html");

        user.IsVerified = true;
        user.VerificationToken = null;
        await _usersService.UpdateAsync(user.Id, user);

        // Retornar HTML de confirmación
        var successHtml = @"
        <html>
            <body style='margin: 0; padding: 0; font-family: ""Segoe UI"", Tahoma, Geneva, Verdana, sans-serif; background-color: #F8F7F5; color: #1C140D; display: flex; justify-content: center; align-items: center; height: 100vh;'>
                <div style='text-align: center; background-color: #ffffff; padding: 40px; border-radius: 12px; box-shadow: 0 4px 16px rgba(0,0,0,0.06); max-width: 400px;'>
                    <h1 style='color: #F48C25; font-size: 32px; margin-bottom: 10px;'>¡Verificado!</h1>
                    <p style='color: #4b5563; font-size: 16px; margin-bottom: 20px;'>Tu correo ha sido confirmado exitosamente.</p>
                    <p style='color: #4b5563; font-size: 14px;'>Ya puedes regresar a la aplicación de Meritum e iniciar sesión.</p>
                </div>
            </body>
        </html>";

        return Content(successHtml, "text/html");
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