namespace turnero_medico_backend.Models.Entities
{
    /// <summary>
    /// Mapeo Many-to-Many entre ObraSocial y Especialidades
    /// Define qué especialidades cubre cada obra social y si requiere validación externa
    /// </summary>
    public class ObraSocialEspecialidad
    {
        public int Id { get; set; }

        // ===== Foreign Keys =====
        public int ObraSocialId { get; set; }
        public ObraSocial ObraSocial { get; set; }

        // ===== Data =====
        /// <summary>
        /// Especialidad médica (ej: "Cardiología", "Pediatría", "Odontología")
        /// </summary>
        public string Especialidad { get; set; } = string.Empty;

        /// <summary>
        /// Si true: doctor debe validar en sistema externo OSDE/SMOLLA/etc antes de confirmar
        /// Si false: turno se acepta automáticamente (cobertura estándar garantizada)
        /// </summary>
        public bool RequiereValidacionExterna { get; set; } = true;

        // ===== Audit =====
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    }
}
