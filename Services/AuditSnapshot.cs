using System.Text.Json;
using System.Text.Json.Serialization;

namespace turnero_medico_backend.Services
{
    /// Helper para serializar snapshots de auditoría.
    /// Usa opciones JSON seguras: ignora ciclos de referencia y escribe indentado para legibilidad.
    internal static class AuditSnapshot
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        internal static string ToJson<T>(T entity) =>
            JsonSerializer.Serialize(entity, Options);
    }
}
