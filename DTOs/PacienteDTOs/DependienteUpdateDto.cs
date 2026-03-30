using System.ComponentModel.DataAnnotations;
using turnero_medico_backend.Models.Entities;

namespace turnero_medico_backend.DTOs.PacienteDTOs
{
    public class DependienteUpdateDto
    {
        [Required]
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre {2} y {1} caracteres")]
        [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$", ErrorMessage = "El nombre solo puede contener letras y espacios")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El apellido es obligatorio")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "El apellido debe tener entre {2} y {1} caracteres")]
        [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$", ErrorMessage = "El apellido solo puede contener letras y espacios")]
        public string Apellido { get; set; } = string.Empty;

        [Required(ErrorMessage = "La fecha de nacimiento es obligatoria")]
        [DataType(DataType.Date)]
        public DateTime FechaNacimiento { get; set; }

        [RegularExpression(@"^[\d\s\-\+\(\)]{8,20}$", ErrorMessage = "El teléfono debe tener entre 8 y 20 caracteres")]
        public string? Telefono { get; set; }

        // Cobertura médica 
        [Range(0, 2, ErrorMessage = "TipoPago debe ser 0 (ObraSocial), 1 (Particular) o 2 (SinCobertura)")]
        public TipoPago TipoPago { get; set; } = TipoPago.ObraSocial;

        [Range(1, int.MaxValue, ErrorMessage = "El ID de la Obra Social debe ser válido")]
        public int? ObraSocialId { get; set; }

        [StringLength(30, ErrorMessage = "El número de afiliado no puede exceder 30 caracteres")]
        public string NumeroAfiliado { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "El plan no puede exceder 50 caracteres")]
        public string? PlanAfiliado { get; set; }
    }
}