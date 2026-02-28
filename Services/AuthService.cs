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
        Task<(bool Success, string Message)> RegisterPacienteAsync(string email, string password, string nombre, string apellido, string dni, string telefono, DateTime fechaNacimiento);
        Task<(bool Success, string Message)> RegisterDoctorAsync(string email, string password, string nombre, string apellido, string matricula, string especialidad, string telefono);
        Task<(bool Success, string Token, string Message)> LoginAsync(string email, string password);
    }

    public class AuthService(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IConfiguration configuration,
        IRepository<Paciente> pacienteRepository,
        IRepository<Doctor> doctorRepository) : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly RoleManager<ApplicationRole> _roleManager = roleManager;
        private readonly IConfiguration _configuration = configuration;
        private readonly IRepository<Paciente> _pacienteRepository = pacienteRepository;
        private readonly IRepository<Doctor> _doctorRepository = doctorRepository;

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

        /// 
        /// Registra un nuevo paciente - Crea el usuario Y el registro en tabla Pacientes
        /// Auto-registro público
        /// </summary>
        public async Task<(bool Success, string Message)> RegisterPacienteAsync(
            string email,
            string password,
            string nombre,
            string apellido,
            string dni,
            string telefono,
            DateTime fechaNacimiento)
        {
            try
            {
                // Verificar si el usuario ya existe
                var userExists = await _userManager.FindByEmailAsync(email);
                if (userExists != null)
                    return (false, "El usuario ya está registrado");

                // Verificar si el DNI ya existe
                var pacienteExistente = await _pacienteRepository.FindAsync(p => p.Dni == dni);
                if (pacienteExistente.Any())
                    return (false, "El DNI ya está registrado");

                // Calcular si es mayor de edad
                var edad = DateTime.UtcNow.Year - fechaNacimiento.Year;
                if (fechaNacimiento > DateTime.UtcNow.AddYears(-edad)) edad--;
                var esMayorDeEdad = edad >= 18;

                // 1. Crear el usuario en AspNetUsers
                var newUser = new ApplicationUser
                {
                    Email = email,
                    UserName = email,
                    Nombre = nombre,
                    Apellido = apellido,
                    Rol = "Paciente",
                    FechaRegistro = DateTime.UtcNow
                };

                var result = await _userManager.CreateAsync(newUser, password);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    return (false, $"Error al crear el usuario: {errors}");
                }

                // 2. Asignar rol Paciente
                var roleExists = await _roleManager.RoleExistsAsync("Paciente");
                if (!roleExists)
                {
                    await _roleManager.CreateAsync(new ApplicationRole("Paciente") { Descripcion = "Rol de Paciente" });
                }

                await _userManager.AddToRoleAsync(newUser, "Paciente");

                // 3. Crear el registro en la tabla Pacientes
                var paciente = new Paciente
                {
                    Dni = dni,
                    Nombre = nombre,
                    Apellido = apellido,
                    Email = email,
                    Telefono = telefono,
                    FechaNacimiento = fechaNacimiento,
                    EsMayorDeEdad = esMayorDeEdad,
                    TipoPago = TipoPago.SinCobertura,  // Por defecto sin cobertura
                    NumeroAfiliado = string.Empty
                };

                await _pacienteRepository.AddAsync(paciente);

                return (true, "Paciente registrado exitosamente. Ya puedes agendar turnos.");
            }
            catch (Exception ex)
            {
                // TODO: Si falla la creación del paciente, deberíamos hacer rollback del usuario
                return (false, $"Error: {ex.Message}");
            }
        }

        /// 
        /// Registra un doctor - Crea el usuario Y el registro en tabla Doctores
        /// SOLO puede ser llamado por Admin
        /// 
        public async Task<(bool Success, string Message)> RegisterDoctorAsync(
            string email, 
            string password, 
            string nombre, 
            string apellido, 
            string matricula, 
            string especialidad, 
            string telefono)
        {
            try
            {
                // Verificar si el usuario ya existe
                var userExists = await _userManager.FindByEmailAsync(email);
                if (userExists != null)
                    return (false, "El usuario ya está registrado");

                // Verificar si la matrícula ya existe
                var doctorExistente = await _doctorRepository.FindAsync(d => d.Matricula == matricula);
                if (doctorExistente.Any())
                    return (false, "La matrícula ya está registrada");

                // 1. Crear el usuario en AspNetUsers
                var newUser = new ApplicationUser
                {
                    Email = email,
                    UserName = email,
                    Nombre = nombre,
                    Apellido = apellido,
                    Rol = "Doctor",
                    FechaRegistro = DateTime.UtcNow
                };

                var result = await _userManager.CreateAsync(newUser, password);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    return (false, $"Error al crear el usuario: {errors}");
                }

                // 2. Asignar rol Doctor
                var roleExists = await _roleManager.RoleExistsAsync("Doctor");
                if (!roleExists)
                {
                    await _roleManager.CreateAsync(new ApplicationRole("Doctor") { Descripcion = "Rol de Doctor" });
                }

                await _userManager.AddToRoleAsync(newUser, "Doctor");

                // 3. Crear el registro en la tabla Doctores
                var doctor = new Doctor
                {
                    Matricula = matricula,
                    Nombre = nombre,
                    Apellido = apellido,
                    Especialidad = especialidad,
                    Email = email,
                    Telefono = telefono
                };

                await _doctorRepository.AddAsync(doctor);

                return (true, "Doctor registrado exitosamente");
            }
            catch (Exception ex)
            {
                // TODO: Si falla la creación del doctor, deberíamos hacer rollback del usuario
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
                new Claim(ClaimTypes.Role, user.Rol),  // ← Cambiar a ClaimTypes.Role para que [Authorize(Roles="")] funcione
                new Claim("Rol", user.Rol)  // ← Mantener también para compatibilidad con CurrentUserService
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
