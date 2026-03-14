using AutoMapper;
using turnero_medico_backend.DTOs.Common;
using turnero_medico_backend.DTOs.PacienteDTOs;
using turnero_medico_backend.Models.Entities;
using turnero_medico_backend.Repositories.Interfaces;
using turnero_medico_backend.Services.Interfaces;

namespace turnero_medico_backend.Services
{
    public class PacienteService(
        IPacienteRepository _pacienteRepository,
        IMapper _mapper,
        ICurrentUserService _currentUserService
    ) : IPacienteService
    {
        public async Task<IEnumerable<PacienteReadDto>> GetAllAsync()
        {
            if (!_currentUserService.IsAdmin())
                throw new UnauthorizedAccessException("No tienes permisos para ver la lista de pacientes");

            var pacientes = await _pacienteRepository.GetAllWithObraSocialAsync();
            return _mapper.Map<IEnumerable<PacienteReadDto>>(pacientes);
        }

        public async Task<PagedResultDto<PacienteReadDto>> GetAllPagedAsync(int page, int pageSize)
        {
            if (!_currentUserService.IsAdmin())
                throw new UnauthorizedAccessException("No tienes permisos para ver la lista de pacientes");

            var (items, total) = await _pacienteRepository.GetAllWithObraSocialPagedAsync(page, pageSize);
            return new PagedResultDto<PacienteReadDto>
            {
                Items = _mapper.Map<IEnumerable<PacienteReadDto>>(items),
                Total = total,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<PacienteReadDto?> GetByIdAsync(int id)
        {
            var paciente = await _pacienteRepository.GetByIdWithObraSocialAsync(id);
            if (paciente == null)
                return null;

            return _mapper.Map<PacienteReadDto>(paciente);
        }

        // Obtiene el perfil del paciente autenticado actual
        public async Task<PacienteReadDto?> GetMyProfileAsync()
        {
            var userId = _currentUserService.GetUserId();
            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("No se pudo obtener el ID del usuario autenticado");

            var pacientes = await _pacienteRepository.FindWithObraSocialAsync(p => p.UserId == userId);
            var paciente = pacientes.FirstOrDefault();

            if (paciente == null)
                return null;

            return _mapper.Map<PacienteReadDto>(paciente);
        }

        public async Task<PacienteReadDto> CreateAsync(PacienteCreateDto dto)
        {
            var paciente = _mapper.Map<Paciente>(dto);
            var createdPaciente = await _pacienteRepository.AddAsync(paciente);
            return _mapper.Map<PacienteReadDto>(createdPaciente);
        }

        public async Task<PacienteReadDto?> UpdateAsync(int id, PacienteUpdateDto dto)
        {
            var paciente = await _pacienteRepository.GetByIdAsync(id);
            if (paciente == null)
                return null;

            _mapper.Map(dto, paciente);
            await _pacienteRepository.UpdateAsync(paciente);
            return _mapper.Map<PacienteReadDto>(paciente);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            return await _pacienteRepository.DeleteAsync(id);
        }

        public async Task<bool> ExistAsync(int id)
        {
            var paciente = await _pacienteRepository.GetByIdAsync(id);
            return paciente != null;
        }

        // ─────────────────────────────────────────────────────────────
        // DEPENDIENTES
        // ─────────────────────────────────────────────────────────────

        public async Task<IEnumerable<PacienteReadDto>> GetMisDependientesAsync()
        {
            var userId = _currentUserService.GetUserId();
            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("No se pudo obtener el ID del usuario autenticado.");

            var dependientes = await _pacienteRepository.FindWithObraSocialAsync(p => p.ResponsableId == userId);
            return _mapper.Map<IEnumerable<PacienteReadDto>>(dependientes);
        }

        public async Task<PacienteReadDto> CreateDependienteAsync(DependienteCreateDto dto)
        {
            var userId = _currentUserService.GetUserId();
            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("No se pudo obtener el ID del usuario autenticado.");

            // Verificar que no exista un paciente con el mismo DNI
            var existentes = await _pacienteRepository.FindAsync(p => p.Dni == dto.Dni);
            if (existentes.Any())
                throw new InvalidOperationException($"Ya existe un paciente con DNI {dto.Dni}.");

            var dependiente = new Paciente
            {
                Dni = dto.Dni,
                Nombre = dto.Nombre,
                Apellido = dto.Apellido,
                FechaNacimiento = dto.FechaNacimiento,
                Telefono = dto.Telefono ?? string.Empty,
                ResponsableId = userId,
                EsMayorDeEdad = false,
                UserId = null // Los dependientes no tienen cuenta de usuario
            };

            var creado = await _pacienteRepository.AddAsync(dependiente);
            return _mapper.Map<PacienteReadDto>(creado);
        }
    }
}
