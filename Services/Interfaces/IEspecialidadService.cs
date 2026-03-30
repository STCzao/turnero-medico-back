using turnero_medico_backend.DTOs.EspecialidadDTOs;

namespace turnero_medico_backend.Services.Interfaces
{
    // Catálogo de especialidades médicas. Es de lectura frecuente y escritura esporádica,
    // por lo que GetAllAsync cachea el resultado en memoria (TTL 60 min).
    // Cualquier escritura (Create/Update/Delete) invalida el caché inmediatamente.
    public interface IEspecialidadService
    {
        Task<IEnumerable<EspecialidadReadDto>> GetAllAsync();          // Usa caché en memoria
        Task<EspecialidadReadDto?> GetByIdAsync(int id);
        Task<EspecialidadReadDto> CreateAsync(EspecialidadCreateDto dto);  // Invalida caché
        Task<EspecialidadReadDto?> UpdateAsync(int id, EspecialidadUpdateDto dto);  // Invalida caché
        Task<bool> DeleteAsync(int id);  // Invalida caché
    }
}
