using turnero_medico_backend.DTOs.Common;
using turnero_medico_backend.DTOs.TurnoDTOs;

namespace turnero_medico_backend.Services.Interfaces
{
    public interface ITurnoService
    {
        Task<IEnumerable<TurnoReadDto>> GetAllAsync();
        Task<PagedResultDto<TurnoReadDto>> GetAllPagedAsync(int page, int pageSize, string? estado = null);
        Task<IEnumerable<TurnoReadDto>> GetByPacienteAsync(int pacienteId, string? estado = null);
        Task<IEnumerable<TurnoReadDto>> GetByDoctorAsync(int doctorId, string? estado = null);
        Task<TurnoReadDto?> GetByIdAsync(int id);
        Task<TurnoReadDto> CreateAsync(TurnoCreateDto dto);
        Task<TurnoReadDto?> UpdateAsync(int id, TurnoUpdateDto dto);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistAsync(int id);

        // Gestión administrativa — solo Secretaria/Admin
        Task<TurnoReadDto?> ConfirmarAsync(int turnoId, ConfirmarTurnoDto dto);
        Task<TurnoReadDto?> RechazarAsync(int turnoId, RechazarTurnoDto dto);

        // Cancelación — Paciente, Doctor, Secretaria y Admin con reglas propias
        Task<TurnoReadDto?> CancelarAsync(int turnoId, CancelarTurnoDto dto);
    }
}
