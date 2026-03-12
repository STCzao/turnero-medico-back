using AutoMapper;
using turnero_medico_backend.DTOs.Common;
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

        public async Task<PagedResultDto<PacienteReadDto>> GetAllPagedAsync(int page, int pageSize)
        {
            if (!_currentUserService.IsAdmin())
                throw new UnauthorizedAccessException("No tienes permisos para ver la lista de pacientes");

            var (items, total) = await _repository.GetAllPagedAsync(page, pageSize);
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
            var paciente = await _repository.GetByIdAsync(id);
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

            var pacientes = await _repository.FindAsync(p => p.UserId == userId);
            var paciente = pacientes.FirstOrDefault();

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

        // ─────────────────────────────────────────────────────────────
        // DEPENDIENTES
        // ─────────────────────────────────────────────────────────────

        public async Task<IEnumerable<PacienteReadDto>> GetMisDependientesAsync()
        {
            var userId = _currentUserService.GetUserId();
            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("No se pudo obtener el ID del usuario autenticado.");

            var dependientes = await _repository.FindAsync(p => p.ResponsableId == userId);
            return _mapper.Map<IEnumerable<PacienteReadDto>>(dependientes);
        }

        public async Task<PacienteReadDto> CreateDependienteAsync(DependienteCreateDto dto)
        {
            var userId = _currentUserService.GetUserId();
            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("No se pudo obtener el ID del usuario autenticado.");

            // Verificar que no exista un paciente con el mismo DNI
            var existentes = await _repository.FindAsync(p => p.Dni == dto.Dni);
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

            var creado = await _repository.AddAsync(dependiente);
            return _mapper.Map<PacienteReadDto>(creado);
        }
    }
}
