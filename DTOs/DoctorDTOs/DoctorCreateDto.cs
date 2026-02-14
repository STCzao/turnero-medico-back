using System.ComponentModel.DataAnnotations;

namespace turnero_medico_backend.DTOs.DoctorDTOs
{
    public class DoctorCreateDto
    {
        [Required(ErrorMessage = "La matrícula es obligatoria")]
        [StringLength(15, MinimumLength = 5, ErrorMessage = "La matrícula debe tener entre {2} y {1} caracteres")]
        [RegularExpression(@"^[A-Za-z0-9]+$", ErrorMessage = "La matrícula solo puede contener letras y números")]
        public string Matricula { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre {2} y {1} caracteres")]
        [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$", ErrorMessage = "El nombre solo puede contener letras y espacios")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El apellido es obligatorio")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "El apellido debe tener entre {2} y {1} caracteres")]
        [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$", ErrorMessage = "El apellido solo puede contener letras y espacios")]
        public string Apellido { get; set; } = string.Empty;

        [Required(ErrorMessage = "La especialidad es obligatoria")]
        [StringLength(30, MinimumLength = 3, ErrorMessage = "La especialidad debe tener entre {2} y {1} caracteres")]
        [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$", ErrorMessage = "La especialidad solo puede contener letras y espacios")]
        public string Especialidad { get; set; } = string.Empty;

        [Required(ErrorMessage = "El email es obligatorio")]
        [EmailAddress(ErrorMessage = "El formato del email no es válido")]
        [StringLength(100, ErrorMessage = "El email no puede exceder 100 caracteres")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "El teléfono es obligatorio")]
        [RegularExpression(@"^[\d\s\-\+\(\)]{8,20}$", ErrorMessage = "El teléfono debe tener entre 8 y 20 caracteres")]
        public string Telefono { get; set; } = string.Empty;
    }
}