namespace turnero_medico_backend.Services.Interfaces
{
    // Servicio de auditoría. Persiste un AuditLog por cada operación sensible.
    // No lanza excepción si el log falla (el error se registra en Serilog).
    // Los snapshots JSON (valoresAnteriores/valoresNuevos) son opcionales y se usan
    // cuando se quiere conservar el estado antes/después del cambio para compliance.
    public interface IAuditService
    {
        Task LogAsync(string accion, string entidad, string entidadId,
            string? valoresAnteriores = null, string? valoresNuevos = null);
    }
}
