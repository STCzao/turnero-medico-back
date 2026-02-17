using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using turnero_medico_backend.Models.Entities;
using turnero_medico_backend.Repositories.Interfaces;

namespace turnero_medico_backend.Services
{
    public interface IAuthService
    {
        Task<(bool Success, string Message)> RegisterAsync(string email, string password, string nombre, string apellido, string rol);
        Task<(bool Success, string Token, string Message)> LoginAsync(string email, string password);
    }

    public class AuthService(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IConfiguration configuration,
        IRepository<Paciente> pacienteRepository) : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly RoleManager<ApplicationRole> _roleManager = roleManager;
        private readonly IConfiguration _configuration = configuration;
        private readonly IRepository<Paciente> _pacienteRepository = pacienteRepository;

        /// Registra un nuevo usuario en el sistema

        public async Task<(bool Success, string Message)> RegisterAsync(string email, string password, string nombre, string apellido, string rol)
        {
            try
            {
                // Verificar si el usuario ya existe
                var userExists = await _userManager.FindByEmailAsync(email);
                if (userExists != null)
                    return (false, "El usuario ya está registrado");

                // Validar rol
                var rolesValidos = new[] { "Paciente", "Doctor", "Admin" };
                if (!rolesValidos.Contains(rol))
                    return (false, "Rol inválido");

                // Crear nuevo usuario
                var newUser = new ApplicationUser
                {
                    Email = email,
                    UserName = email,
                    Nombre = nombre,
                    Apellido = apellido,
                    Rol = rol,
                    FechaRegistro = DateTime.UtcNow
                };

                // Intentar crear el usuario con la contraseña
                var result = await _userManager.CreateAsync(newUser, password);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    return (false, $"Error al crear el usuario: {errors}");
                }

                // Asignar rol
                var roleExists = await _roleManager.RoleExistsAsync(rol);
                if (!roleExists)
                {
                    // Crear el rol si no existe
                    await _roleManager.CreateAsync(new ApplicationRole(rol) { Descripcion = $"Rol de {rol}" });
                }

                await _userManager.AddToRoleAsync(newUser, rol);

                // Auto-liberar ResponsableId si es transición a autonomía
                // Si alguien se registra y tiene un Paciente con ResponsableId, significa que era un dependiente que ahora se está volviendo autónomo
                if (rol == "Paciente")
                {
                    var pacienteExistente = await _pacienteRepository.FindAsync(p => p.Email == email);
                    if (pacienteExistente.Any())
                    {
                        var paciente = pacienteExistente.First();
                        // Si el paciente tenía un responsable, ahora es autónomo
                        if (paciente.ResponsableId != null)
                        {
                            paciente.ResponsableId = null;  // Liberar responsable
                            await _pacienteRepository.UpdateAsync(paciente);
                        }
                    }
                }

                return (true, "Usuario registrado exitosamente");
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}");
            }
        }

        /// Autentica un usuario y devuelve un token JWT

        public async Task<(bool Success, string Token, string Message)> LoginAsync(string email, string password)
        {
            try
            {
                // Buscar el usuario
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                    return (false, string.Empty, "Credenciales inválidas");

                // Verificar contraseña
                var isPasswordCorrect = await _userManager.CheckPasswordAsync(user, password);
                if (!isPasswordCorrect)
                    return (false, string.Empty, "Credenciales inválidas");

                // Generar token JWT
                var token = GenerateJwtToken(user);

                return (true, token, "Login exitoso");
            }
            catch (Exception ex)
            {
                return (false, string.Empty, $"Error: {ex.Message}");
            }
        }

        /// Genera un token JWT firmado para el usuario

        private string GenerateJwtToken(ApplicationUser user)
        {
            var secretKey = _configuration["Jwt:SecretKey"];
            var issuer = _configuration["Jwt:Issuer"];
            var audience = _configuration["Jwt:Audience"];
            var expirationMinutes = int.Parse(_configuration["Jwt:ExpirationMinutes"] ?? "1440");

            // Crear las claims (información del usuario en el token)
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim(ClaimTypes.Name, $"{user.Nombre} {user.Apellido}"),
                new Claim("Rol", user.Rol)
            };

            // Crear la clave de firma
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey ?? string.Empty));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Crear el token
            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
