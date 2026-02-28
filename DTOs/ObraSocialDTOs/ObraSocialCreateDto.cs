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
    }
}
