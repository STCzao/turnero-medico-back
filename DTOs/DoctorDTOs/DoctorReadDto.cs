namespace turnero_medico_backend.DTOs.DoctorDTOs;

public class DoctorReadDto
{
    public int Id { get; set; }
    public string Matricula { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Especialidad { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
}