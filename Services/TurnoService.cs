using AutoMapper;
using Microsoft.EntityFrameworkCore;
using turnero_medico_backend.Data;
using turnero_medico_backend.DTOs.Common;
using turnero_medico_backend.DTOs.TurnoDTOs;
using turnero_medico_backend.Models.Entities;
using turnero_medico_backend.Repositories.Interfaces;
using turnero_medico_backend.Services.Interfaces;

namespace turnero_medico_backend.Services
{
    public class TurnoService(
        ITurnoRepository turnoRepository,
        IPacienteRepository pacienteRepository,
        IRepository<Doctor> doctorRepository,
        IRepository<Especialidad> especialidadRepository,
        ApplicationDbContext dbContext,
        IMapper mapper,
        ICurrentUserService currentUserService,
        IAuditService auditService) : ITurnoService
    {
        private readonly ITurnoRepository _turnoRepository = turnoRepository;
        private readonly IPacienteRepository _pacienteRepository = pacienteRepository;
        private readonly IRepository<Doctor> _doctorRepository = doctorRepository;
        private readonly IRepository<Especialidad> _especialidadRepository = especialidadRepository;
        private readonly ApplicationDbContext _dbContext = dbContext;
        private readonly IMapper _mapper = mapper;
        private readonly ICurrentUserService _currentUserService = currentUserService;
        private readonly IAuditService _auditService = auditService;

        // ─────────────────────────────────────────────────────────────
        // FILTRADO DE CAMPOS SENSIBLES SEGÚN ROL
        // ObservacionClinica: solo Doctor, Secretaria, Admin
        // NotasSecretaria:    solo Secretaria, Admin
        // ─────────────────────────────────────────────────────────────

        private TurnoReadDto FiltrarCamposSensibles(TurnoReadDto dto)
        {
            var rol = _currentUserService.GetUserRole();
            if (rol != "Doctor" && rol != "Secretaria" && rol != "Admin")
                dto.ObservacionClinica = null;
            if (rol != "Secretaria" && rol != "Admin")
                dto.NotasSecretaria = null;
            return dto;
        }

        // ─────────────────────────────────────────────────────────────
        // LECTURA
        // ─────────────────────────────────────────────────────────────

        public async Task<PagedResultDto<TurnoReadDto>> GetAllPagedAsync(int page, int pageSize, string? estado = null)
        {
            // GetAllWithDetailsPagedAsync aplica el filtro y la paginación en base de datos.
            // Soluciona el bug anterior donde el filtrado por estado cargaba todos los registros
            // en memoria antes de paginar.
            var (pagedItems, pagedTotal) = await _turnoRepository.GetAllWithDetailsPagedAsync(page, pageSize, estado);
            return new PagedResultDto<TurnoReadDto>
            {
                Items = pagedItems.Select(t => FiltrarCamposSensibles(_mapper.Map<TurnoReadDto>(t))),
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
                var turnos = await _turnoRepository.FindWithDetailsAsync(t =>
                    t.PacienteId == pacienteId &&
                    (estado == null || t.Estado == estado));
                return turnos.Select(t => FiltrarCamposSensibles(_mapper.Map<TurnoReadDto>(t)));
            }

            if (userRole == "Paciente")
            {
                var paciente = await _pacienteRepository.GetByIdAsync(pacienteId)
                    ?? throw new InvalidOperationException($"El paciente con ID {pacienteId} no existe.");

                if (paciente.UserId != userId && paciente.ResponsableId != userId)
                    throw new UnauthorizedAccessException(
                        "Solo puedes ver tus propios turnos o los de tus dependientes.");

                var turnos = await _turnoRepository.FindWithDetailsAsync(t =>
                    t.PacienteId == pacienteId &&
                    (estado == null || t.Estado == estado));
                return turnos.Select(t => FiltrarCamposSensibles(_mapper.Map<TurnoReadDto>(t)));
            }

            throw new UnauthorizedAccessException("Los doctores deben consultar turnos por doctor, no por paciente.");
        }

        public async Task<IEnumerable<TurnoReadDto>> GetByDoctorAsync(int doctorId, string? estado = null)
        {
            var userRole = _currentUserService.GetUserRole();
            var userId = _currentUserService.GetUserId();

            if (userRole == "Admin" || userRole == "Secretaria")
            {
                var turnos = await _turnoRepository.FindWithDetailsAsync(t =>
                    t.DoctorId == doctorId &&
                    (estado == null || t.Estado == estado));
                return turnos.Select(t => FiltrarCamposSensibles(_mapper.Map<TurnoReadDto>(t)));
            }

            if (userRole == "Doctor")
            {
                var doctor = await _doctorRepository.GetByIdAsync(doctorId);
                if (doctor == null || doctor.UserId != userId)
                    throw new UnauthorizedAccessException("No tienes permisos para ver los turnos de este doctor.");

                var turnos = await _turnoRepository.FindWithDetailsAsync(t =>
                    t.DoctorId == doctorId &&
                    (estado == null || t.Estado == estado));
                return turnos.Select(t => FiltrarCamposSensibles(_mapper.Map<TurnoReadDto>(t)));
            }

            throw new UnauthorizedAccessException("No tienes permisos para consultar turnos de doctores.");
        }

        public async Task<TurnoReadDto?> GetByIdAsync(int id)
        {
            var turno = await _turnoRepository.GetByIdWithDetailsAsync(id);
            if (turno == null) return null;

            var userRole = _currentUserService.GetUserRole();
            var userId = _currentUserService.GetUserId();

            if (userRole == "Admin" || userRole == "Secretaria")
                return FiltrarCamposSensibles(_mapper.Map<TurnoReadDto>(turno));

            if (userRole == "Paciente")
            {
                var paciente = await _pacienteRepository.GetByIdAsync(turno.PacienteId)
                    ?? throw new InvalidOperationException("El paciente del turno no existe.");

                if (paciente.UserId != userId && paciente.ResponsableId != userId)
                    throw new UnauthorizedAccessException("No tienes permisos para ver este turno.");

                return FiltrarCamposSensibles(_mapper.Map<TurnoReadDto>(turno));
            }

            if (userRole == "Doctor")
            {
                if (turno.DoctorId == null)
                    throw new UnauthorizedAccessException("Este turno aun no tiene doctor asignado.");

                var doctor = await _doctorRepository.GetByIdAsync(turno.DoctorId.Value);
                if (doctor?.UserId != userId)
                    throw new UnauthorizedAccessException("No tienes permisos para ver este turno.");

                return FiltrarCamposSensibles(_mapper.Map<TurnoReadDto>(turno));
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

                if (doctor.EspecialidadId != dto.EspecialidadId)
                    throw new InvalidOperationException(
                        "El doctor seleccionado no pertenece a la especialidad solicitada.");
            }

            var especialidad = await _especialidadRepository.GetByIdAsync(dto.EspecialidadId)
                ?? throw new InvalidOperationException($"La especialidad con ID {dto.EspecialidadId} no existe.");

            // Validar que la obra social del paciente cubra la especialidad solicitada (solo si tiene OS activa)
            if (dto.ObraSocialId.HasValue)
            {
                var obraSocial = await _dbContext.ObrasSociales
                .Include(o => o.Especialidades)
                .Where(o => o.Id == dto.ObraSocialId.Value)
                .FirstOrDefaultAsync()
                ?? throw new InvalidOperationException($"La obra social con Id {dto.ObraSocialId} no existe.");

                var cubre = obraSocial.Especialidades.Any(e => e.Id == dto.EspecialidadId);

                if (!cubre)
                    throw new InvalidOperationException("La obra social seleccionada no cubre la especialidad solicitada.");
            }

            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("No se pudo identificar el usuario actual.");

            var turno = new Turno
            {
                PacienteId = dto.PacienteId,
                DoctorId = dto.DoctorId,
                EspecialidadId = dto.EspecialidadId,
                Motivo = dto.Motivo,
                Estado = EstadoTurno.SolicitudPendiente,
                CreatedByUserId = userId,
                CreatedAt = DateTime.UtcNow,
                ObraSocialId = dto.ObraSocialId,
                NumeroAfiliadoDeclarado = dto.NumeroAfiliadoDeclarado,
                PlanAfiliadoDeclarado = dto.PlanAfiliadoDeclarado,
            };

            var created = await _turnoRepository.AddAsync(turno);
            await _auditService.LogAsync(AuditAccion.Crear, "Turno", created.Id.ToString());
            // Recargar con navegaciones para que PacienteNombre y DoctorNombre estén disponibles.
            var createdConDetalles = await _turnoRepository.GetByIdWithDetailsAsync(created.Id);
            return FiltrarCamposSensibles(_mapper.Map<TurnoReadDto>(createdConDetalles!));
        }

        // ─────────────────────────────────────────────────────────────
        // ACTUALIZAR (Doctor: Completado/Ausente + ObservacionClinica)
        // ─────────────────────────────────────────────────────────────

        public async Task<TurnoReadDto> UpdateAsync(int id, TurnoUpdateDto dto)
        {
            var turno = await _turnoRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Turno con ID {id} no encontrado.");

            var userRole = _currentUserService.GetUserRole();
            var userId = _currentUserService.GetUserId();

            // Validar que el estado sea uno de los permitidos para PATCH (Completado o Ausente)
            var estadosPermitidosPatch = new[] { EstadoTurno.Completado, EstadoTurno.Ausente };
            if (!string.IsNullOrEmpty(dto.Estado) && !estadosPermitidosPatch.Contains(dto.Estado))
                throw new InvalidOperationException(
                    $"Estado '{dto.Estado}' no es válido. Solo se permite: {string.Join(", ", estadosPermitidosPatch)}.");

            if (userRole == "Admin")
            {
                if (!string.IsNullOrEmpty(dto.Estado))
                    turno.Estado = dto.Estado;
                if (!string.IsNullOrEmpty(dto.ObservacionClinica))
                    turno.ObservacionClinica = dto.ObservacionClinica;

                await _turnoRepository.UpdateAsync(turno);
                await _auditService.LogAsync(AuditAccion.Actualizar, "Turno", id.ToString());
                var updatedAdmin = await _turnoRepository.GetByIdWithDetailsAsync(turno.Id);
                return FiltrarCamposSensibles(_mapper.Map<TurnoReadDto>(updatedAdmin!));
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
            await _auditService.LogAsync(AuditAccion.Actualizar, "Turno", id.ToString());
            var updatedDoctor = await _turnoRepository.GetByIdWithDetailsAsync(turno.Id);
            return FiltrarCamposSensibles(_mapper.Map<TurnoReadDto>(updatedDoctor!));
        }
        // ─────────────────────────────────────────────────────────────

        public async Task<TurnoReadDto> ConfirmarAsync(int turnoId, ConfirmarTurnoDto dto)
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var turno = await _turnoRepository.GetByIdAsync(turnoId)
                    ?? throw new KeyNotFoundException($"Turno con ID {turnoId} no encontrado.");

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

                // Validar que la especialidad del doctor coincida con la del turno
                if (doctor.EspecialidadId != turno.EspecialidadId)
                {
                    var espDoctor = doctor.EspecialidadId.HasValue ? await _especialidadRepository.GetByIdAsync(doctor.EspecialidadId.Value) : null;
                    var espTurno = turno.EspecialidadId.HasValue ? await _especialidadRepository.GetByIdAsync(turno.EspecialidadId.Value) : null;
                    throw new InvalidOperationException(
                        $"El doctor '{doctor.Nombre} {doctor.Apellido}' es especialista en '{espDoctor?.Nombre}', "
                        + $"pero el turno requiere '{espTurno?.Nombre}'.");
                }

                if (dto.FechaHora <= DateTime.UtcNow)
                    throw new InvalidOperationException("La fecha y hora del turno debe ser en el futuro.");

                // Validar que la FechaHora caiga dentro de un horario de atención configurado del doctor
                var diaSemanaConfirmar = (int)dto.FechaHora.DayOfWeek;
                var horaDelTurno = TimeOnly.FromDateTime(dto.FechaHora);
                var horario = await _dbContext.Horarios.FirstOrDefaultAsync(h =>
                    h.DoctorId == doctorId &&
                    h.DiaSemana == diaSemanaConfirmar &&
                    h.HoraInicio <= horaDelTurno &&
                    horaDelTurno < h.HoraFin)
                    ?? throw new InvalidOperationException(
                        $"El doctor no tiene horario de atención configurado para el {dto.FechaHora:dddd} a las {dto.FechaHora:HH:mm}.");

                // Verificar conflictos usando rango de tiempo (no solo igualdad exacta).
                // Dos turnos se superponen si sus intervalos [FechaHora, FechaHora+duracion) se tocan.
                var duracion = horario.DuracionMinutos;
                var slotFin = dto.FechaHora.AddMinutes(duracion);
                var slotInicio = dto.FechaHora.AddMinutes(-duracion);

                var hayConflicto = await _dbContext.Turnos.AnyAsync(t =>
                    t.DoctorId == doctorId &&
                    t.Estado == EstadoTurno.Confirmado &&
                    t.Id != turnoId &&
                    t.FechaHora.HasValue &&
                    t.FechaHora.Value < slotFin &&
                    t.FechaHora.Value > slotInicio);

                if (hayConflicto)
                    throw new InvalidOperationException(
                        $"El doctor ya tiene un turno confirmado que se superpone con el horario {dto.FechaHora:dd/MM/yyyy HH:mm}.");

                var userId = _currentUserService.GetUserId();

                turno.DoctorId = doctorId;
                turno.FechaHora = DateTime.SpecifyKind(dto.FechaHora, DateTimeKind.Utc);
                turno.Estado = EstadoTurno.Confirmado;
                turno.NotasSecretaria = dto.NotasSecretaria;
                turno.ConfirmadaPorId = userId;
                turno.FechaGestion = DateTime.UtcNow;

                await _turnoRepository.UpdateAsync(turno);
                await _auditService.LogAsync(AuditAccion.Confirmar, "Turno", turnoId.ToString());
                var confirmado = await _turnoRepository.GetByIdWithDetailsAsync(turno.Id);
                await transaction.CommitAsync();
                return FiltrarCamposSensibles(_mapper.Map<TurnoReadDto>(confirmado!));
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // ─────────────────────────────────────────────────────────────
        // RECHAZAR (Secretaria / Admin)
        // ─────────────────────────────────────────────────────────────

        public async Task<TurnoReadDto> RechazarAsync(int turnoId, RechazarTurnoDto dto)
        {
            var turno = await _turnoRepository.GetByIdAsync(turnoId)
                ?? throw new KeyNotFoundException($"Turno con ID {turnoId} no encontrado.");

            var userRole = _currentUserService.GetUserRole();
            if (userRole != "Secretaria" && userRole != "Admin")
                throw new UnauthorizedAccessException("Solo la secretaria o el administrador pueden rechazar turnos.");

            if (turno.Estado != EstadoTurno.SolicitudPendiente)
                throw new InvalidOperationException(
                    $"Solo se pueden rechazar solicitudes pendientes. Estado actual: '{turno.Estado}'.");

            var userId = _currentUserService.GetUserId();

            turno.Estado = EstadoTurno.Rechazado;
            turno.MotivoRechazo = dto.MotivoRechazo;
            turno.ConfirmadaPorId = userId;
            turno.FechaGestion = DateTime.UtcNow;

            await _turnoRepository.UpdateAsync(turno);
            await _auditService.LogAsync(AuditAccion.Rechazar, "Turno", turnoId.ToString());
            var rechazado = await _turnoRepository.GetByIdWithDetailsAsync(turno.Id);
            return FiltrarCamposSensibles(_mapper.Map<TurnoReadDto>(rechazado!));
        }

        // ─────────────────────────────────────────────────────────────
        // CANCELAR (Paciente / Doctor / Secretaria / Admin)
        // ─────────────────────────────────────────────────────────────

        public async Task<TurnoReadDto> CancelarAsync(int turnoId, CancelarTurnoDto dto)
        {
            var turno = await _turnoRepository.GetByIdAsync(turnoId)
                ?? throw new KeyNotFoundException($"Turno con ID {turnoId} no encontrado.");

            var userRole = _currentUserService.GetUserRole();
            var userId = _currentUserService.GetUserId();

            var estadosCancelables = new[] { EstadoTurno.SolicitudPendiente, EstadoTurno.Confirmado };
            if (!estadosCancelables.Contains(turno.Estado))
                throw new InvalidOperationException(
                    $"No se puede cancelar un turno en estado '{turno.Estado}'.");

            if (userRole == "Admin" || userRole == "Secretaria")
            {
                turno.Estado = EstadoTurno.Cancelado;
                turno.MotivoCancelacion = dto.Motivo;
                await _turnoRepository.UpdateAsync(turno);
                await _auditService.LogAsync(AuditAccion.Cancelar, "Turno", turnoId.ToString());
                var canceladoAdmin = await _turnoRepository.GetByIdWithDetailsAsync(turno.Id);
                return FiltrarCamposSensibles(_mapper.Map<TurnoReadDto>(canceladoAdmin!));
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

                turno.Estado = EstadoTurno.Cancelado;
                turno.MotivoCancelacion = dto.Motivo;
                await _turnoRepository.UpdateAsync(turno);
                await _auditService.LogAsync(AuditAccion.Cancelar, "Turno", turnoId.ToString());
                var canceladoDoctor = await _turnoRepository.GetByIdWithDetailsAsync(turno.Id);
                return FiltrarCamposSensibles(_mapper.Map<TurnoReadDto>(canceladoDoctor!));
            }

            if (userRole == "Paciente")
            {
                var paciente = await _pacienteRepository.GetByIdAsync(turno.PacienteId)
                    ?? throw new InvalidOperationException("El paciente del turno no existe.");

                if (paciente.UserId != userId && paciente.ResponsableId != userId)
                    throw new UnauthorizedAccessException("No tienes permisos para cancelar este turno.");

                turno.Estado = EstadoTurno.Cancelado;
                turno.MotivoCancelacion = dto.Motivo;
                await _turnoRepository.UpdateAsync(turno);
                await _auditService.LogAsync(AuditAccion.Cancelar, "Turno", turnoId.ToString());
                var canceladoPaciente = await _turnoRepository.GetByIdWithDetailsAsync(turno.Id);
                return FiltrarCamposSensibles(_mapper.Map<TurnoReadDto>(canceladoPaciente!));
            }

            throw new UnauthorizedAccessException("No tienes permisos para cancelar este turno.");
        }

        // ─────────────────────────────────────────────────────────────
        // ELIMINAR (solo Admin)
        // ─────────────────────────────────────────────────────────────

        public async Task<bool> DeleteAsync(int id)
        {
            var turno = await _turnoRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Turno con ID {id} no encontrado.");

            if (!_currentUserService.IsAdmin())
                throw new UnauthorizedAccessException("Solo el administrador puede eliminar turnos.");

            var deleted = await _turnoRepository.DeleteAsync(id);
            if (deleted)
                await _auditService.LogAsync(AuditAccion.Eliminar, "Turno", id.ToString());
            return deleted;
        }

        public async Task<bool> ExistAsync(int id)
            => await _turnoRepository.ExistAsync(id);

        // ─────────────────────────────────────────────────────────────
        // MIS TURNOS — resuelve automáticamente según rol
        // ─────────────────────────────────────────────────────────────

        public async Task<IEnumerable<TurnoReadDto>> GetMyTurnosAsync(string? estado = null)
        {
            var userId = _currentUserService.GetUserId();
            var userRole = _currentUserService.GetUserRole();

            if (userRole == "Paciente")
            {
                var pacientes = await _pacienteRepository.FindAsync(p => p.UserId == userId);
                var paciente = pacientes.FirstOrDefault()
                    ?? throw new InvalidOperationException("No se encontró un registro de paciente asociado a tu usuario.");

                var turnos = await _turnoRepository.FindWithDetailsAsync(t =>
                    t.PacienteId == paciente.Id &&
                    (estado == null || t.Estado == estado));
                return turnos.Select(t => FiltrarCamposSensibles(_mapper.Map<TurnoReadDto>(t)));
            }

            if (userRole == "Doctor")
            {
                var doctores = await _doctorRepository.FindAsync(d => d.UserId == userId);
                var doctor = doctores.FirstOrDefault()
                    ?? throw new InvalidOperationException("No se encontró un registro de doctor asociado a tu usuario.");

                var turnos = await _turnoRepository.FindWithDetailsAsync(t =>
                    t.DoctorId == doctor.Id &&
                    (estado == null || t.Estado == estado));
                return turnos.Select(t => FiltrarCamposSensibles(_mapper.Map<TurnoReadDto>(t)));
            }

            throw new UnauthorizedAccessException("Los usuarios Admin/Secretaria deben usar los endpoints de listado general.");
        }

        // ─────────────────────────────────────────────────────────────
        // AGENDA DEL DOCTOR — turnos confirmados para una fecha
        // ─────────────────────────────────────────────────────────────

        public async Task<IEnumerable<TurnoReadDto>> GetMyAgendaAsync(DateTime fecha)
        {
            var userId = _currentUserService.GetUserId();
            var userRole = _currentUserService.GetUserRole();

            if (userRole != "Doctor")
                throw new UnauthorizedAccessException("Solo los doctores pueden consultar su agenda.");

            var doctores = await _doctorRepository.FindAsync(d => d.UserId == userId);
            var doctor = doctores.FirstOrDefault()
                ?? throw new InvalidOperationException("No se encontró un registro de doctor asociado a tu usuario.");

            var fechaInicio = DateTime.SpecifyKind(fecha.Date, DateTimeKind.Utc);
            var fechaFin = fechaInicio.AddDays(1);

            var turnos = await _turnoRepository.FindWithDetailsAsync(t =>
                t.DoctorId == doctor.Id &&
                t.Estado == EstadoTurno.Confirmado &&
                t.FechaHora.HasValue &&
                t.FechaHora >= fechaInicio &&
                t.FechaHora < fechaFin);

            var ordenados = turnos.OrderBy(t => t.FechaHora);
            return ordenados.Select(t => FiltrarCamposSensibles(_mapper.Map<TurnoReadDto>(t)));
        }

        // ─────────────────────────────────────────────────────────────
        // PENDIENTES — solicitudes pendientes para la secretaria
        // ─────────────────────────────────────────────────────────────

        public async Task<PagedResultDto<TurnoReadDto>> GetPendientesAsync(int page, int pageSize)
        {
            pageSize = Math.Clamp(pageSize, 1, 100);
            var userRole = _currentUserService.GetUserRole();
            if (userRole != "Admin" && userRole != "Secretaria")
                throw new UnauthorizedAccessException("Solo Secretaria y Admin pueden ver los turnos pendientes.");

            var (items, total) = await _turnoRepository.GetAllWithDetailsPagedAsync(page, pageSize, EstadoTurno.SolicitudPendiente);
            return new PagedResultDto<TurnoReadDto>
            {
                Items = items.Select(t => FiltrarCamposSensibles(_mapper.Map<TurnoReadDto>(t))),
                Total = total,
                Page = page,
                PageSize = pageSize
            };
        }

        // ─────────────────────────────────────────────────────────────
        // HISTORIAL — turnos completados de un paciente
        // ─────────────────────────────────────────────────────────────

        public async Task<IEnumerable<TurnoReadDto>> GetHistorialAsync(int pacienteId)
        {
            var userRole = _currentUserService.GetUserRole();
            var userId = _currentUserService.GetUserId();

            if (userRole == "Paciente")
            {
                var paciente = await _pacienteRepository.GetByIdAsync(pacienteId)
                    ?? throw new InvalidOperationException($"El paciente con ID {pacienteId} no existe.");

                if (paciente.UserId != userId && paciente.ResponsableId != userId)
                    throw new UnauthorizedAccessException("Solo puedes ver el historial propio o de tus dependientes.");
            }
            else if (userRole != "Admin" && userRole != "Secretaria" && userRole != "Doctor")
            {
                throw new UnauthorizedAccessException("No tienes permisos para ver el historial.");
            }

            var turnos = await _turnoRepository.FindWithDetailsAsync(t =>
                t.PacienteId == pacienteId &&
                t.Estado == EstadoTurno.Completado);

            var ordenados = turnos.OrderByDescending(t => t.FechaHora);
            return ordenados.Select(t => FiltrarCamposSensibles(_mapper.Map<TurnoReadDto>(t)));
        }
    }
}
