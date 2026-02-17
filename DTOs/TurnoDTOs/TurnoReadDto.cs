namespace turnero_medico_backend.DTOs.TurnoDTOs;

public class TurnoReadDto
{
    public int Id { get; set; }
    public DateTime FechaHora { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public int PacienteId { get; set; }
    public int DoctorId { get; set; }
    public string Especialidad { get; set; } = string.Empty;
    public string PacienteNombre { get; set; } = string.Empty;
    public string DoctorNombre { get; set; } = string.Empty;
    
    // ===== Nuevos campos para Familia & ObraSocial =====
    public string CreatedByUserId { get; set; } = string.Empty;  // Quién creó el turno
    public DateTime CreatedAt { get; set; }  // Cuándo se creó
    public int? ObraSocialId { get; set; }  // A qué OS facturar
    public string NotasFacturacion { get; set; } = string.Empty;  // Detalles de facturación
    public ObraSocialDTOs.ObraSocialReadDto? ObraSocial { get; set; }  // Datos de la OS
    
    // ===== NUEVA: Validación de cobertura externa =====
    public string? MotivoRechazo { get; set; }  // Razón del rechazo (si aplica)
    public DateTime? FechaValidacion { get; set; }  // Cuándo se validó
    public string? ValidadoPorDoctorId { get; set; }  // ID del doctor que validó
}