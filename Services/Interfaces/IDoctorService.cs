using turnero_medico_backend.DTOs.Common;
using turnero_medico_backend.DTOs.DoctorDTOs;

namespace turnero_medico_backend.Services.Interfaces
{
    public interface IDoctorService
    {
        Task<IEnumerable<DoctorReadDto>> GetAllAsync();
        Task<PagedResultDto<DoctorReadDto>> GetAllPagedAsync(int page, int pageSize);
        Task<IEnumerable<DoctorReadDto>> GetByEspecialidadAsync(string especialidad);
        Task<DoctorReadDto?> GetByIdAsync(int id);
        Task<DoctorReadDto?> GetMyProfileAsync();  // ← NUEVO: Obtener perfil propio
        Task<DoctorReadDto> CreateAsync(DoctorCreateDto dto);
        Task<DoctorReadDto?> UpdateAsync(int id, DoctorUpdateDto dto);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistAsync(int id);
    }
}
