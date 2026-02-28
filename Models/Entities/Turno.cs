namespace turnero_medico_backend.Models.Entities
{
    public class Turno
    {
        public int Id { get; set; }
        
        public DateTime FechaHora { get; set; }
        
        public string Motivo { get; set; } = string.Empty;
        
        public string Estado { get; set; } = "Pendiente"; // Pendiente, Confirmado, Cancelado, Completado
        
        // ===== Claves foráneas =====
        public int PacienteId { get; set; }
        public int DoctorId { get; set; }
        
        // ===== NUEVO: Quién creó el turno (Responsable) =====
        public string CreatedByUserId { get; set; } = string.Empty;  // FK → AspNetUser
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // ===== NUEVO: Facturación =====
        public int? ObraSocialId { get; set; }  // FK → ObraSocial (a quién se factura)
        
        public string NotasFacturacion { get; set; } = string.Empty;  // Ej: "OSDE 80%" o "Pago particular"
        
        // ===== NUEVO: Validación de cobertura por doctor =====
        /// <>
        /// Si RequiereValidacionExterna=true en ObraSocialEspecialidad, doctor debe validar
        /// Estados: PendienteValidacionDoctor → Aceptado/Rechazado
        /// </>
        public string? MotivoRechazo { get; set; }  // null si Aceptado; "No cubre este caso" si Rechazado
        
        public DateTime? FechaValidacion { get; set; }  // Cuándo doctor validó externamente
        
        public string? ValidadoPorDoctorId { get; set; }  // FK → Doctor que validó la cobertura
        
        // ===== Propiedades de navegación =====
        public virtual Paciente Paciente { get; set; } = null!;
        public virtual Doctor Doctor { get; set; } = null!;
        public virtual ObraSocial? ObraSocial { get; set; }
    }
}