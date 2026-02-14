using turnero_medico_backend.DTOs.TurnoDTOs;
using turnero_medico_backend.Models.Entities;
using turnero_medico_backend.Repositories.Interfaces;
using turnero_medico_backend.Services.Interfaces;

namespace turnero_medico_backend.Services
{
    public class TurnoService(
        IRepository<Turno> turnoRepository,
        IRepository<Paciente> pacienteRepository,
        IRepository<Doctor> doctorRepository) : ITurnoService
    {
        private readonly IRepository<Turno> _turnoRepository = turnoRepository;
        private readonly IRepository<Paciente> _pacienteRepository = pacienteRepository;
        private readonly IRepository<Doctor> _doctorRepository = doctorRepository;

        public async Task<IEnumerable<TurnoReadDto>> GetAllAsync()
        {
            var turnos = await _turnoRepository.GetAllAsync();
            return turnos.Select(t => Mapper.MapToTurnoReadDto(t));
        }

        public async Task<IEnumerable<TurnoReadDto>> GetByPacienteAsync(int pacienteId)
        {
            var turnos = await _turnoRepository.FindAsync(t => t.PacienteId == pacienteId);
            return turnos.Select(t => Mapper.MapToTurnoReadDto(t));
        }

        public async Task<IEnumerable<TurnoReadDto>> GetByDoctorAsync(int doctorId)
        {
            var turnos = await _turnoRepository.FindAsync(t => t.DoctorId == doctorId);
            return turnos.Select(t => Mapper.MapToTurnoReadDto(t));
        }

        public async Task<TurnoReadDto?> GetByIdAsync(int id)
        {
            var turno = await _turnoRepository.GetByIdAsync(id);
            return turno == null ? null : Mapper.MapToTurnoReadDto(turno);
        }

        public async Task<TurnoReadDto> CreateAsync(TurnoCreateDto dto)
        {
            // Validar que paciente y doctor existan
            var pacienteExiste = await _pacienteRepository.GetByIdAsync(dto.PacienteId);
            if (pacienteExiste == null)
                throw new InvalidOperationException($"El paciente con ID {dto.PacienteId} no existe");

            var doctorExiste = await _doctorRepository.GetByIdAsync(dto.DoctorId);
            if (doctorExiste == null)
                throw new InvalidOperationException($"El doctor con ID {dto.DoctorId} no existe");

            // Validar que el doctor tenga la especialidad solicitada
            if (!doctorExiste.Especialidad.Equals(dto.Especialidad, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"El doctor no es especialista en {dto.Especialidad}");

            var turno = Mapper.MapToTurno(dto);
            var createdTurno = await _turnoRepository.AddAsync(turno);
            return Mapper.MapToTurnoReadDto(createdTurno);
        }

        public async Task<TurnoReadDto?> UpdateAsync(int id, TurnoUpdateDto dto)
        {
            var turno = await _turnoRepository.GetByIdAsync(id);
            if (turno == null)
                return null;

            // Si cambia de doctor, validar que exista
            if (dto.DoctorId.HasValue && dto.DoctorId != turno.DoctorId)
            {
                var doctorExiste = await _doctorRepository.GetByIdAsync(dto.DoctorId.Value);
                if (doctorExiste == null)
                    throw new InvalidOperationException($"El doctor con ID {dto.DoctorId} no existe");
            }

            var updatedTurno = Mapper.MapToTurno(dto, turno);
            await _turnoRepository.UpdateAsync(updatedTurno);
            return Mapper.MapToTurnoReadDto(updatedTurno);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            return await _turnoRepository.DeleteAsync(id);
        }

        public async Task<bool> ExistAsync(int id)
        {
            var turno = await _turnoRepository.GetByIdAsync(id);
            return turno != null;
        }
    }
}
