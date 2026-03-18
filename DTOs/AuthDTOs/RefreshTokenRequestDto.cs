using System.ComponentModel.DataAnnotations;

namespace turnero_medico_backend.DTOs.AuthDTOs
{
    public class RefreshTokenRequestDto
    {
        [Required(ErrorMessage = "El ID de usuario es obligatorio")]
        public string UserId { get; set; } = string.Empty;

        [Required(ErrorMessage = "El refresh token es obligatorio")]
        public string RefreshToken { get; set; } = string.Empty;
    }
}
