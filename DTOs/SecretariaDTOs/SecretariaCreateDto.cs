using System.ComponentModel.DataAnnotations;

namespace turnero_medico_backend.DTOs.SecretariaDTOs;

public class SecretariaCreateDto
{
    [Required(ErrorMessage = "El nombre es obligatorio")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre {2} y {1} caracteres")]
    [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$", ErrorMessage = "El nombre solo puede contener letras y espacios")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El apellido es obligatorio")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "El apellido debe tener entre {2} y {1} caracteres")]
    [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$", ErrorMessage = "El apellido solo puede contener letras y espacios")]
    public string Apellido { get; set; } = string.Empty;

    [Required(ErrorMessage = "El DNI es obligatorio")]
    [RegularExpression(@"^\d{7,8}$", ErrorMessage = "El DNI debe tener 7 u 8 dígitos numéricos")]
    public string Dni { get; set; } = string.Empty;

    [Required(ErrorMessage = "El email es obligatorio")]
    [EmailAddress(ErrorMessage = "El formato del email no es válido")]
    [StringLength(100, ErrorMessage = "El email no puede exceder 100 caracteres")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "El teléfono es obligatorio")]
    [RegularExpression(@"^[\d\s\-\+\(\)]{8,20}$", ErrorMessage = "El teléfono debe tener entre 8 y 20 caracteres")]
    public string Telefono { get; set; } = string.Empty;
}
