using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using turnero_medico_backend.Data;
using turnero_medico_backend.DTOs.Common;
using turnero_medico_backend.DTOs.ObraSocialDTOs;
using turnero_medico_backend.Models.Entities;
using turnero_medico_backend.Services.Interfaces;

namespace turnero_medico_backend.Services
{
    // Catálogo de obras sociales. Misma estrategia de caché que EspecialidadService:
    // lectura cacheada 60 min, cualquier escritura invalida el caché.
    // Usa ApplicationDbContext directamente (en lugar de IRepository) porque ObraSocial
    // tiene una relación muchos-a-muchos con Especialidades que requiere Include()
    // y manipulación explícita de la colección Especialidades en Create/Update.
    public class ObraSocialService(
        ApplicationDbContext context,
        IMapper mapper,
        IMemoryCache cache,
        IAuditService auditService) : IObraSocialService
    {
        private readonly ApplicationDbContext _context = context;
        private readonly IMapper _mapper = mapper;
        private readonly IMemoryCache _cache = cache;
        private readonly IAuditService _auditService = auditService;

        private const string CacheKey = "obras-sociales:all";  // clave de IMemoryCache
        private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(60);  // TTL del caché

        // Obtiene la lista completa desde caché o DB
        private async Task<List<ObraSocialReadDto>> GetCachedAllAsync()
        {
            if (_cache.TryGetValue(CacheKey, out List<ObraSocialReadDto>? cached) && cached != null)
                return cached;

            var items = await _context.ObrasSociales
                .Include(o => o.Especialidades)
                .OrderBy(o => o.Id)
                .AsNoTracking()
                .ToListAsync();

            var result = items.Select(o => _mapper.Map<ObraSocialReadDto>(o)).ToList();
            _cache.Set(CacheKey, result, Ttl);
            return result;
        }

        public async Task<PagedResultDto<ObraSocialReadDto>> GetAllPagedAsync(int page, int pageSize)
        {
            pageSize = Math.Clamp(pageSize, 1, 100);
            var all = await GetCachedAllAsync();
            return new PagedResultDto<ObraSocialReadDto>
            {
                Items = all.Skip((page - 1) * pageSize).Take(pageSize),
                Total = all.Count,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<ObraSocialReadDto?> GetByIdAsync(int id)
        {
            var all = await GetCachedAllAsync();
            return all.FirstOrDefault(o => o.Id == id);
        }

        public async Task<ObraSocialReadDto> CreateAsync(ObraSocialCreateDto dto)
        {
            var existente = await _context.ObrasSociales.AnyAsync(o => o.Nombre == dto.Nombre);
            if (existente)
                throw new InvalidOperationException($"Ya existe una obra social con el nombre '{dto.Nombre}'");

            var especialidades = await _context.Especialidades
                .Where(e => dto.EspecialidadIds.Contains(e.Id))
                .ToListAsync();

            if (especialidades.Count != dto.EspecialidadIds.Distinct().Count())
                throw new InvalidOperationException("Una o más especialidades proporcionadas no existen.");

            var obra = new ObraSocial
            {
                Nombre = dto.Nombre,
                Planes = dto.Planes,
                Observaciones = dto.Observaciones,
                Especialidades = especialidades
            };

            _context.ObrasSociales.Add(obra);
            await _context.SaveChangesAsync();
            _cache.Remove(CacheKey);
            await _auditService.LogAsync(AuditAccion.Crear, "ObraSocial", obra.Id.ToString());
            return _mapper.Map<ObraSocialReadDto>(obra);
        }

        public async Task<ObraSocialReadDto?> UpdateAsync(int id, ObraSocialUpdateDto dto)
        {
            var obra = await _context.ObrasSociales
                .Include(o => o.Especialidades)
                .FirstOrDefaultAsync(o => o.Id == id);
            if (obra == null) return null;

            var especialidades = await _context.Especialidades
                .Where(e => dto.EspecialidadIds.Contains(e.Id))
                .ToListAsync();

            if (especialidades.Count != dto.EspecialidadIds.Distinct().Count())
                throw new InvalidOperationException("Una o más especialidades proporcionadas no existen.");

            obra.Nombre = dto.Nombre;
            obra.Planes = dto.Planes;
            obra.Observaciones = dto.Observaciones;
            obra.Especialidades = especialidades;

            await _context.SaveChangesAsync();
            _cache.Remove(CacheKey);
            await _auditService.LogAsync(AuditAccion.Actualizar, "ObraSocial", id.ToString());
            return _mapper.Map<ObraSocialReadDto>(obra);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var obra = await _context.ObrasSociales.FindAsync(id);
            if (obra == null) return false;
            _context.ObrasSociales.Remove(obra);
            await _context.SaveChangesAsync();
            _cache.Remove(CacheKey);
            await _auditService.LogAsync(AuditAccion.Eliminar, "ObraSocial", id.ToString());
            return true;
        }
    }
}
