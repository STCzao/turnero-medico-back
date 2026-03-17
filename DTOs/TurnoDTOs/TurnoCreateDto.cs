using System.ComponentModel.DataAnnotations;
using turnero_medico_backend.DTOs.Validations;

namespace turnero_medico_backend.DTOs.TurnoDTOs
{
    public class TurnoCreateDto
    {
        [Required(ErrorMessage = "El ID del paciente es obligatorio")]
        [Range(1, int.MaxValue, ErrorMessage = "El ID del paciente debe ser un número válido")]
        public int PacienteId { get; set; }

        [Required(ErrorMessage = "La especialidad es obligatoria")]
        [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar una especialidad válida")]
        public int EspecialidadId { get; set; }

        // Opcional: el paciente puede solicitar un doctor específico.
        // Si no lo indica, la secretaria asigna al confirmar.
        [Range(1, int.MaxValue, ErrorMessage = "El ID del doctor debe ser un número válido")]
        public int? DoctorId { get; set; }

        [Required(ErrorMessage = "El motivo de la consulta es obligatorio")]
        [StringLength(500, MinimumLength = 5, ErrorMessage = "El motivo debe tener entre {2} y {1} caracteres")]
        public string Motivo { get; set; } = string.Empty;

        // ===== Datos declarativos de cobertura =====
        // El paciente declara su cobertura. La secretaria verifica contra la realidad.

        [StringLength(30, ErrorMessage = "El número de afiliado declarado no puede exceder 30 caracteres")]
        public string? NumeroAfiliadoDeclarado { get; set; }

        [StringLength(50, ErrorMessage = "El plan declarado no puede exceder 50 caracteres")]
        public string? PlanAfiliadoDeclarado { get; set; }
    }
}