namespace turnero_medico_backend.DTOs.ObraSocialDTOs
{
    public class ObraSocialReadDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public List<string> Especialidades { get; set; } = [];
        public List<string> Planes { get; set; } = [];
        public string Observaciones { get; set; } = string.Empty;
    }
}
