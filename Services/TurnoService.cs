using AutoMapper;
using turnero_medico_backend.DTOs.Common;
using turnero_medico_backend.DTOs.TurnoDTOs;
using turnero_medico_backend.Models.Entities;
using turnero_medico_backend.Repositories.Interfaces;
using turnero_medico_backend.Services.Interfaces;

namespace turnero_medico_backend.Services
{
    public class TurnoService(
        IRepository<Turno> turnoRepository,
        IRepository<Paciente> pacienteRepository,
        IRepository<Doctor> doctorRepository,
        IRepository<ObraSocial> obraSocialRepository,
        IMapper mapper,
        CurrentUserService currentUserService) : ITurnoService
    {
        private readonly IRepository<Turno> _turnoRepository = turnoRepository;
        private readonly IRepository<Paciente> _pacienteRepository = pacienteRepository;
        private readonly IRepository<Doctor> _doctorRepository = doctorRepository;
        private readonly IRepository<ObraSocial> _obraSocialRepository = obraSocialRepository;
        private readonly IMapper _mapper = mapper;
        private readonly CurrentUserService _currentUserService = currentUserService;

        // ─────────────────────────────────────────────────────────────
        // LECTURA
        // ─────────────────────────────────────────────────────────────

        public async Task<IEnumerable<TurnoReadDto>> GetAllAsync()
        {
            var userRole = _currentUserService.GetUserRole();
            if (userRole != "Admin" && userRole != "Secretaria")
                throw new UnauthorizedAccessException("No tienes permisos para ver el listado completo de turnos.");

            var turnos = await _turnoRepository.GetAllAsync();
            return turnos.Select(t => _mapper.Map<TurnoReadDto>(t));
        }

        public async Task<PagedResultDto<TurnoReadDto>> GetAllPagedAsync(int page, int pageSize, string? estado = null)
        {
            var userRole = _currentUserService.GetUserRole();
            if (userRole != "Admin" && userRole != "Secretaria")
                throw new UnauthorizedAccessException("No tienes permisos para ver el listado completo de turnos.");

            // Si hay filtro de estado, usamos FindAsync con predicado; si no, paginación directa
            if (!string.IsNullOrWhiteSpace(estado))
            {
                var filtrados = await _turnoRepository.FindAsync(t => t.Estado == estado);
                var total = filtrados.Count();
                var items = filtrados
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize);
                return new PagedResultDto<TurnoReadDto>
                {
                    Items = items.Select(t => _mapper.Map<TurnoReadDto>(t)),
                    Total = total,
                    Page = page,
                    PageSize = pageSize
                };
            }

            var (pagedItems, pagedTotal) = await _turnoRepository.GetAllPagedAsync(page, pageSize);
            return new PagedResultDto<TurnoReadDto>
            {
                Items = pagedItems.Select(t => _mapper.Map<TurnoReadDto>(t)),
                Total = pagedTotal,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<IEnumerable<TurnoReadDto>> GetByPacienteAsync(int pacienteId, string? estado = null)
        {
            var userRole = _currentUserService.GetUserRole();
            var userId = _currentUserService.GetUserId();

            if (userRole == "Admin" || userRole == "Secretaria")
            {
                var turnos = await _turnoRepository.FindAsync(t =>
                    t.PacienteId == pacienteId &&
                    (estado == null || t.Estado == estado));
                return turnos.Select(t => _mapper.Map<TurnoReadDto>(t));
            }

            if (userRole == "Paciente")
            {
                var paciente = await _pacienteRepository.GetByIdAsync(pacienteId)
                    ?? throw new InvalidOperationException($"El paciente con ID {pacienteId} no existe.");

                if (paciente.UserId != userId && paciente.ResponsableId != userId)
                    throw new UnauthorizedAccessException(
                        "Solo puedes ver tus propios turnos o los de tus dependientes.");

                var turnos = await _turnoRepository.FindAsync(t =>
                    t.PacienteId == pacienteId &&
                    (estado == null || t.Estado == estado));
                return turnos.Select(t => _mapper.Map<TurnoReadDto>(t));
            }

            throw new UnauthorizedAccessException("Los doctores deben consultar turnos por doctor, no por paciente.");
        }

        public async Task<IEnumerable<TurnoReadDto>> GetByDoctorAsync(int doctorId, string? estado = null)
        {
            var userRole = _currentUserService.GetUserRole();
            var userId = _currentUserService.GetUserId();

            if (userRole == "Admin" || userRole == "Secretaria")
            {
                var turnos = await _turnoRepository.FindAsync(t =>
                    t.DoctorId == doctorId &&
                    (estado == null || t.Estado == estado));
                return turnos.Select(t => _mapper.Map<TurnoReadDto>(t));
            }

            if (userRole == "Doctor")
            {
                var doctor = await _doctorRepository.GetByIdAsync(doctorId);
                if (doctor == null || doctor.UserId != userId)
                    throw new UnauthorizedAccessException("No tienes permisos para ver los turnos de este doctor.");

                var turnos = await _turnoRepository.FindAsync(t =>
                    t.DoctorId == doctorId &&
                    (estado == null || t.Estado == estado));
                return turnos.Select(t => _mapper.Map<TurnoReadDto>(t));
            }

            throw new UnauthorizedAccessException("No tienes permisos para consultar turnos de doctores.");
        }

        public async Task<TurnoReadDto?> GetByIdAsync(int id)
        {
            var turno = await _turnoRepository.GetByIdAsync(id);
            if (turno == null) return null;

            var userRole = _currentUserService.GetUserRole();
            var userId = _currentUserService.GetUserId();

            if (userRole == "Admin" || userRole == "Secretaria")
                return _mapper.Map<TurnoReadDto>(turno);

            if (userRole == "Paciente")
            {
                var paciente = await _pacienteRepository.GetByIdAsync(turno.PacienteId)
                    ?? throw new InvalidOperationException("El paciente del turno no existe.");

                if (paciente.UserId != userId && paciente.ResponsableId != userId)
                    throw new UnauthorizedAccessException("No tienes permisos para ver este turno.");

                return _mapper.Map<TurnoReadDto>(turno);
            }

            if (userRole == "Doctor")
            {
                if (turno.DoctorId == null)
                    throw new UnauthorizedAccessException("Este turno aun no tiene doctor asignado.");

                var doctor = await _doctorRepository.GetByIdAsync(turno.DoctorId.Value);
                if (doctor?.UserId != userId)
                    throw new UnauthorizedAccessException("No tienes permisos para ver este turno.");

                return _mapper.Map<TurnoReadDto>(turno);
            }

            throw new UnauthorizedAccessException("No tienes permisos para ver este turno.");
        }

        // ─────────────────────────────────────────────────────────────
        // CREAR SOLICITUD
        // ─────────────────────────────────────────────────────────────

        public async Task<TurnoReadDto> CreateAsync(TurnoCreateDto dto)
        {
            var userRole = _currentUserService.GetUserRole();
            var userId = _currentUserService.GetUserId();

            if (userRole == "Doctor")
                throw new UnauthorizedAccessException("Los doctores no pueden crear solicitudes de turno.");

            var paciente = await _pacienteRepository.GetByIdAsync(dto.PacienteId)
                ?? throw new InvalidOperationException($"El paciente con ID {dto.PacienteId} no existe.");

            if (userRole == "Paciente")
            {
                if (paciente.UserId != userId && paciente.ResponsableId != userId)
                    throw new UnauthorizedAccessException(
                        "Solo puedes solicitar turnos para ti mismo o para tus dependientes.");
            }

            if (dto.DoctorId.HasValue)
            {
                var doctor = await _doctorRepository.GetByIdAsync(dto.DoctorId.Value)
                    ?? throw new InvalidOperationException($"El doctor con ID {dto.DoctorId} no existe.");

                if (!doctor.Especialidad.Equals(dto.Especialidad, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException(
                        $"El doctor no es especialista en '{dto.Especialidad}'. Su especialidad es '{doctor.Especialidad}'.");
            }

            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("No se pudo identificar el usuario actual.");

            var turno = new Turno
            {
                PacienteId              = dto.PacienteId,
                DoctorId                = dto.DoctorId,
                Especialidad            = dto.Especialidad,
                Motivo                  = dto.Motivo,
                Estado                  = EstadoTurno.SolicitudPendiente,
                CreatedByUserId         = userId,
                CreatedAt               = DateTime.UtcNow,
                ObraSocialId            = paciente.ObraSocialId,
                NumeroAfiliadoDeclarado = dto.NumeroAfiliadoDeclarado,
                PlanAfiliadoDeclarado   = dto.PlanAfiliadoDeclarado,
            };

            var created = await _turnoRepository.AddAsync(turno);
            return _mapper.Map<TurnoReadDto>(created);
        }

        // ─────────────────────────────────────────────────────────────
        // ACTUALIZAR (Doctor: Completado/Ausente + ObservacionClinica)
        // ─────────────────────────────────────────────────────────────

        public async Task<TurnoReadDto?> UpdateAsync(int id, TurnoUpdateDto dto)
        {
            var turno = await _turnoRepository.GetByIdAsync(id);
            if (turno == null) return null;

            var userRole = _currentUserService.GetUserRole();
            var userId = _currentUserService.GetUserId();

            if (userRole == "Admin")
            {
                if (!string.IsNullOrEmpty(dto.Estado))
                    turno.Estado = dto.Estado;
                if (!string.IsNullOrEmpty(dto.ObservacionClinica))
                    turno.ObservacionClinica = dto.ObservacionClinica;

                await _turnoRepository.UpdateAsync(turno);
                return _mapper.Map<TurnoReadDto>(turno);
            }

            if (userRole != "Doctor")
                throw new UnauthorizedAccessException("Solo los doctores pueden actualizar el estado clinico del turno.");

            if (turno.DoctorId == null)
                throw new InvalidOperationException("Este turno no tiene doctor asignado aun.");

            var doctorDelTurno = await _doctorRepository.GetByIdAsync(turno.DoctorId.Value);
            if (doctorDelTurno?.UserId != userId)
                throw new UnauthorizedAccessException("No tienes permisos para modificar este turno.");

            if (turno.Estado != EstadoTurno.Confirmado)
                throw new InvalidOperationException(
                    $"Solo puedes actualizar turnos confirmados. Estado actual: '{turno.Estado}'.");

            if (!string.IsNullOrEmpty(dto.Estado))
                turno.Estado = dto.Estado;

            if (!string.IsNullOrEmpty(dto.ObservacionClinica))
                turno.ObservacionClinica = dto.ObservacionClinica;

            await _turnoRepository.UpdateAsync(turno);
            return _mapper.Map<TurnoReadDto>(turno);
        }

        // ─────────────────────────────────────────────────────────────
        // CONFIRMAR (Secretaria / Admin)
        // ─────────────────────────────────────────────────────────────

        public async Task<TurnoReadDto?> ConfirmarAsync(int turnoId, ConfirmarTurnoDto dto)
        {
            var turno = await _turnoRepository.GetByIdAsync(turnoId);
            if (turno == null) return null;

            var userRole = _currentUserService.GetUserRole();
            if (userRole != "Secretaria" && userRole != "Admin")
                throw new UnauthorizedAccessException("Solo la secretaria o el administrador pueden confirmar turnos.");

            if (turno.Estado != EstadoTurno.SolicitudPendiente)
                throw new InvalidOperationException(
                    $"Solo se pueden confirmar solicitudes pendientes. Estado actual: '{turno.Estado}'.");

            var doctorId = dto.DoctorId ?? turno.DoctorId
                ?? throw new InvalidOperationException(
                    "Debe asignar un doctor al confirmar el turno. El paciente no eligio uno al solicitar.");

            var doctor = await _doctorRepository.GetByIdAsync(doctorId)
                ?? throw new InvalidOperationException($"El doctor con ID {doctorId} no existe.");

            if (dto.FechaHora <= DateTime.UtcNow)
                throw new InvalidOperationException("La fecha y hora del turno debe ser en el futuro.");

            var turnosConflicto = await _turnoRepository.FindAsync(t =>
                t.DoctorId == doctorId &&
                t.FechaHora == dto.FechaHora &&
                t.Estado == EstadoTurno.Confirmado &&
                t.Id != turnoId);

            if (turnosConflicto.Any())
                throw new InvalidOperationException(
                    $"El doctor ya tiene un turno confirmado para el {dto.FechaHora:dd/MM/yyyy HH:mm}.");

            var userId = _currentUserService.GetUserId();

            turno.DoctorId        = doctorId;
            turno.FechaHora       = dto.FechaHora;
            turno.Estado          = EstadoTurno.Confirmado;
            turno.NotasSecretaria = dto.NotasSecretaria;
            turno.ConfirmadaPorId = userId;
            turno.FechaGestion    = DateTime.UtcNow;

            await _turnoRepository.UpdateAsync(turno);
            return _mapper.Map<TurnoReadDto>(turno);
        }

        // ─────────────────────────────────────────────────────────────
        // RECHAZAR (Secretaria / Admin)
        // ─────────────────────────────────────────────────────────────

        public async Task<TurnoReadDto?> RechazarAsync(int turnoId, RechazarTurnoDto dto)
        {
            var turno = await _turnoRepository.GetByIdAsync(turnoId);
            if (turno == null) return null;

            var userRole = _currentUserService.GetUserRole();
            if (userRole != "Secretaria" && userRole != "Admin")
                throw new UnauthorizedAccessException("Solo la secretaria o el administrador pueden rechazar turnos.");

            if (turno.Estado != EstadoTurno.SolicitudPendiente)
                throw new InvalidOperationException(
                    $"Solo se pueden rechazar solicitudes pendientes. Estado actual: '{turno.Estado}'.");

            var userId = _currentUserService.GetUserId();

            turno.Estado          = EstadoTurno.Rechazado;
            turno.MotivoRechazo   = dto.MotivoRechazo;
            turno.ConfirmadaPorId = userId;
            turno.FechaGestion    = DateTime.UtcNow;

            await _turnoRepository.UpdateAsync(turno);
            return _mapper.Map<TurnoReadDto>(turno);
        }

        // ─────────────────────────────────────────────────────────────
        // CANCELAR (Paciente / Doctor / Secretaria / Admin)
        // ─────────────────────────────────────────────────────────────

        public async Task<TurnoReadDto?> CancelarAsync(int turnoId, CancelarTurnoDto dto)
        {
            var turno = await _turnoRepository.GetByIdAsync(turnoId);
            if (turno == null) return null;

            var userRole = _currentUserService.GetUserRole();
            var userId = _currentUserService.GetUserId();

            var estadosCancelables = new[] { EstadoTurno.SolicitudPendiente, EstadoTurno.Confirmado };
            if (!estadosCancelables.Contains(turno.Estado))
                throw new InvalidOperationException(
                    $"No se puede cancelar un turno en estado '{turno.Estado}'.");

            if (userRole == "Admin" || userRole == "Secretaria")
            {
                turno.Estado        = EstadoTurno.Cancelado;
                turno.MotivoRechazo = dto.Motivo;
                await _turnoRepository.UpdateAsync(turno);
                return _mapper.Map<TurnoReadDto>(turno);
            }

            if (userRole == "Doctor")
            {
                if (turno.DoctorId == null)
                    throw new UnauthorizedAccessException("Este turno no tiene doctor asignado.");

                var doctor = await _doctorRepository.GetByIdAsync(turno.DoctorId.Value);
                if (doctor?.UserId != userId)
                    throw new UnauthorizedAccessException("No tienes permisos para cancelar este turno.");

                if (turno.Estado != EstadoTurno.Confirmado)
                    throw new InvalidOperationException("El doctor solo puede cancelar turnos confirmados.");

                turno.Estado        = EstadoTurno.Cancelado;
                turno.MotivoRechazo = dto.Motivo;
                await _turnoRepository.UpdateAsync(turno);
                return _mapper.Map<TurnoReadDto>(turno);
            }

            if (userRole == "Paciente")
            {
                var paciente = await _pacienteRepository.GetByIdAsync(turno.PacienteId)
                    ?? throw new InvalidOperationException("El paciente del turno no existe.");

                if (paciente.UserId != userId && paciente.ResponsableId != userId)
                    throw new UnauthorizedAccessException("No tienes permisos para cancelar este turno.");

                turno.Estado        = EstadoTurno.Cancelado;
                turno.MotivoRechazo = dto.Motivo;
                await _turnoRepository.UpdateAsync(turno);
                return _mapper.Map<TurnoReadDto>(turno);
            }

            throw new UnauthorizedAccessException("No tienes permisos para cancelar este turno.");
        }

        // ─────────────────────────────────────────────────────────────
        // ELIMINAR (solo Admin)
        // ─────────────────────────────────────────────────────────────

        public async Task<bool> DeleteAsync(int id)
        {
            var turno = await _turnoRepository.GetByIdAsync(id);
            if (turno == null) return false;

            if (!_currentUserService.IsAdmin())
                throw new UnauthorizedAccessException("Solo el administrador puede eliminar turnos.");

            return await _turnoRepository.DeleteAsync(id);
        }

        public async Task<bool> ExistAsync(int id)
        {
            var turno = await _turnoRepository.GetByIdAsync(id);
            return turno != null;
        }
    }
}
