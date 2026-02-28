using Microsoft.AspNetCore.Identity;
using turnero_medico_backend.Models.Entities;

namespace turnero_medico_backend.Services
{
    // Servicio para inicializar datos en la BD al arrancar la aplicacion.
    // Responsabilidad unica: infraestructura minima para que el sistema funcione.
    // Los datos de negocio (obras sociales, especialidades, etc.) los gestiona el Admin via API.
    public class SeedDataService(
        RoleManager<ApplicationRole> roleManager,
        UserManager<ApplicationUser> userManager,
        ILogger<SeedDataService> logger)
    {
        private readonly RoleManager<ApplicationRole> _roleManager = roleManager;
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly ILogger<SeedDataService> _logger = logger;

        public async Task SeedAsync()
        {
            try
            {
                await CreateRolesAsync();
                await CreateAdminUserAsync();
                _logger.LogInformation("Data seeding completado exitosamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante data seeding: {Message}", ex.Message);
                throw;
            }
        }

        private async Task CreateRolesAsync()
        {
            var roles = new[]
            {
                new ApplicationRole { Name = "Paciente", Descripcion = "Rol para pacientes que pueden agendar turnos" },
                new ApplicationRole { Name = "Doctor",   Descripcion = "Rol para doctores que pueden recibir turnos" },
                new ApplicationRole { Name = "Admin",    Descripcion = "Rol para administradores con acceso total" }
            };

            foreach (var role in roles)
            {
                if (string.IsNullOrEmpty(role.Name)) continue;

                if (!await _roleManager.RoleExistsAsync(role.Name))
                {
                    var result = await _roleManager.CreateAsync(role);
                    if (result.Succeeded)
                        _logger.LogInformation("Rol '{RoleName}' creado", role.Name);
                    else
                        _logger.LogWarning("Error al crear rol '{RoleName}'", role.Name);
                }
            }
        }

        private async Task CreateAdminUserAsync()
        {
            const string adminEmail    = "admin@turneromedico.local";
            const string adminPassword = "Admin@123456";

            if (await _userManager.FindByEmailAsync(adminEmail) != null)
            {
                _logger.LogInformation("Usuario admin ya existe");
                return;
            }

            var adminUser = new ApplicationUser
            {
                Email          = adminEmail,
                UserName       = adminEmail,
                Nombre         = "Administrador",
                Apellido       = "Sistema",
                Rol            = "Admin",
                FechaRegistro  = DateTime.UtcNow,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(adminUser, adminPassword);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(adminUser, "Admin");
                _logger.LogInformation("Usuario admin creado: {Email}", adminEmail);
            }
            else
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogWarning("Error al crear usuario admin: {Errors}", errors);
            }
        }
    }

    public static class SeedDataServiceExtensions
    {
        public static async Task SeedDatabaseAsync(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var seeder = scope.ServiceProvider.GetRequiredService<SeedDataService>();
            await seeder.SeedAsync();
        }
    }
}
