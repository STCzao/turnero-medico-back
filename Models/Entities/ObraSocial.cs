namespace turnero_medico_backend.Models.Entities
{
    public class ObraSocial
    {
        public int Id { get; set; }
        
        public string Nombre { get; set; } = string.Empty;  // Ej: "Helvetia", "OSDE"
        
        public string Cobertura { get; set; } = string.Empty;  // Ej: "Consultas, Cirugía, Internación"
        
        public decimal PorcentajeCobertura { get; set; }  // Ej: 80, 85, 90
        
        // ===== Relaciones =====
        /// <>
        /// Especialidades que cubre esta obra social (Many-to-Many mapping)
        /// </>
        public virtual ICollection<ObraSocialEspecialidad> Especialidades { get; set; } = new List<ObraSocialEspecialidad>();
        
        /// <>
        /// Pacientes que utilizan esta obra social
        /// </>
        public virtual ICollection<Paciente> Pacientes { get; set; } = new List<Paciente>();
    }
}
