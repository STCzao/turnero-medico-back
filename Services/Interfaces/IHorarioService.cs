using turnero_medico_backend.DTOs.HorarioDTOs;

namespace turnero_medico_backend.Services.Interfaces
{
    public interface IHorarioService
    {
        Task<IEnumerable<HorarioReadDto>> GetByDoctorAsync(int doctorId);
        Task<HorarioReadDto> CreateAsync(HorarioCreateDto dto);
        Task<bool> DeleteAsync(int id);
        Task<IEnumerable<SlotDisponibleDto>> GetDisponibilidadAsync(int doctorId, DateTime fecha);
    }
}
