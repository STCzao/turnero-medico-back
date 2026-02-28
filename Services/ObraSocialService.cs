using AutoMapper;
using turnero_medico_backend.DTOs.ObraSocialDTOs;
using turnero_medico_backend.Models.Entities;
using turnero_medico_backend.Repositories.Interfaces;
using turnero_medico_backend.Services.Interfaces;

namespace turnero_medico_backend.Services
{
    public class ObraSocialService(
        IRepository<ObraSocial> obraSocialRepository,
        IMapper mapper) : IObraSocialService
    {
        private readonly IRepository<ObraSocial> _repository = obraSocialRepository;
        private readonly IMapper _mapper = mapper;

        public async Task<IEnumerable<ObraSocialReadDto>> GetAllAsync()
        {
            var obras = await _repository.GetAllAsync();
            return obras.Select(o => _mapper.Map<ObraSocialReadDto>(o));
        }

        public async Task<ObraSocialReadDto?> GetByIdAsync(int id)
        {
            var obra = await _repository.GetByIdAsync(id);
            return obra != null ? _mapper.Map<ObraSocialReadDto>(obra) : null;
        }

        public async Task<ObraSocialReadDto> CreateAsync(ObraSocialCreateDto dto)
        {
            var existente = await _repository.FindAsync(o => o.Nombre == dto.Nombre);
            if (existente.Any())
                throw new InvalidOperationException($"Ya existe una obra social con el nombre '{dto.Nombre}'");

            var obra = _mapper.Map<ObraSocial>(dto);
            var created = await _repository.AddAsync(obra);
            return _mapper.Map<ObraSocialReadDto>(created);
        }

        public async Task<ObraSocialReadDto?> UpdateAsync(int id, ObraSocialUpdateDto dto)
        {
            var obra = await _repository.GetByIdAsync(id);
            if (obra == null) return null;

            obra.Nombre = dto.Nombre;
            obra.Especialidades = dto.Especialidades;

            await _repository.UpdateAsync(obra);
            return _mapper.Map<ObraSocialReadDto>(obra);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var obra = await _repository.GetByIdAsync(id);
            if (obra == null) return false;
            return await _repository.DeleteAsync(id);
        }
    }
}
