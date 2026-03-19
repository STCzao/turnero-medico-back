namespace turnero_medico_backend.Services.Interfaces
{
    public interface IAuditService
    {
        Task LogAsync(string accion, string entidad, string entidadId,
            string? valoresAnteriores = null, string? valoresNuevos = null);
    }
}
