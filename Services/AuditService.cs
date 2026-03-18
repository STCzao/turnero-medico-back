using Serilog;
using turnero_medico_backend.Data;
using turnero_medico_backend.Models.Entities;
using turnero_medico_backend.Services.Interfaces;

namespace turnero_medico_backend.Services
{
    public class AuditService(
        ApplicationDbContext dbContext,
        ICurrentUserService currentUserService,
        IHttpContextAccessor httpContextAccessor) : IAuditService
    {
        private readonly ApplicationDbContext _dbContext = dbContext;
        private readonly ICurrentUserService _currentUserService = currentUserService;
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

        public async Task LogAsync(string accion, string entidad, string entidadId,
            string? valoresAnteriores = null, string? valoresNuevos = null)
        {
            var userId       = _currentUserService.GetUserId();
            var usuarioNombre = _currentUserService.GetUserName();
            var ip           = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

            var log = new AuditLog
            {
                UserId            = userId,
                UsuarioNombre     = usuarioNombre,
                Entidad           = entidad,
                EntidadId         = entidadId,
                Accion            = accion,
                IpCliente         = ip,
                FechaHora         = DateTime.UtcNow,
                ValoresAnteriores = valoresAnteriores,
                ValoresNuevos     = valoresNuevos
            };

            _dbContext.AuditLogs.Add(log);
            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Fallo al persistir AuditLog para {Accion} {Entidad} {EntidadId}",
                    accion, entidad, entidadId);
            }
        }
    }
}
