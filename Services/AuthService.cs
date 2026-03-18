using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using turnero_medico_backend.Data;
using turnero_medico_backend.Models.Entities;
using turnero_medico_backend.Repositories.Interfaces;
using turnero_medico_backend.Services.Interfaces;

namespace turnero_medico_backend.Services
{
    public class AuthService(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IConfiguration configuration,
        IRepository<Paciente> pacienteRepository,
        IRepository<Doctor> doctorRepository,
        ApplicationDbContext dbContext,
        IAuditService auditService) : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly RoleManager<ApplicationRole> _roleManager = roleManager;
        private readonly IConfiguration _configuration = configuration;
        private readonly IRepository<Paciente> _pacienteRepository = pacienteRepository;
        private readonly IRepository<Doctor> _doctorRepository = doctorRepository;
        private readonly ApplicationDbContext _dbContext = dbContext;
        private readonly IAuditService _auditService = auditService;

        // ─────────────────────────────────────────────────────────────
        // REGISTRO PACIENTE (auto-registro público)
        // Vinculación por DNI: si ya existe un registro de Paciente con ese DNI
        // (creado por secretaria como dependiente), se vincula en vez de crear uno nuevo.
        // ─────────────────────────────────────────────────────────────
        public async Task<(bool Success, string Message)> RegisterPacienteAsync(
            string email,
            string password,
            string nombre,
            string apellido,
            string dni,
            string telefono,
            DateTime fechaNacimiento)
        {
            // Verificar si el email ya tiene cuenta
            var userExists = await _userManager.FindByEmailAsync(email);
            if (userExists != null)
                return (false, "El email ya está registrado como usuario");

            // Buscar si ya existe un Paciente con este DNI (dependiente o creado por secretaria)
            var pacientesConDni = await _pacienteRepository.FindAsync(p => p.Dni == dni);
            var pacienteExistente = pacientesConDni.FirstOrDefault();

            // Si el paciente ya existe Y ya tiene cuenta vinculada, rechazar
            if (pacienteExistente != null && !string.IsNullOrEmpty(pacienteExistente.UserId))
                return (false, "El DNI ya está vinculado a una cuenta de usuario");

            // Calcular si es mayor de edad
            var edad = DateTime.UtcNow.Year - fechaNacimiento.Year;
            if (fechaNacimiento > DateTime.UtcNow.AddYears(-edad)) edad--;
            var esMayorDeEdad = edad >= 18;

            // --- Todo dentro de una transacción ---
            await using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                // 1. Crear el usuario en AspNetUsers
                var newUser = new ApplicationUser
                {
                    Email = email,
                    UserName = email,
                    Nombre = nombre,
                    Apellido = apellido,
                    FechaRegistro = DateTime.UtcNow
                };

                var result = await _userManager.CreateAsync(newUser, password);
                if (!result.Succeeded)
                {
                    await transaction.RollbackAsync();
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    return (false, $"Error al crear el usuario: {errors}");
                }

                // 2. Asignar rol Paciente
                await EnsureRoleExistsAsync("Paciente");
                await _userManager.AddToRoleAsync(newUser, "Paciente");

                // 3. Vincular o crear Paciente
                if (pacienteExistente != null)
                {
                    // Paciente ya existía (dependiente/creado por secretaria) → vincular por DNI
                    pacienteExistente.UserId = newUser.Id;
                    pacienteExistente.Email = email;
                    pacienteExistente.Telefono = telefono;
                    pacienteExistente.ResponsableId = null; // Ahora es autónomo
                    await _pacienteRepository.UpdateAsync(pacienteExistente);

                    newUser.PacienteId = pacienteExistente.Id;
                }
                else
                {
                    // Paciente nuevo → crear registro
                    var paciente = new Paciente
                    {
                        Dni = dni,
                        Nombre = nombre,
                        Apellido = apellido,
                        Email = email,
                        Telefono = telefono,
                        FechaNacimiento = fechaNacimiento,
                        EsMayorDeEdad = esMayorDeEdad,
                        TipoPago = TipoPago.SinCobertura,
                        NumeroAfiliado = string.Empty,
                        UserId = newUser.Id
                    };

                    var createdPaciente = await _pacienteRepository.AddAsync(paciente);
                    newUser.PacienteId = createdPaciente.Id;
                }

                // 4. Actualizar PacienteId en el usuario para navegación inversa
                await _userManager.UpdateAsync(newUser);

                await transaction.CommitAsync();
                await _auditService.LogAsync(AuditAccion.Registro, "ApplicationUser", newUser.Id);
                return (true, "Paciente registrado exitosamente. Ya puedes agendar turnos.");
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // ─────────────────────────────────────────────────────────────
        // REGISTRO DOCTOR (solo Admin)
        // Vinculación por Matrícula: si ya existe un Doctor con esa matrícula
        // (creado previamente vía CRUD), se vincula a la cuenta nueva.
        // ─────────────────────────────────────────────────────────────
        public async Task<(bool Success, string Message)> RegisterDoctorAsync(
            string email, 
            string password, 
            string nombre, 
            string apellido, 
            string matricula, 
            int especialidadId, 
            string telefono)
        {
            // Verificar si el email ya tiene cuenta
            var userExists = await _userManager.FindByEmailAsync(email);
            if (userExists != null)
                return (false, "El email ya está registrado como usuario");

            // Buscar si ya existe un Doctor con esta matrícula
            var doctoresConMatricula = await _doctorRepository.FindAsync(d => d.Matricula == matricula);
            var doctorExistente = doctoresConMatricula.FirstOrDefault();

            // Si el doctor ya existe Y ya tiene cuenta vinculada, rechazar
            if (doctorExistente != null && !string.IsNullOrEmpty(doctorExistente.UserId))
                return (false, "La matrícula ya está vinculada a una cuenta de usuario");

            // --- Todo dentro de una transacción ---
            await using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                // 1. Crear el usuario en AspNetUsers
                var newUser = new ApplicationUser
                {
                    Email = email,
                    UserName = email,
                    Nombre = nombre,
                    Apellido = apellido,
                    FechaRegistro = DateTime.UtcNow
                };

                var result = await _userManager.CreateAsync(newUser, password);
                if (!result.Succeeded)
                {
                    await transaction.RollbackAsync();
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    return (false, $"Error al crear el usuario: {errors}");
                }

                // 2. Asignar rol Doctor
                await EnsureRoleExistsAsync("Doctor");
                await _userManager.AddToRoleAsync(newUser, "Doctor");

                // 3. Vincular o crear Doctor
                if (doctorExistente != null)
                {
                    // Doctor ya existía (creado vía CRUD sin cuenta) → vincular por Matrícula
                    doctorExistente.UserId = newUser.Id;
                    doctorExistente.Email = email;
                    doctorExistente.Telefono = telefono;
                    await _doctorRepository.UpdateAsync(doctorExistente);

                    newUser.DoctorId = doctorExistente.Id;
                }
                else
                {
                    // Doctor nuevo → crear registro
                    var doctor = new Doctor
                    {
                        Matricula = matricula,
                        Nombre = nombre,
                        Apellido = apellido,
                        EspecialidadId = especialidadId,
                        Email = email,
                        Telefono = telefono,
                        UserId = newUser.Id
                    };

                    var createdDoctor = await _doctorRepository.AddAsync(doctor);
                    newUser.DoctorId = createdDoctor.Id;
                }

                // 4. Actualizar DoctorId en el usuario para navegación inversa
                await _userManager.UpdateAsync(newUser);

                await transaction.CommitAsync();
                await _auditService.LogAsync(AuditAccion.Registro, "ApplicationUser", newUser.Id);
                return (true, "Doctor registrado exitosamente");
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // ─────────────────────────────────────────────────────────────
        // REGISTRO SECRETARIA (solo Admin)
        // ─────────────────────────────────────────────────────────────
        public async Task<(bool Success, string Message)> RegisterSecretariaAsync(
            string email,
            string password,
            string nombre,
            string apellido)
        {
            var userExists = await _userManager.FindByEmailAsync(email);
            if (userExists != null)
                return (false, "El email ya está registrado como usuario");

            await using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var newUser = new ApplicationUser
                {
                    Email = email,
                    UserName = email,
                    Nombre = nombre,
                    Apellido = apellido,
                    FechaRegistro = DateTime.UtcNow
                };

                var result = await _userManager.CreateAsync(newUser, password);
                if (!result.Succeeded)
                {
                    await transaction.RollbackAsync();
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    return (false, $"Error al crear el usuario: {errors}");
                }

                await EnsureRoleExistsAsync("Secretaria");
                await _userManager.AddToRoleAsync(newUser, "Secretaria");

                await transaction.CommitAsync();
                await _auditService.LogAsync(AuditAccion.Registro, "ApplicationUser", newUser.Id);
                return (true, "Secretaria registrada exitosamente");
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        /// Autentica un usuario y devuelve un token JWT

        public async Task<(bool Success, string Token, string RefreshToken, string Message)> LoginAsync(string email, string password)
        {
            try
            {
                // Buscar el usuario
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                    return (false, string.Empty, string.Empty, "Credenciales inválidas");

                // Verificar contraseña
                var isPasswordCorrect = await _userManager.CheckPasswordAsync(user, password);
                if (!isPasswordCorrect)
                    return (false, string.Empty, string.Empty, "Credenciales inválidas");

                // Generar tokens
                var roles = await _userManager.GetRolesAsync(user);
                var token = GenerateJwtToken(user, roles);
                var refreshToken = GenerateRefreshToken();

                user.RefreshTokenHash   = HashToken(refreshToken);
                user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(GetRefreshTokenExpirationDays());
                await _userManager.UpdateAsync(user);

                await _auditService.LogAsync(AuditAccion.Login, "ApplicationUser", user.Id);
                return (true, token, refreshToken, "Login exitoso");
            }
            catch (Exception)
            {
                return (false, string.Empty, string.Empty, "Error interno al intentar iniciar sesión. Intente nuevamente.");
            }
        }

        /// Rota el par access+refresh. El token anterior queda inválido.

        public async Task<(bool Success, string Token, string RefreshToken, string Message)> RefreshTokenAsync(
            string userId, string refreshToken)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user?.RefreshTokenHash is null || user.RefreshTokenExpiry is null)
                return (false, string.Empty, string.Empty, "Token inválido o expirado.");

            if (user.RefreshTokenExpiry < DateTime.UtcNow)
                return (false, string.Empty, string.Empty, "El refresh token ha expirado. Iniciá sesión nuevamente.");

            var providedHashBytes = Encoding.UTF8.GetBytes(HashToken(refreshToken));
            var storedHashBytes   = Encoding.UTF8.GetBytes(user.RefreshTokenHash);

            if (!CryptographicOperations.FixedTimeEquals(providedHashBytes, storedHashBytes))
                return (false, string.Empty, string.Empty, "Token inválido o expirado.");

            // Rotar: nuevo access + nuevo refresh (invalidar el anterior)
            var roles           = await _userManager.GetRolesAsync(user);
            var newAccessToken  = GenerateJwtToken(user, roles);
            var newRefreshToken = GenerateRefreshToken();

            user.RefreshTokenHash   = HashToken(newRefreshToken);
            user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(GetRefreshTokenExpirationDays());
            await _userManager.UpdateAsync(user);

            return (true, newAccessToken, newRefreshToken, "Token renovado exitosamente.");
        }

        /// Genera un token JWT firmado para el usuario

        private string GenerateJwtToken(ApplicationUser user, IList<string> roles)
        {
            var secretKey = _configuration["Jwt:SecretKey"];
            var issuer = _configuration["Jwt:Issuer"];
            var audience = _configuration["Jwt:Audience"];
            var expirationMinutes = int.Parse(_configuration["Jwt:ExpirationMinutes"] ?? "1440");

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id),
                new(ClaimTypes.Email, user.Email ?? string.Empty),
                new(ClaimTypes.Name, $"{user.Nombre} {user.Apellido}"),
            };

            foreach (var role in roles)
                claims.Add(new Claim(ClaimTypes.Role, role));

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

        // Helper: asegura que el rol exista antes de asignarlo
        private async Task EnsureRoleExistsAsync(string roleName)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                await _roleManager.CreateAsync(new ApplicationRole(roleName) { Descripcion = $"Rol de {roleName}" });
            }
        }

        private int GetRefreshTokenExpirationDays()
            => int.TryParse(_configuration["Jwt:RefreshTokenExpirationDays"], out var days) && days > 0 ? days : 30;

        // Genera un token opaco de 32 bytes criptográficamente seguro.
        private static string GenerateRefreshToken()
        {
            var bytes = new byte[32];
            RandomNumberGenerator.Fill(bytes);
            return Convert.ToBase64String(bytes);
        }

        // Devuelve el SHA-256 hexadecimal del token para almacenamiento seguro.
        private static string HashToken(string token)
            => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}
