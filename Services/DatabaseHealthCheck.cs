using Microsoft.Extensions.Diagnostics.HealthChecks;
using turnero_medico_backend.Data;

namespace turnero_medico_backend.Services
{
    public class DatabaseHealthCheck(ApplicationDbContext db) : IHealthCheck
    {
        private readonly ApplicationDbContext _db = db;

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            var canConnect = await _db.Database.CanConnectAsync(cancellationToken);
            return canConnect
                ? HealthCheckResult.Healthy("Base de datos accesible.")
                : HealthCheckResult.Unhealthy("No se puede conectar a la base de datos.");
        }
    }
}
