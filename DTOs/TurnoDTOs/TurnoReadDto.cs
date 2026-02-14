namespace turnero_medico_backend.DTOs.TurnoDTOs;

public class TurnoReadDto
{
    public int Id { get; set; }
    public DateTime FechaHora { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public int PacienteId { get; set; }
    public int DoctorId { get; set; }
    public string Especialidad { get; set; } = string.Empty;}