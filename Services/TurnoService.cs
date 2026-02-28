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

        public async Task<IEnumerable<TurnoReadDto>> GetAllAsync()
        {
            if (!_currentUserService.IsAdmin())
                throw new UnauthorizedAccessException("No tienes permisos para ver el listado de turnos.");

            var turnos = await _turnoRepository.GetAllAsync();
            return turnos.Select(t => _mapper.Map<TurnoReadDto>(t));
        }

        public async Task<PagedResultDto<TurnoReadDto>> GetAllPagedAsync(int page, int pageSize)
        {
            if (!_currentUserService.IsAdmin())
                throw new UnauthorizedAccessException("No tienes permisos para ver el listado de turnos.");

            var (items, total) = await _turnoRepository.GetAllPagedAsync(page, pageSize);
            return new PagedResultDto<TurnoReadDto>
            {
                Items = items.Select(t => _mapper.Map<TurnoReadDto>(t)),
                Total = total,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<IEnumerable<TurnoReadDto>> GetByPacienteAsync(int pacienteId)
        {
            var userRole = _currentUserService.GetUserRole();
            var userEmail = _currentUserService.GetUserEmail();
            var userId = _currentUserService.GetUserId();

            // Admin ve todos los turnos de cualquier paciente
            if (_currentUserService.IsAdmin())
            {
                var turnos = await _turnoRepository.FindAsync(t => t.PacienteId == pacienteId);
                return turnos.Select(t => _mapper.Map<TurnoReadDto>(t));
            }

            // Paciente solo ve sus propios turnos o los de sus dependientes
            if (userRole == "Paciente")
            {
                var paciente = await _pacienteRepository.GetByIdAsync(pacienteId);
                if (paciente == null)
                    throw new InvalidOperationException($"El paciente con ID {pacienteId} no existe");

                // Caso 1: Es su propio paciente
                if (paciente.UserId == userId)
                {
                    var turnos = await _turnoRepository.FindAsync(t => t.PacienteId == pacienteId);
                    return turnos.Select(t => _mapper.Map<TurnoReadDto>(t));
                }

                // Caso 2: Es responsable del paciente (mamá viendo turnos del esposo/hijo dependiente)
                if (paciente.ResponsableId == userId)
                {
                    var turnos = await _turnoRepository.FindAsync(t => t.PacienteId == pacienteId);
                    return turnos.Select(t => _mapper.Map<TurnoReadDto>(t));
                }

                // Caso 3: No tiene permiso
                throw new UnauthorizedAccessException(
                    "No tienes permisos para ver los turnos de este paciente. " +
                    "Solo puedes ver tus propios turnos o los de tus dependientes.");
            }

            // Doctor no puede ver turnos de pacientes específicos
            throw new UnauthorizedAccessException("Los doctores no pueden consultar turnos por paciente.");
        }

        public async Task<IEnumerable<TurnoReadDto>> GetByDoctorAsync(int doctorId)
        {
            var userRole = _currentUserService.GetUserRole();
            var userId = _currentUserService.GetUserId();

            // Admin ve todos los turnos de cualquier doctor
            if (_currentUserService.IsAdmin())
            {
                var turnos = await _turnoRepository.FindAsync(t => t.DoctorId == doctorId);
                return turnos.Select(t => _mapper.Map<TurnoReadDto>(t));
            }

            // Doctor solo ve sus propios turnos
            if (userRole == "Doctor")
            {
                var doctor = await _doctorRepository.GetByIdAsync(doctorId);
                if (doctor == null || doctor.UserId != userId)
                    throw new UnauthorizedAccessException("No tienes permisos para ver los turnos de este doctor.");

                var turnos = await _turnoRepository.FindAsync(t => t.DoctorId == doctorId);
                return turnos.Select(t => _mapper.Map<TurnoReadDto>(t));
            }

            // Paciente no puede ver turnos de doctores
            throw new UnauthorizedAccessException("No tienes permisos para consultar turnos de doctores.");
        }

        public async Task<TurnoReadDto?> GetByIdAsync(int id)
        {
            var turno = await _turnoRepository.GetByIdAsync(id);
            if (turno == null)
                return null;

            var userRole = _currentUserService.GetUserRole();
            var userEmail = _currentUserService.GetUserEmail();
            var userId = _currentUserService.GetUserId();

            // Admin ve cualquier turno
            if (_currentUserService.IsAdmin())
                return _mapper.Map<TurnoReadDto>(turno);

            // Paciente solo ve sus propios turnos o los de sus dependientes
            if (userRole == "Paciente")
            {
                var paciente = await _pacienteRepository.GetByIdAsync(turno.PacienteId)
                    ?? throw new InvalidOperationException("El paciente del turno no existe");

                // Caso 1: Es su propio paciente
                if (paciente.UserId == userId)
                    return _mapper.Map<TurnoReadDto>(turno);

                // Caso 2: Es responsable del paciente (mamá viendo turno del esposo/hijo)
                if (paciente.ResponsableId == userId)
                    return _mapper.Map<TurnoReadDto>(turno);

                // Caso 3: No tiene permiso
                throw new UnauthorizedAccessException(
                    "No tienes permisos para ver este turno. " +
                    "Solo puedes ver tus propios turnos o los de tus dependientes.");
            }
            // Doctor solo ve sus propios turnos
            else if (userRole == "Doctor")
            {
                var doctor = await _doctorRepository.GetByIdAsync(turno.DoctorId);
                if (doctor?.UserId != userId)
                    throw new UnauthorizedAccessException("No tienes permisos para ver este turno.");
            }

            return _mapper.Map<TurnoReadDto>(turno);
        }

        public async Task<TurnoReadDto> CreateAsync(TurnoCreateDto dto)
        {
            var userRole = _currentUserService.GetUserRole();
            var userEmail = _currentUserService.GetUserEmail();
            var userId = _currentUserService.GetUserId();

            // Solo Admin y Paciente pueden crear turnos
            if (userRole != "Admin" && userRole != "Paciente")
                throw new UnauthorizedAccessException("Los doctores no pueden crear turnos. Contacta con un administrador.");

            // Validar que paciente y doctor existan
            var pacienteExiste = await _pacienteRepository.GetByIdAsync(dto.PacienteId) 
                ?? throw new InvalidOperationException($"El paciente con ID {dto.PacienteId} no existe");

            var doctorExiste = await _doctorRepository.GetByIdAsync(dto.DoctorId) 
                ?? throw new InvalidOperationException($"El doctor con ID {dto.DoctorId} no existe");

            // Validar que el doctor tenga la especialidad solicitada
            if (!doctorExiste.Especialidad.Equals(dto.Especialidad, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"El doctor no es especialista en {dto.Especialidad}");

            //  Validación de familia: Paciente crea turnos solo para sí mismo o para sus dependientes 
            if (userRole == "Paciente")
            {
                // Creando turno para sí mismo
                if (pacienteExiste.UserId == userId)
                {
                    // Permitido
                }
                // Creando turno para un dependiente (mamá crea para esposo/hijo)
                else if (pacienteExiste.ResponsableId == userId)
                {
                    // Permitido - es responsable
                }
                // Sin permisos
                else
                {
                    throw new UnauthorizedAccessException(
                        "Solo puedes crear turnos para ti mismo o para tus dependientes. " +
                        "No tienes permiso para crear turnos para este paciente.");
                }
            }

            // Verificar que no haya otro turno en la misma hora
            var turnosDuplcados = await _turnoRepository.FindAsync(t => 
                t.DoctorId == dto.DoctorId && 
                t.FechaHora == dto.FechaHora);
            
            if (turnosDuplcados.Any())
                throw new InvalidOperationException(
                    $"El doctor ya tiene un turno agendado para el {dto.FechaHora:dd/MM/yyyy HH:mm}. " +
                    $"Por favor, elige otro horario.");

            var turno = _mapper.Map<Turno>(dto);
            
            // ===== Validación de Obra Social =====
            if (pacienteExiste.ObraSocialId.HasValue)
            {
                var obraSocial = await _obraSocialRepository.GetByIdAsync(pacienteExiste.ObraSocialId.Value);
                if (obraSocial == null)
                    throw new InvalidOperationException(
                        $"La obra social del paciente (ID: {pacienteExiste.ObraSocialId}) no existe. Actualiza la información del paciente.");

                bool cubre = obraSocial.Especialidades
                    .Any(e => e.Equals(dto.Especialidad, StringComparison.OrdinalIgnoreCase));

                if (!cubre)
                    throw new InvalidOperationException(
                        $"La obra social '{obraSocial.Nombre}' no cubre la especialidad '{dto.Especialidad}'. " +
                        $"Verificá con tu obra social o cambiá a pago particular.");

                turno.Estado = EstadoTurno.Aceptado;
            }
            else if (pacienteExiste.TipoPago == TipoPago.Particular)
            {
                turno.Estado = EstadoTurno.Aceptado;
            }
            else
            {
                turno.Estado = EstadoTurno.Pendiente;
            }
            
            // Nuevos campos para familia y facturación 
            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("No se pudo identificar el usuario actual.");
            
            turno.CreatedByUserId = userId;  // Registrar quién creó el turno
            turno.CreatedAt = DateTime.UtcNow;  // Timestamp de creación
            turno.ObraSocialId = pacienteExiste.ObraSocialId;  // Heredar OS del paciente para facturación
            turno.NotasFacturacion = dto.NotasFacturacion;  // Incluir notas de facturación
            
            var createdTurno = await _turnoRepository.AddAsync(turno);
            return _mapper.Map<TurnoReadDto>(createdTurno);
        }

        public async Task<TurnoReadDto?> UpdateAsync(int id, TurnoUpdateDto dto)
        {
            var turno = await _turnoRepository.GetByIdAsync(id);
            if (turno == null)
                return null;

            var userRole = _currentUserService.GetUserRole();
            var userEmail = _currentUserService.GetUserEmail();
            var userId = _currentUserService.GetUserId();

            // Admin puede editar cualquier campo de cualquier turno
            if (_currentUserService.IsAdmin())
            {
                if (dto.DoctorId.HasValue && dto.DoctorId != turno.DoctorId)
                {
                    var doctorExiste = await _doctorRepository.GetByIdAsync(dto.DoctorId.Value) 
                        ?? throw new InvalidOperationException($"El doctor con ID {dto.DoctorId} no existe");
                }

                var updatedTurno = _mapper.Map(dto, turno);
                await _turnoRepository.UpdateAsync(updatedTurno);
                return _mapper.Map<TurnoReadDto>(updatedTurno);
            }

            // Doctor solo puede cambiar el Estado del turno (solo si es su turno)
            if (userRole == "Doctor")
            {
                var doctor = await _doctorRepository.GetByIdAsync(turno.DoctorId);
                if (doctor?.UserId != userId)
                    throw new UnauthorizedAccessException("No tienes permisos para modificar este turno.");

                // Doctor solo puede cambiar Estado
                if (!string.IsNullOrEmpty(dto.Estado))
                {
                    turno.Estado = dto.Estado;
                }
                else
                {
                    throw new InvalidOperationException("Los doctores solo pueden cambiar el estado del turno.");
                }

                // Validar que no intente cambiar otros campos
                if (dto.DoctorId.HasValue || !string.IsNullOrEmpty(dto.Especialidad) || dto.FechaHora.HasValue || !string.IsNullOrEmpty(dto.Motivo))
                {
                    throw new UnauthorizedAccessException("Los doctores solo pueden cambiar el estado del turno.");
                }

                await _turnoRepository.UpdateAsync(turno);
                return _mapper.Map<TurnoReadDto>(turno);
            }

            // Paciente puede editar su turno pero NO el Estado
            if (userRole == "Paciente")
            {
                var paciente = await _pacienteRepository.GetByIdAsync(turno.PacienteId)
                    ?? throw new InvalidOperationException("El paciente del turno no existe");

                // Es su propio turno
                bool essuPropio = paciente.UserId == userId;
                
                // Es responsable del paciente 
                bool esResponsable = paciente.ResponsableId == userId;

                if (!essuPropio && !esResponsable)
                {
                    throw new UnauthorizedAccessException(
                        "No tienes permisos para modificar este turno. " +
                        "Solo puedes editar tus propios turnos o los de tus dependientes.");
                }

                // Paciente no puede cambiar Estado
                if (!string.IsNullOrEmpty(dto.Estado))
                {
                    throw new UnauthorizedAccessException("No tienes permisos para cambiar el estado del turno.");
                }

                // Si intenta cambiar Doctor, validar que turno sea Pendiente y que haya disponibilidad
                if (dto.DoctorId.HasValue && dto.DoctorId != turno.DoctorId)
                {
                    if (turno.Estado != EstadoTurno.Pendiente)
                        throw new InvalidOperationException("Solo puedes cambiar de doctor si el turno está pendiente.");

                    var newDoctor = await _doctorRepository.GetByIdAsync(dto.DoctorId.Value)
                        ?? throw new InvalidOperationException($"El doctor con ID {dto.DoctorId} no existe");

                    var currentDoctor = await _doctorRepository.GetByIdAsync(turno.DoctorId);
                    var especialidad = dto.Especialidad ?? currentDoctor?.Especialidad ?? string.Empty;

                    // Validar que el nuevo doctor tenga la especialidad solicitada
                    if (!newDoctor.Especialidad.Equals(especialidad, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException($"El doctor no es especialista en {especialidad}");

                    // Verificar disponibilidad horaria con el nuevo doctor
                    var fechaHora = dto.FechaHora ?? turno.FechaHora;
                    var turnosConflicto = await _turnoRepository.FindAsync(t => 
                        t.DoctorId == dto.DoctorId && 
                        t.FechaHora == fechaHora &&
                        t.Id != id); // Excluir el turno actual
                    
                    if (turnosConflicto.Any())
                        throw new InvalidOperationException(
                            $"El doctor no tiene disponibilidad en {fechaHora:dd/MM/yyyy HH:mm}.");
                }

                // Paciente puede cambiar Especialidad, FechaHora, Motivo, Doctor (con validaciones arriba)
                var updatedTurno = _mapper.Map(dto, turno);
                await _turnoRepository.UpdateAsync(updatedTurno);
                return _mapper.Map<TurnoReadDto>(updatedTurno);
            }

            throw new UnauthorizedAccessException("No tienes permisos para modificar turnos.");
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var turno = await _turnoRepository.GetByIdAsync(id);
            if (turno == null)
                return false;

            var userRole = _currentUserService.GetUserRole();
            var userEmail = _currentUserService.GetUserEmail();
            var userId = _currentUserService.GetUserId();

            // Admin puede eliminar cualquier turno
            if (_currentUserService.IsAdmin())
                return await _turnoRepository.DeleteAsync(id);

            // Doctor puede eliminar turnos (solo si son suyos)
            if (userRole == "Doctor")
            {
                var doctor = await _doctorRepository.GetByIdAsync(turno.DoctorId);
                if (doctor?.UserId != userId)
                    throw new UnauthorizedAccessException("No tienes permisos para eliminar este turno.");

                return await _turnoRepository.DeleteAsync(id);
            }

            // Paciente puede eliminar solo sus propios turnos o los de sus dependientes
            if (userRole == "Paciente")
            {
                var paciente = await _pacienteRepository.GetByIdAsync(turno.PacienteId)
                    ?? throw new InvalidOperationException("El paciente del turno no existe");

                // Es su propio turno
                if (paciente.UserId == userId)
                    return await _turnoRepository.DeleteAsync(id);

                // Es responsable del paciente 
                if (paciente.ResponsableId == userId)
                    return await _turnoRepository.DeleteAsync(id);

                // Sin permisos
                throw new UnauthorizedAccessException(
                    "No tienes permisos para eliminar este turno. " +
                    "Solo puedes eliminar tus propios turnos o los de tus dependientes.");
            }

            throw new UnauthorizedAccessException("No tienes permisos para eliminar turnos.");
        }

        public async Task<bool> ExistAsync(int id)
        {
            var turno = await _turnoRepository.GetByIdAsync(id);
            return turno != null;
        }

        // Doctor valida cobertura en sistema externo y actualiza estado del turno
        public async Task<TurnoReadDto?> ValidarCoberturaAsync(int turnoId, TurnoValidarCoberturaDto dto)
        {
            var turno = await _turnoRepository.GetByIdAsync(turnoId);
            if (turno == null)
                return null;

            // Solo doctor puede validar (y debe ser su turno)
            var userRole = _currentUserService.GetUserRole();
            if (userRole != "Doctor")
                throw new UnauthorizedAccessException("Solo los doctores pueden validar coberturas.");

            var userEmail = _currentUserService.GetUserEmail();
            var userId = _currentUserService.GetUserId();
            var doctor = await _doctorRepository.GetByIdAsync(turno.DoctorId);

            if (doctor?.UserId != userId)
                throw new UnauthorizedAccessException("Solo puedes validar coberturas de tus propios turnos.");

            // Validar que el turno esté pendiente de validación
            if (turno.Estado != EstadoTurno.PendienteValidacionDoctor)
            {
                throw new InvalidOperationException(
                    $"Este turno no requiere validación (Estado actual: {turno.Estado}). " +
                    $"Solo turnos en estado '{EstadoTurno.PendienteValidacionDoctor}' pueden ser validados.");
            }

            // Procesar la validación
            if (string.Equals(dto.Resultado, EstadoTurno.Aceptado, StringComparison.OrdinalIgnoreCase))
            {
                turno.Estado = EstadoTurno.Aceptado;
                turno.MotivoRechazo = null;  // Limpiar motivo si estaba previo
            }
            else if (string.Equals(dto.Resultado, EstadoTurno.Rechazado, StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(dto.MotivoRechazo))
                    throw new InvalidOperationException("Debe proporcionar un motivo de rechazo.");

                turno.Estado = EstadoTurno.Rechazado;
                turno.MotivoRechazo = dto.MotivoRechazo;
            }
            else
            {
                throw new InvalidOperationException($"Resultado debe ser '{EstadoTurno.Aceptado}' o '{EstadoTurno.Rechazado}'");
            }

            // Registrar validación
            turno.FechaValidacion = DateTime.UtcNow;
            turno.ValidadoPorDoctorId = userId;

            await _turnoRepository.UpdateAsync(turno);
            return _mapper.Map<TurnoReadDto>(turno);
        }
    }
}
