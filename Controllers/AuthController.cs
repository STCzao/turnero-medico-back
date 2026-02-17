using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using turnero_medico_backend.Services;

namespace turnero_medico_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController(IAuthService authService) : ControllerBase
    {
        private readonly IAuthService _authService = authService;

        /// Registra un nuevo usuario
        /// Email, contraseña, nombre, apellido, rol (Paciente/Doctor/Admin)
        /// Resultado del registro

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var (success, message) = await _authService.RegisterAsync(
                request.Email,
                request.Password,
                request.Nombre,
                request.Apellido,
                request.Rol
            );

            if (!success)
                return BadRequest(new { message });

            return Ok(new { message });
        }

        /// Autentica un usuario y devuelve un token JWT
        /// Email y contraseña
        /// Token JWT si es exitoso
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var (success, token, message) = await _authService.LoginAsync(request.Email, request.Password);

            if (!success)
                return Unauthorized(new { message });

            return Ok(new { token, message });
        }

        /// Verifica que el usuario está autenticado (requiere token válido)

        [HttpGet("profile")]
        [Authorize]
        public IActionResult GetProfile()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var nombre = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
            var rol = User.FindFirst("Rol")?.Value;

            return Ok(new
            {
                userId,
                email,
                nombre,
                rol,
                message = "Usuario autenticado correctamente"
            });
        }
    }

    /// DTO para solicitud de registro

    public class RegisterRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public string Rol { get; set; } = "Paciente"; // Por defecto es Paciente
    }

    /// DTO para solicitud de login

    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
