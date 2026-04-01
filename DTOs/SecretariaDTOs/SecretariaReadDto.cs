namespace turnero_medico_backend.DTOs.SecretariaDTOs;

public class SecretariaReadDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Dni { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    // Indica si ya tiene cuenta de usuario registrada
    public bool TieneCuenta { get; set; }
}
