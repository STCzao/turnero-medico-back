using System.ComponentModel.DataAnnotations;

namespace turnero_medico_backend.DTOs.SecretariaDTOs;

public class SecretariaUpdateDto
{
    [Required(ErrorMessage = "El ID es obligatorio")]
    public string Id { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre es obligatorio")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre {2} y {1} caracteres")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El apellido es obligatorio")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "El apellido debe tener entre {2} y {1} caracteres")]
    public string Apellido { get; set; } = string.Empty;

    [Required(ErrorMessage = "El email es obligatorio")]
    [EmailAddress(ErrorMessage = "El email no tiene un formato válido")]
    [StringLength(256, ErrorMessage = "El email no puede exceder 256 caracteres")]
    public string Email { get; set; } = string.Empty;

    [RegularExpression(@"^\d{7,8}$", ErrorMessage = "El DNI debe tener 7 u 8 dígitos numéricos")]
    public string? Dni { get; set; }
}
