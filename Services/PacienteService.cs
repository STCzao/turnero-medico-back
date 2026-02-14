using turnero_medico_backend.DTOs.PacienteDTOs;
using turnero_medico_backend.Models.Entities;
using turnero_medico_backend.Repositories.Interfaces;
using turnero_medico_backend.Services.Interfaces;

namespace turnero_medico_backend.Services
{
    public class PacienteService(IRepository<Paciente> _repository) : IPacienteService
    {
        public async Task<IEnumerable<PacienteReadDto>> GetAllAsync()
        {
            var pacientes = await _repository.GetAllAsync();
            return pacientes.Select(p => Mapper.MapToPacienteReadDto(p));
        }

        public async Task<PacienteReadDto?> GetByIdAsync(int id)
        {
            var paciente = await _repository.GetByIdAsync(id);
            return paciente == null ? null : Mapper.MapToPacienteReadDto(paciente);
        }

        public async Task<PacienteReadDto> CreateAsync(PacienteCreateDto dto)
        {
            var paciente = Mapper.MapToPaciente(dto);
            var createdPaciente = await _repository.AddAsync(paciente);
            return Mapper.MapToPacienteReadDto(createdPaciente);
        }

        public async Task<PacienteReadDto?> UpdateAsync(int id, PacienteUpdateDto dto)
        {
            var paciente = await _repository.GetByIdAsync(id);
            if (paciente == null)
                return null;

            var updatedPaciente = Mapper.MapToPaciente(dto, paciente);
            await _repository.UpdateAsync(updatedPaciente);
            return Mapper.MapToPacienteReadDto(updatedPaciente);
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
