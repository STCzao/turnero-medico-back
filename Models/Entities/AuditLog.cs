namespace turnero_medico_backend.Models.Entities
{
    /// Registro de auditoría para operaciones sobre entidades críticas.
    /// Cumple requisitos regulatorios (LGPD/GDPR) de trazabilidad de accesos.
    public class AuditLog
    {
        public long Id { get; set; }

        // Quién realizó la acción (UserId de AspNetUsers; puede ser null en operaciones de sistema)
        public string? UserId { get; set; }

        // Nombre del usuario para lectura rápida (sin JOIN)
        public string? UsuarioNombre { get; set; }

        // Entidad afectada (ej: "Turno", "Paciente", "Doctor")
        public string Entidad { get; set; } = string.Empty;

        // ID del registro afectado dentro de esa entidad
        public string EntidadId { get; set; } = string.Empty;

        // Acción realizada
        public string Accion { get; set; } = string.Empty;

        // IP del cliente que originó la solicitud
        public string? IpCliente { get; set; }

        // Instante UTC de la operación
        public DateTime FechaHora { get; set; } = DateTime.UtcNow;

        // Snapshot JSON del estado anterior (null en creaciones)
        public string? ValoresAnteriores { get; set; }

        // Snapshot JSON del estado nuevo (null en eliminaciones)
        public string? ValoresNuevos { get; set; }
    }

    public static class AuditAccion
    {
        public const string Crear    = "CREAR";
        public const string Leer     = "LEER";
        public const string Actualizar = "ACTUALIZAR";
        public const string Eliminar = "ELIMINAR";
        public const string Confirmar = "CONFIRMAR";
        public const string Rechazar  = "RECHAZAR";
        public const string Cancelar  = "CANCELAR";
        public const string Login     = "LOGIN";
        public const string Registro  = "REGISTRO";
        public const string ExportarDatos = "EXPORTAR_DATOS";
    }
}
