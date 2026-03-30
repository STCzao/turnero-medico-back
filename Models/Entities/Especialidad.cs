namespace turnero_medico_backend.Models.Entities
{
    // Catálogo de especialidades médicas (ej: Cardiología, Pediatría).
    // Se usa como FK en Doctor y Turno, y como relación muchos-a-muchos en ObraSocial.
    // El nombre es único (validado en EspecialidadService, no solo con constraint de BD).
    public class Especialidad
    {
        public int Id { get; set; }

        public string Nombre { get; set; } = string.Empty;

        // Doctores que tienen esta especialidad
        public ICollection<Doctor> Doctores { get; set; } = new List<Doctor>();

        // Obras sociales que cubren esta especialidad
        public ICollection<ObraSocial> ObrasSociales { get; set; } = new List<ObraSocial>();
    }
}
