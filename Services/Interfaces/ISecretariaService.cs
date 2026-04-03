using turnero_medico_backend.DTOs.Common;
using turnero_medico_backend.DTOs.SecretariaDTOs;

namespace turnero_medico_backend.Services.Interfaces;

public interface ISecretariaService
{
    Task<PagedResultDto<SecretariaReadDto>> GetAllPagedAsync(int page, int pageSize);
    Task<SecretariaReadDto?> GetByIdAsync(int id);
    Task<SecretariaReadDto?> GetMyProfileAsync();
    Task<SecretariaReadDto> CreateAsync(SecretariaCreateDto dto);
    Task<SecretariaReadDto> UpdateAsync(int id, SecretariaUpdateDto dto);
    Task<bool> DeleteAsync(int id);
    Task<SecretariaReadDto> ReactivarAsync(int id);
}
