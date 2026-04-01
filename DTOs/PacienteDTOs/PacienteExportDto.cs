namespace turnero_medico_backend.DTOs.PacienteDTOs;

public class PacienteExportDto
{
    public DateTime FechaExportacion { get; set; } = DateTime.UtcNow;

    // Datos personales
    public int Id { get; set; }
    public string Dni { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public DateTime FechaNacimiento { get; set; }
    public bool EsMayorDeEdad { get; set; }

    // Historial de turnos
    public IEnumerable<TurnoExportItem> Turnos { get; set; } = [];

    public class TurnoExportItem
    {
        public int Id { get; set; }
        public DateTime? FechaHora { get; set; }
        public string Motivo { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public string EspecialidadNombre { get; set; } = string.Empty;
        public string DoctorNombre { get; set; } = string.Empty;
        public string? ObraSocialNombre { get; set; }
        public string? NumeroAfiliadoDeclarado { get; set; }
        public string? PlanAfiliadoDeclarado { get; set; }
        public DateTime CreadoEn { get; set; }
        public string? ObservacionClinica { get; set; }
    }
}
