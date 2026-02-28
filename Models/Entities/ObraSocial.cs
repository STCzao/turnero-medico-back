namespace turnero_medico_backend.Models.Entities
{
    public class ObraSocial
    {
        public int Id { get; set; }

        public string Nombre { get; set; } = string.Empty;

        // Almacenado como JSONB en PostgreSQL
        public List<string> Especialidades { get; set; } = [];

        // Pacientes que utilizan esta obra social
        public virtual ICollection<Paciente> Pacientes { get; set; } = new List<Paciente>();
    }
}
