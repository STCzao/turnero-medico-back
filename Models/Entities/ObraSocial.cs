namespace turnero_medico_backend.Models.Entities
{
    // Obra social o mutual de cobertura médica.
    // La relación con Especialidades (muchos-a-muchos) determina qué especialidades cubre.
    // TurnoService.CreateAsync valida que la OS del paciente cubra la especialidad solicitada
    // antes de crear la solicitud de turno.
    public class ObraSocial
    {
        public int Id { get; set; }

        public string Nombre { get; set; } = string.Empty;

        // Especialidades cubiertas (relación muchos-a-muchos)
        public ICollection<Especialidad> Especialidades { get; set; } = new List<Especialidad>();

        // Planes disponibles (ej: "Plan 210", "Plan 310"). Almacenado como JSONB.
        public List<string> Planes { get; set; } = [];

        // Texto libre para que el Admin documente condiciones, copagos, restricciones, etc.
        public string Observaciones { get; set; } = string.Empty;

    }
}
