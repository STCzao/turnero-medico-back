using AutoMapper;
using turnero_medico_backend.DTOs.EspecialidadDTOs;
using turnero_medico_backend.Models.Entities;
using turnero_medico_backend.Repositories.Interfaces;
using turnero_medico_backend.Services.Interfaces;

namespace turnero_medico_backend.Services
{
    public class EspecialidadService(
        IRepository<Especialidad> repository,
        IMapper mapper) : IEspecialidadService
    {
        private readonly IRepository<Especialidad> _repository = repository;
        private readonly IMapper _mapper = mapper;

        public async Task<IEnumerable<EspecialidadReadDto>> GetAllAsync()
        {
            var especialidades = await _repository.GetAllAsync();
            return especialidades.Select(e => _mapper.Map<EspecialidadReadDto>(e));
        }

        public async Task<EspecialidadReadDto?> GetByIdAsync(int id)
        {
            var especialidad = await _repository.GetByIdAsync(id);
            return especialidad != null ? _mapper.Map<EspecialidadReadDto>(especialidad) : null;
        }

        public async Task<EspecialidadReadDto> CreateAsync(EspecialidadCreateDto dto)
        {
            var existente = await _repository.FindAsync(e => e.Nombre.ToLower() == dto.Nombre.ToLower());
            if (existente.Any())
                throw new InvalidOperationException($"Ya existe una especialidad con el nombre '{dto.Nombre}'");

            var especialidad = _mapper.Map<Especialidad>(dto);
            var created = await _repository.AddAsync(especialidad);
            return _mapper.Map<EspecialidadReadDto>(created);
        }

        public async Task<EspecialidadReadDto?> UpdateAsync(int id, EspecialidadUpdateDto dto)
        {
            var especialidad = await _repository.GetByIdAsync(id);
            if (especialidad == null) return null;

            var duplicado = await _repository.FindAsync(e => e.Nombre.ToLower() == dto.Nombre.ToLower() && e.Id != id);
            if (duplicado.Any())
                throw new InvalidOperationException($"Ya existe una especialidad con el nombre '{dto.Nombre}'");

            especialidad.Nombre = dto.Nombre;
            await _repository.UpdateAsync(especialidad);
            return _mapper.Map<EspecialidadReadDto>(especialidad);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var especialidad = await _repository.GetByIdAsync(id);
            if (especialidad == null) return false;
            return await _repository.DeleteAsync(id);
        }
    }
}
