namespace turnero_medico_backend.DTOs.SecretariaDTOs;

public class SecretariaReadDto
{
    public string Id { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime FechaRegistro { get; set; }
}
