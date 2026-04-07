using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using turnero_medico_backend.Data;

namespace turnero_medico_backend.Tests.Integration
{
    /// <summary>
    /// Levanta el stack completo de ASP.NET (routing, middleware, auth, serialización)
    /// usando EF Core InMemory en lugar de PostgreSQL.
    ///
    /// Program.cs ya detecta el entorno "Testing" y registra InMemory directamente,
    /// evitando el conflicto "Only a single database provider can be registered" que
    /// ocurre cuando WebApplicationFactory intenta reemplazar Npgsql en el container.
    ///
    /// La factory solo necesita:
    ///   1. Declarar el entorno "Testing"
    ///   2. Proveer la configuración mínima que Program.cs necesita para arrancar
    /// </summary>
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        // JWT — deben coincidir con appsettings.Testing.json
        public const string TestJwtSecret = "integration-test-secret-key-minimum-32-bytes-long!!";
        public const string TestIssuer    = "test-issuer";
        public const string TestAudience  = "test-app";

        // Admin seeded — ConfigureAppConfiguration garantiza que SeedDatabaseAsync
        // use estas credenciales, independientemente de appsettings.Local.json
        public const string AdminEmail    = "admin@test.local";
        public const string AdminPassword = "AdminTest123!";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // "Testing" hace que Program.cs:
            //   - Use InMemory en lugar de Npgsql
            //   - Omita HTTPS redirect
            //   - Omita RateLimiter
            //   - Omita SeedDatabaseAsync
            builder.UseEnvironment("Testing");

            // El nombre de la DB se captura FUERA del lambda para que todos los DbContext
            // dentro de esta factory compartan la misma base de datos InMemory.
            // Si se llamara Guid.NewGuid() DENTRO del lambda, cada request obtendría
            // una DB distinta y el registro no sería visible en el login.
            var dbName = Guid.NewGuid().ToString();

            // Program.cs no registra Npgsql en Testing → aquí agregamos InMemory sin conflicto.
            // También registramos TestThrowController (del ensamblado de tests) para poder
            // disparar excepciones específicas y verificar el comportamiento del middleware.
            builder.ConfigureTestServices(services =>
            {
                services.AddControllers()
                        .AddApplicationPart(typeof(TestThrowController).Assembly);

                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseInMemoryDatabase(dbName)
                           .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)));
            });

            // ConfigureAppConfiguration corre DESPUÉS de que appsettings.Local.json es cargado
            // por Program.cs → sus valores tienen la mayor prioridad y pisan las credenciales
            // reales del entorno local de desarrollo.
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    // Pisa appsettings.Local.json para que el seeding use credenciales conocidas
                    ["AdminSeed:Email"]    = AdminEmail,
                    ["AdminSeed:Password"] = AdminPassword,
                    // Pisa el JWT de appsettings.Local.json para que coincida con TestJwtSecret
                    ["Jwt:SecretKey"]      = TestJwtSecret,
                    ["Jwt:Issuer"]         = TestIssuer,
                    ["Jwt:Audience"]       = TestAudience,
                    // Necesario para que el CORS "AllowProduction" no lance excepción
                    ["Cors:AllowedOrigin"] = "http://localhost:5173"
                });
            });
        }
    }
}
