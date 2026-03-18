using AutoMapper;
using Microsoft.EntityFrameworkCore;
using turnero_medico_backend.Data;
using turnero_medico_backend.DTOs.Common;
using turnero_medico_backend.DTOs.ObraSocialDTOs;
using turnero_medico_backend.Models.Entities;
using turnero_medico_backend.Services.Interfaces;

namespace turnero_medico_backend.Services
{
    public class ObraSocialService(
        ApplicationDbContext context,
        IMapper mapper) : IObraSocialService
    {
        private readonly ApplicationDbContext _context = context;
        private readonly IMapper _mapper = mapper;

        public async Task<PagedResultDto<ObraSocialReadDto>> GetAllPagedAsync(int page, int pageSize)
        {
            var total = await _context.ObrasSociales.CountAsync();
            var items = await _context.ObrasSociales
                .Include(o => o.Especialidades)
                .OrderBy(o => o.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return new PagedResultDto<ObraSocialReadDto>
            {
                Items = items.Select(o => _mapper.Map<ObraSocialReadDto>(o)),
                Total = total,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<ObraSocialReadDto?> GetByIdAsync(int id)
        {
            var obra = await _context.ObrasSociales
                .Include(o => o.Especialidades)
                .FirstOrDefaultAsync(o => o.Id == id);
            return obra != null ? _mapper.Map<ObraSocialReadDto>(obra) : null;
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
            return _mapper.Map<ObraSocialReadDto>(obra);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var obra = await _context.ObrasSociales.FindAsync(id);
            if (obra == null) return false;
            _context.ObrasSociales.Remove(obra);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
