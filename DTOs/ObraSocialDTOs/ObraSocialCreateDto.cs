using System.ComponentModel.DataAnnotations;

namespace turnero_medico_backend.DTOs.ObraSocialDTOs
{
    public class ObraSocialCreateDto
    {
        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre {2} y {1} caracteres")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe indicar al menos una especialidad")]
        [MinLength(1, ErrorMessage = "Debe indicar al menos una especialidad")]
        public List<string> Especialidades { get; set; } = [];

        // Planes disponibles (ej: "Plan 210", "Plan 310"). Opcional.
        public List<string> Planes { get; set; } = [];

        // Observaciones libres: copagos, restricciones, condiciones especiales, etc.
        [StringLength(1000, ErrorMessage = "Las observaciones no pueden exceder 1000 caracteres")]
        public string Observaciones { get; set; } = string.Empty;
    }
}
