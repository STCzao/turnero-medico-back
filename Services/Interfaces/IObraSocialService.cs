using turnero_medico_backend.DTOs.ObraSocialDTOs;

namespace turnero_medico_backend.Services.Interfaces
{
    public interface IObraSocialService
    {
        Task<IEnumerable<ObraSocialReadDto>> GetAllAsync();
        Task<ObraSocialReadDto?> GetByIdAsync(int id);
        Task<ObraSocialReadDto> CreateAsync(ObraSocialCreateDto dto);
        Task<ObraSocialReadDto?> UpdateAsync(int id, ObraSocialUpdateDto dto);
        Task<bool> DeleteAsync(int id);
    }
}
