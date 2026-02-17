using AutoMapper;
using turnero_medico_backend.DTOs.PacienteDTOs;
using turnero_medico_backend.Models.Entities;
using turnero_medico_backend.Repositories.Interfaces;
using turnero_medico_backend.Services.Interfaces;

namespace turnero_medico_backend.Services
{
    public class PacienteService(
        IRepository<Paciente> _repository,
        IMapper _mapper,
        CurrentUserService _currentUserService
    ) : IPacienteService
    {
        public async Task<IEnumerable<PacienteReadDto>> GetAllAsync()
        {
            var pacientes = await _repository.GetAllAsync();
            
            // Admin ve todos, Paciente ve solo sus datos
            if (!_currentUserService.IsAdmin())
            {
                // Solo admins ven la lista completa
                throw new UnauthorizedAccessException("No tienes permisos para ver la lista de pacientes");
            }
            
            return _mapper.Map<IEnumerable<PacienteReadDto>>(pacientes);
        }

        public async Task<PacienteReadDto?> GetByIdAsync(int id)
        {
            var paciente = await _repository.GetByIdAsync(id);
            if (paciente == null)
                return null;

            return _mapper.Map<PacienteReadDto>(paciente);
        }

        public async Task<PacienteReadDto> CreateAsync(PacienteCreateDto dto)
        {
            var paciente = _mapper.Map<Paciente>(dto);
            var createdPaciente = await _repository.AddAsync(paciente);
            return _mapper.Map<PacienteReadDto>(createdPaciente);
        }

        public async Task<PacienteReadDto?> UpdateAsync(int id, PacienteUpdateDto dto)
        {
            var paciente = await _repository.GetByIdAsync(id);
            if (paciente == null)
                return null;

            _mapper.Map(dto, paciente);
            await _repository.UpdateAsync(paciente);
            return _mapper.Map<PacienteReadDto>(paciente);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            return await _repository.DeleteAsync(id);
        }

        public async Task<bool> ExistAsync(int id)
        {
            var paciente = await _repository.GetByIdAsync(id);
            return paciente != null;
        }
    }
}
