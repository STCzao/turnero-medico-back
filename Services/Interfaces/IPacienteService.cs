using turnero_medico_backend.DTOs.PacienteDTOs;
using turnero_medico_backend.Models.Entities;

namespace turnero_medico_backend.Services.Interfaces
{
    public interface IPacienteService
    {
        Task<IEnumerable<PacienteReadDto>> GetAllAsync();
        Task<PacienteReadDto?> GetByIdAsync(int id);
        Task<PacienteReadDto?> GetMyProfileAsync();  // ← NUEVO: Obtener perfil propio
        Task<PacienteReadDto> CreateAsync(PacienteCreateDto dto);
        Task<PacienteReadDto?> UpdateAsync(int id, PacienteUpdateDto dto);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistAsync(int id);
    }
}
