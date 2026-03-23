using turnero_medico_backend.DTOs.Common;
using turnero_medico_backend.DTOs.PacienteDTOs;
using turnero_medico_backend.Models.Entities;

namespace turnero_medico_backend.Services.Interfaces
{
    public interface IPacienteService
    {
        Task<PagedResultDto<PacienteReadDto>> GetAllPagedAsync(int page, int pageSize);
        Task<PacienteReadDto?> GetByIdAsync(int id);
        Task<PacienteReadDto?> GetMyProfileAsync();
        Task<PacienteReadDto> CreateAsync(PacienteCreateDto dto);
        Task<PacienteReadDto> UpdateAsync(int id, PacienteUpdateDto dto);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistAsync(int id);

        // Dependientes
        Task<PagedResultDto<PacienteReadDto>> GetMisDependientesAsync(int page, int pageSize);
        Task<PacienteReadDto> CreateDependienteAsync(DependienteCreateDto dto);

        // GDPR: exportar todos los datos del paciente autenticado
        Task<PacienteExportDto?> ExportarMisDatosAsync();
    }
}
