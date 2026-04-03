namespace turnero_medico_backend.DTOs.TurnoDTOs;

public class TurnoReadDto
{
    public int Id { get; set; }

    // Null hasta que la secretaria confirme y asigne fecha/hora
    public DateTime? FechaHora { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public int? EspecialidadId { get; set; }
    public string EspecialidadNombre { get; set; } = string.Empty;

    public int PacienteId { get; set; }
    public string PacienteNombre { get; set; } = string.Empty;

    // Null si el paciente no eligió doctor al solicitar
    public int? DoctorId { get; set; }
    public string DoctorNombre { get; set; } = string.Empty;

    // Trazabilidad
    public DateTime CreatedAt { get; set; }

    // Cobertura (declarada por paciente al solicitar)
    public int? ObraSocialId { get; set; }
    public string? NumeroAfiliadoDeclarado { get; set; }
    public string? PlanAfiliadoDeclarado { get; set; }
    public ObraSocialDTOs.ObraSocialReadDto? ObraSocial { get; set; }

    // Gestión por secretaria
    public string? NotasSecretaria { get; set; }
    public string? MotivoRechazo { get; set; }
    public string? MotivoCancelacion { get; set; }
    public string? ConfirmadaPorId { get; set; }
    public DateTime? FechaGestion { get; set; }

    // Observación clínica del doctor
    public string? ObservacionClinica { get; set; }
}