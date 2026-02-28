namespace turnero_medico_backend.DTOs.ObraSocialDTOs
{
    public class ObraSocialReadDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public List<string> Especialidades { get; set; } = [];
    }
}
