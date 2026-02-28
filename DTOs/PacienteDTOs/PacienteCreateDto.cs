using System.ComponentModel.DataAnnotations;
using turnero_medico_backend.DTOs.Validations;

namespace turnero_medico_backend.DTOs.PacienteDTOs
{
    public class PacienteCreateDto
    {
        [Required(ErrorMessage = "El DNI es obligatorio")]
        [RegularExpression(@"^\d{7,8}$", ErrorMessage = "El DNI debe tener 7 u 8 dígitos numéricos")]
        public string Dni { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre {2} y {1} caracteres")]
        [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$", ErrorMessage = "El nombre solo puede contener letras y espacios")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El apellido es obligatorio")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "El apellido debe tener entre {2} y {1} caracteres")]
        [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$", ErrorMessage = "El apellido solo puede contener letras y espacios")]
        public string Apellido { get; set; } = string.Empty;

        [Required(ErrorMessage = "El email es obligatorio")]
        [EmailAddress(ErrorMessage = "El formato del email no es válido")]
        [StringLength(100, ErrorMessage = "El email no puede exceder 100 caracteres")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "El teléfono es obligatorio")]
        [RegularExpression(@"^[\d\s\-\+\(\)]{8,20}$", ErrorMessage = "El teléfono debe tener entre 8 y 20 caracteres (números, espacios, guiones, +, paréntesis)")]
        public string Telefono { get; set; } = string.Empty;

        [Required(ErrorMessage = "La fecha de nacimiento es obligatoria")]
        [DataType(DataType.Date)]
        [MinimumAge(18, ErrorMessage = "El paciente debe ser mayor de 18 años")]
        public DateTime FechaNacimiento { get; set; }

        // ===== Nuevos campos para Familia & ObraSocial =====
        [Range(0, 2, ErrorMessage = "TipoPago debe ser 0(ObraSocial), 1(Particular) o 2(SinCobertura)")]
        public int TipoPago { get; set; } = 0;  // 0=ObraSocial (default), 1=Particular, 2=SinCobertura

        [Range(1, int.MaxValue, ErrorMessage = "El ID de la Obra Social debe ser válido")]
        public int? ObraSocialId { get; set; }  // Opcional, solo si TipoPago=0

        [StringLength(30, ErrorMessage = "El número de afiliado no puede exceder 30 caracteres")]
        public string NumeroAfiliado { get; set; } = string.Empty;  // Número de afiliado de la OS
    }
}