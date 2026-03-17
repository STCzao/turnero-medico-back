using turnero_medico_backend.DTOs.EspecialidadDTOs;

namespace turnero_medico_backend.Services.Interfaces
{
    public interface IEspecialidadService
    {
        Task<IEnumerable<EspecialidadReadDto>> GetAllAsync();
        Task<EspecialidadReadDto?> GetByIdAsync(int id);
        Task<EspecialidadReadDto> CreateAsync(EspecialidadCreateDto dto);
        Task<EspecialidadReadDto?> UpdateAsync(int id, EspecialidadUpdateDto dto);
        Task<bool> DeleteAsync(int id);
    }
}
