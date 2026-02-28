using System.ComponentModel.DataAnnotations;

namespace turnero_medico_backend.DTOs.AuthDTOs
{
    public class RegisterRequestDto
    {
        [Required(ErrorMessage = "El email es obligatorio")]
        [EmailAddress(ErrorMessage = "El email no tiene un formato válido")]
        [StringLength(256, ErrorMessage = "El email no puede exceder 256 caracteres")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener entre {2} y {1} caracteres")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre {2} y {1} caracteres")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El apellido es obligatorio")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "El apellido debe tener entre {2} y {1} caracteres")]
        public string Apellido { get; set; } = string.Empty;

        [Required(ErrorMessage = "El rol es obligatorio")]
        [RegularExpression("^(Paciente|Doctor|Admin)$", ErrorMessage = "El rol debe ser Paciente, Doctor o Admin")]
        public string Rol { get; set; } = "Paciente";
    }
}
