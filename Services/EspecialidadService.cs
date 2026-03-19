using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using turnero_medico_backend.DTOs.EspecialidadDTOs;
using turnero_medico_backend.Models.Entities;
using turnero_medico_backend.Repositories.Interfaces;
using turnero_medico_backend.Services.Interfaces;

namespace turnero_medico_backend.Services
{
    public class EspecialidadService(
        IRepository<Especialidad> repository,
        IMapper mapper,
        IMemoryCache cache,
        IAuditService auditService) : IEspecialidadService
    {
        private readonly IRepository<Especialidad> _repository = repository;
        private readonly IMapper _mapper = mapper;
        private readonly IMemoryCache _cache = cache;
        private readonly IAuditService _auditService = auditService;

        private const string CacheKey = "especialidades:all";
        private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(60);

        public async Task<IEnumerable<EspecialidadReadDto>> GetAllAsync()
        {
            if (_cache.TryGetValue(CacheKey, out IEnumerable<EspecialidadReadDto>? cached) && cached != null)
                return cached;

            var especialidades = await _repository.GetAllAsync();
            var result = especialidades.Select(e => _mapper.Map<EspecialidadReadDto>(e)).ToList();
            _cache.Set(CacheKey, result, Ttl);
            return result;
        }

        public async Task<EspecialidadReadDto?> GetByIdAsync(int id)
        {
            var especialidad = await _repository.GetByIdAsync(id);
            return especialidad != null ? _mapper.Map<EspecialidadReadDto>(especialidad) : null;
        }

        public async Task<EspecialidadReadDto> CreateAsync(EspecialidadCreateDto dto)
        {
            var existente = await _repository.FindAsync(e => EF.Functions.ILike(e.Nombre, dto.Nombre));
            if (existente.Any())
                throw new InvalidOperationException($"Ya existe una especialidad con el nombre '{dto.Nombre}'");

            var especialidad = _mapper.Map<Especialidad>(dto);
            var created = await _repository.AddAsync(especialidad);
            _cache.Remove(CacheKey);
            await _auditService.LogAsync(AuditAccion.Crear, "Especialidad", created.Id.ToString());
            return _mapper.Map<EspecialidadReadDto>(created);
        }

        public async Task<EspecialidadReadDto?> UpdateAsync(int id, EspecialidadUpdateDto dto)
        {
            var especialidad = await _repository.GetByIdAsync(id);
            if (especialidad == null) return null;

            var duplicado = await _repository.FindAsync(e => EF.Functions.ILike(e.Nombre, dto.Nombre) && e.Id != id);
            if (duplicado.Any())
                throw new InvalidOperationException($"Ya existe una especialidad con el nombre '{dto.Nombre}'");

            especialidad.Nombre = dto.Nombre;
            await _repository.UpdateAsync(especialidad);
            _cache.Remove(CacheKey);
            await _auditService.LogAsync(AuditAccion.Actualizar, "Especialidad", id.ToString());
            return _mapper.Map<EspecialidadReadDto>(especialidad);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var especialidad = await _repository.GetByIdAsync(id);
            if (especialidad == null) return false;
            var deleted = await _repository.DeleteAsync(id);
            if (deleted)
            {
                _cache.Remove(CacheKey);
                await _auditService.LogAsync(AuditAccion.Eliminar, "Especialidad", id.ToString());
            }
            return deleted;
        }
    }
}
