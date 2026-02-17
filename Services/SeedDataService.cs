using Microsoft.AspNetCore.Identity;
using turnero_medico_backend.Models.Entities;
using turnero_medico_backend.Repositories.Interfaces;

namespace turnero_medico_backend.Services
{
    /// <summary>
    /// Servicio para inicializar datos en la BD
    /// Se ejecuta al iniciar la aplicación
    /// </summary>
    public class SeedDataService(
        RoleManager<ApplicationRole> roleManager,
        UserManager<ApplicationUser> userManager,
        ILogger<SeedDataService> logger,
        IRepository<ObraSocial> obraSocialRepository,
        IRepository<ObraSocialEspecialidad> obraSocialEspecialidadRepository)
    {
        private readonly RoleManager<ApplicationRole> _roleManager = roleManager;
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly ILogger<SeedDataService> _logger = logger;
        private readonly IRepository<ObraSocial> _obraSocialRepository = obraSocialRepository;
        private readonly IRepository<ObraSocialEspecialidad> _obraSocialEspecialidadRepository = obraSocialEspecialidadRepository;

        /// <summary>
        /// Ejecuta el seeding inicial de datos
        /// </summary>
        public async Task SeedAsync()
        {
            try
            {
                // Crear roles por defecto
                await CreateRolesAsync();
                
                // Crear usuario admin de prueba (solo en desarrollo)
                await CreateAdminUserAsync();

                // Crear obras sociales iniciales
                await CreateObrasSocialesAsync();

                // ===== NUEVA: Crear especialidades por obra social =====
                await CreateEspecialidadesPorObraAsync();

                _logger.LogInformation("Data seeding completado exitosamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante data seeding: {Message}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Crea los tres roles de la aplicación
        /// </summary>
        private async Task CreateRolesAsync()
        {
            var roles = new[]
            {
                new ApplicationRole
                {
                    Name = "Paciente",
                    Descripcion = "Rol para pacientes que pueden agendar turnos"
                },
                new ApplicationRole
                {
                    Name = "Doctor",
                    Descripcion = "Rol para doctores que pueden recibir turnos"
                },
                new ApplicationRole
                {
                    Name = "Admin",
                    Descripcion = "Rol para administradores con acceso total"
                }
            };

            foreach (var role in roles)
            {
                if (string.IsNullOrEmpty(role.Name))
                    continue;

                var roleExists = await _roleManager.RoleExistsAsync(role.Name);
                if (!roleExists)
                {
                    var result = await _roleManager.CreateAsync(role);
                    if (result.Succeeded)
                        _logger.LogInformation(" Rol '{RoleName}' creado", role.Name);
                    else
                        _logger.LogWarning(" Error al crear rol '{RoleName}'", role.Name);
                }
                else
                {
                    _logger.LogInformation(" Rol '{RoleName}' ya existe", role.Name);
                }
            }
        }

        /// <summary>
        /// Crea un usuario admin de prueba (solo si no existe)
        /// </summary>
        private async Task CreateAdminUserAsync()
        {
            const string adminEmail = "admin@turneromedico.local";
            const string adminPassword = "Admin@123456";

            var adminExists = await _userManager.FindByEmailAsync(adminEmail);
            if (adminExists != null)
            {
                _logger.LogInformation("ℹ Usuario admin '{Email}' ya existe", adminEmail);
                return;
            }

            var adminUser = new ApplicationUser
            {
                Email = adminEmail,
                UserName = adminEmail,
                Nombre = "Administrador",
                Apellido = "Sistema",
                Rol = "Admin",
                FechaRegistro = DateTime.UtcNow,
                EmailConfirmed = true // Confirmado automáticamente para testing
            };

            var result = await _userManager.CreateAsync(adminUser, adminPassword);
            if (result.Succeeded)
            {
                // Asignar rol Admin
                await _userManager.AddToRoleAsync(adminUser, "Admin");
                _logger.LogInformation(
                    " Usuario admin creado: Email={Email}, Contraseña={Password}",
                    adminEmail,
                    adminPassword);
            }
            else
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogWarning(" Error al crear usuario admin: {Errors}", errors);
            }
        }

        /// <summary>
        /// Crea las obras sociales iniciales de prueba
        /// </summary>
        private async Task CreateObrasSocialesAsync()
        {
            var obrasSocialesIniciales = new[]
            {
                new ObraSocial
                {
                    Nombre = "OSDE",
                    Cobertura = "Cobertura integral de salud",
                    PorcentajeCobertura = 80
                },
                new ObraSocial
                {
                    Nombre = "OBRA SOCIAL DOCENTES (OSD)",
                    Cobertura = "Cobertura médica para docentes",
                    PorcentajeCobertura = 75
                },
                new ObraSocial
                {
                    Nombre = "SMOLLA",
                    Cobertura = "Cobertura médica y farmacéutica",
                    PorcentajeCobertura = 70
                },
                new ObraSocial
                {
                    Nombre = "SOS MEDICO",
                    Cobertura = "Servicio de emergencia médica",
                    PorcentajeCobertura = 85
                },
                new ObraSocial
                {
                    Nombre = "SWISS MEDICAL",
                    Cobertura = "Cobertura médica prepaga",
                    PorcentajeCobertura = 90
                }
            };

            foreach (var obra in obrasSocialesIniciales)
            {
                // Verificar si ya existe
                var existente = await _obraSocialRepository.FindAsync(o => o.Nombre == obra.Nombre);
                if (!existente.Any())
                {
                    await _obraSocialRepository.AddAsync(obra);
                    _logger.LogInformation(" Obra social '{Nombre}' creada", obra.Nombre);
                }
                else
                {
                    _logger.LogInformation(" Obra social '{Nombre}' ya existe", obra.Nombre);
                }
            }
        }

        /// <summary>
        /// Crea las especialidades cubiertas por cada obra social
        /// Define qué requiere validación externa y qué no
        /// </summary>
        private async Task CreateEspecialidadesPorObraAsync()
        {
            // Obtener todas las obras sociales
            var obras = await _obraSocialRepository.GetAllAsync();
            var osDict = obras.ToDictionary(o => o.Nombre);

            // Definir especialidades por OS y si requieren validación
            var especialidadesPorOS = new Dictionary<string, List<(string Especialidad, bool RequiereValidacion)>>
            {
                // OSDE: cubre todo pero cardio y casos complejos requieren validación
                { "OSDE", new List<(string, bool)>
                {
                    ("Cardiología", true),
                    ("Pediatría", false),
                    ("Odontología", false),
                    ("Oftalmología", false),
                    ("Dermatología", false),
                    ("Gastroenterología", true),
                    ("Neurología", true),
                    ("Traumatología", false)
                }},
                // OSD: cobertura estándar para docentes
                { "OBRA SOCIAL DOCENTES (OSD)", new List<(string, bool)>
                {
                    ("Cardiología", false),
                    ("Pediatría", false),
                    ("Odontología", false),
                    ("Oftalmología", false),
                    ("Dermatología", false),
                    ("Traumatología", false)
                }},
                // SMOLLA: cobertura limitada
                { "SMOLLA", new List<(string, bool)>
                {
                    ("Pediatría", false),
                    ("Odontología", false),
                    ("Dermatología", false),
                    ("Oftalmología", true)
                }},
                // SOS MEDICO: emergencia y urgencia
                { "SOS MEDICO", new List<(string, bool)>
                {
                    ("Cardiología", true),
                    ("Traumatología", false),
                    ("Neurología", true),
                    ("Pediatría", false)
                }},
                // SWISS MEDICAL: cobertura premium
                { "SWISS MEDICAL", new List<(string, bool)>
                {
                    ("Cardiología", false),
                    ("Pediatría", false),
                    ("Odontología", false),
                    ("Oftalmología", false),
                    ("Dermatología", false),
                    ("Gastroenterología", false),
                    ("Neurología", false),
                    ("Traumatología", false)
                }}
            };

            // Insertar especialidades
            foreach (var (osNombre, especialidades) in especialidadesPorOS)
            {
                if (!osDict.TryGetValue(osNombre, out var os))
                    continue;

                foreach (var (especialidad, requiereValidacion) in especialidades)
                {
                    // Verificar si ya existe
                    var existente = await _obraSocialEspecialidadRepository.FindAsync(ose =>
                        ose.ObraSocialId == os.Id &&
                        ose.Especialidad.ToLower() == especialidad.ToLower());

                    if (!existente.Any())
                    {
                        var ose = new ObraSocialEspecialidad
                        {
                            ObraSocialId = os.Id,
                            Especialidad = especialidad,
                            RequiereValidacionExterna = requiereValidacion
                        };
                        await _obraSocialEspecialidadRepository.AddAsync(ose);
                        _logger.LogInformation(
                            " Especialidad '{Especialidad}' agregada a '{ObraSocial}' (Validación externa: {Requiere})",
                            especialidad, osNombre, requiereValidacion);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Extensión para facilitar el seeding en Program.cs
    /// </summary>
    public static class SeedDataServiceExtensions
    {
        public static async Task SeedDatabaseAsync(this WebApplication app)
        {
            using (var scope = app.Services.CreateScope())
            {
                var seeder = scope.ServiceProvider.GetRequiredService<SeedDataService>();
                await seeder.SeedAsync();
            }
        }
    }
}
