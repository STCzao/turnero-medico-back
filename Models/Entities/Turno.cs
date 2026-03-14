namespace turnero_medico_backend.Models.Entities
{
    public class Turno
    {
        public int Id { get; set; }

        // Asignada por la secretaria al confirmar. Null mientras la solicitud está pendiente.
        public DateTime? FechaHora { get; set; }

        public string Motivo { get; set; } = string.Empty;

        public string Especialidad { get; set; } = string.Empty;

        // Estado inicial siempre SolicitudPendiente. Ver EstadoTurno para valores válidos.
        public string Estado { get; set; } = EstadoTurno.SolicitudPendiente;

        // ===== Claves foráneas =====
        public int PacienteId { get; set; }
        public int? DoctorId { get; set; }  // Puede ser null si el paciente no eligió doctor

        // ===== Trazabilidad de creación =====
        public string CreatedByUserId { get; set; } = string.Empty;  // FK → AspNetUser
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // ===== Datos declarativos de cobertura (informados por el paciente) =====
        public int? ObraSocialId { get; set; }         // FK → ObraSocial
        public string? NumeroAfiliadoDeclarado { get; set; }  // Número de afiliado declarado por el paciente
        public string? PlanAfiliadoDeclarado { get; set; }    // Plan declarado por el paciente

        // ===== Gestión por secretaria =====
        public string? NotasSecretaria { get; set; }   // Condiciones, copago, requisitos informados al paciente
        public string? MotivoRechazo { get; set; }     // Obligatorio si Estado = Rechazado
        public string? MotivoCancelacion { get; set; }  // Obligatorio si Estado = Cancelado
        public string? ConfirmadaPorId { get; set; }   // FK → AspNetUser (secretaria o admin que gestionó)
        public DateTime? FechaGestion { get; set; }    // Cuándo se confirmó/rechazó

        // ===== Observación clínica del doctor =====
        public string? ObservacionClinica { get; set; }

        // ===== Concurrencia optimista =====
        public byte[]? RowVersion { get; set; }

        // ===== Propiedades de navegación =====
        public virtual Paciente Paciente { get; set; } = null!;
        public virtual Doctor? Doctor { get; set; }
        public virtual ObraSocial? ObraSocial { get; set; }
    }
}