using System.ComponentModel.DataAnnotations;

namespace turnero_medico_backend.DTOs.AuthDTOs
{
    public class LoginRequestDto
    {
        [Required(ErrorMessage = "El email es obligatorio")]
        [EmailAddress(ErrorMessage = "El email no tiene un formato válido")]
        [StringLength(256, ErrorMessage = "El email no puede exceder 256 caracteres")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria")]
        [StringLength(100, ErrorMessage = "La contraseña no puede exceder 100 caracteres")]
        public string Password { get; set; } = string.Empty;
    }
}
