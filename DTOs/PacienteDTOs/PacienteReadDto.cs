namespace turnero_medico_backend.DTOs.PacienteDTOs;

public class PacienteReadDto
{
    public int Id { get; set; }
    public string Dni { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public DateTime FechaNacimiento { get; set; }
    
    // ===== Nuevos campos para Familia & ObraSocial =====
    public string? ResponsableId { get; set; }  // null si es autónomo, sino es ID del responsable
    public bool EsMayorDeEdad { get; set; }  // Control de mayoría de edad
    public int TipoPago { get; set; }  // 0=ObraSocial, 1=Particular, 2=SinCobertura
    public int? ObraSocialId { get; set; }  // FK a ObraSocial
    public string NumeroAfiliado { get; set; } = string.Empty;  // Número de afiliado
    public ObraSocialDTOs.ObraSocialReadDto? ObraSocial { get; set; }  // Datos de la OS
}
