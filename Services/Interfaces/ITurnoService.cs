using turnero_medico_backend.DTOs.TurnoDTOs;

namespace turnero_medico_backend.Services.Interfaces
{
    public interface ITurnoService
    {
        Task<IEnumerable<TurnoReadDto>> GetAllAsync();
        Task<IEnumerable<TurnoReadDto>> GetByPacienteAsync(int pacienteId);
        Task<IEnumerable<TurnoReadDto>> GetByDoctorAsync(int doctorId);
        Task<TurnoReadDto?> GetByIdAsync(int id);
        Task<TurnoReadDto> CreateAsync(TurnoCreateDto dto);
        Task<TurnoReadDto?> UpdateAsync(int id, TurnoUpdateDto dto);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistAsync(int id);
    }
}
