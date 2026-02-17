namespace turnero_medico_backend.Models.Entities
{
    /// <>
    /// Mapeo Many-to-Many entre ObraSocial y Especialidades
    /// Define qué especialidades cubre cada obra social y si requiere validación externa
    /// </>
    public class ObraSocialEspecialidad
    {
        public int Id { get; set; }

        // ===== Foreign Keys =====
        public int ObraSocialId { get; set; }
        public ObraSocial ObraSocial { get; set; }

        // ===== Data =====
        /// <>
        /// Especialidad médica (ej: "Cardiología", "Pediatría", "Odontología")
        /// </>
        public string Especialidad { get; set; } = string.Empty;

        /// <>
        /// Si true: doctor debe validar en sistema externo OSDE/SMOLLA/etc antes de confirmar
        /// Si false: turno se acepta automáticamente (cobertura estándar garantizada)
        /// </>
        public bool RequiereValidacionExterna { get; set; } = true;

        // ===== Audit =====
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    }
}
