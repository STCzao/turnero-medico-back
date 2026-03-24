using turnero_medico_backend.DTOs.Common;
using turnero_medico_backend.DTOs.SecretariaDTOs;

namespace turnero_medico_backend.Services.Interfaces;

public interface ISecretariaService
{
    Task<PagedResultDto<SecretariaReadDto>> GetAllPagedAsync(int page, int pageSize);
    Task<SecretariaReadDto?> GetByIdAsync(string id);
    Task<SecretariaReadDto> UpdateAsync(string id, SecretariaUpdateDto dto);
    Task<bool> DeleteAsync(string id);
}
