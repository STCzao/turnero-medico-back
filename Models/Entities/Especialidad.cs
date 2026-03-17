namespace turnero_medico_backend.Models.Entities
{
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
