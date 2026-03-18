using System.ComponentModel.DataAnnotations;

namespace turnero_medico_backend.DTOs.EspecialidadDTOs
{
    public class EspecialidadCreateDto
    {
        [Required(ErrorMessage = "El nombre de la especialidad es obligatorio")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre {2} y {1} caracteres")]
        [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$", ErrorMessage = "El nombre solo puede contener letras y espacios")]
        public string Nombre { get; set; } = string.Empty;
    }
}
