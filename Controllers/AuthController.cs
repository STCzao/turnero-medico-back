using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using turnero_medico_backend.DTOs.AuthDTOs;
using turnero_medico_backend.Services.Interfaces;

namespace turnero_medico_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController(IAuthService authService) : ControllerBase
    {
        private readonly IAuthService _authService = authService;

        // Auto-registro público de pacientes.
        // Vinculación por DNI: si ya existe un Paciente con ese DNI (dependiente),
        // se vincula a la cuenta nueva en vez de crear otro registro.
        [HttpPost("register-paciente")]
        [AllowAnonymous]
        public async Task<IActionResult> RegisterPaciente([FromBody] RegisterPacienteDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var (success, message) = await _authService.RegisterPacienteAsync(
                request.Email,
                request.Password,
                request.Nombre,
                request.Apellido,
                request.Dni,
                request.Telefono,
                request.FechaNacimiento
            );

            if (!success)
                return BadRequest(new { message });

            return Ok(new { message });
        }

        // Registro de doctor — exclusivo para Admin.
        // Vinculación por Matrícula: si ya existe un Doctor con esa matrícula (creado vía CRUD),
        // se vincula a la cuenta nueva.
        [HttpPost("register-doctor")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RegisterDoctor([FromBody] RegisterDoctorDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var (success, message) = await _authService.RegisterDoctorAsync(
                request.Email,
                request.Password,
                request.Nombre,
                request.Apellido,
                request.Matricula,
                request.EspecialidadId,
                request.Telefono
            );

            if (!success)
                return BadRequest(new { message });

            return Ok(new { message });
        }

        // Registro de secretaria — exclusivo para Admin.
        [HttpPost("register-secretaria")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RegisterSecretaria([FromBody] RegisterSecretariaDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var (success, message) = await _authService.RegisterSecretariaAsync(
                request.Email,
                request.Password,
                request.Nombre,
                request.Apellido
            );

            if (!success)
                return BadRequest(new { message });

            return Ok(new { message });
        }

        [HttpPost("login")]
        [AllowAnonymous]
        [EnableRateLimiting("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var (success, token, message) = await _authService.LoginAsync(request.Email, request.Password);

            if (!success)
                return Unauthorized(new { message });

            return Ok(new { token, message });
        }

        [HttpGet("profile")]
        [Authorize]
        public IActionResult GetProfile()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var nombre = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
            var rol = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

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
}
