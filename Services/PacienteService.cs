using AutoMapper;
using Microsoft.EntityFrameworkCore;
using turnero_medico_backend.Data;
using turnero_medico_backend.DTOs.Common;
using turnero_medico_backend.DTOs.PacienteDTOs;
using turnero_medico_backend.Models.Entities;
using turnero_medico_backend.Repositories.Interfaces;
using turnero_medico_backend.Services.Interfaces;
using static turnero_medico_backend.DTOs.PacienteDTOs.PacienteExportDto;

namespace turnero_medico_backend.Services
{
    public class PacienteService(
        IPacienteRepository pacienteRepository,
        ApplicationDbContext dbContext,
        IMapper mapper,
        ICurrentUserService currentUserService,
        IAuditService auditService
    ) : IPacienteService
    {
        private readonly IPacienteRepository _pacienteRepository = pacienteRepository;
        private readonly ApplicationDbContext _dbContext = dbContext;
        private readonly IMapper _mapper = mapper;
        private readonly ICurrentUserService _currentUserService = currentUserService;
        private readonly IAuditService _auditService = auditService;
        public async Task<PagedResultDto<PacienteReadDto>> GetAllPagedAsync(int page, int pageSize)
        {
            pageSize = Math.Clamp(pageSize, 1, 100);
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
            var userRole = _currentUserService.GetUserRole();
            var userId   = _currentUserService.GetUserId();

            var paciente = await _pacienteRepository.GetByIdWithObraSocialAsync(id);
            if (paciente == null)
                return null;

            // Admin y Secretaria pueden ver cualquier paciente.
            if (userRole == "Admin" || userRole == "Secretaria")
                return _mapper.Map<PacienteReadDto>(paciente);

            // Un doctor solo puede ver pacientes con los que tiene (o tuvo) algún turno asignado
            if (userRole == "Doctor")
            {
                var doctorId = await _dbContext.Doctores
                    .Where(d => d.UserId == userId)
                    .Select(d => (int?)d.Id)
                    .FirstOrDefaultAsync();

                if (doctorId == null)
                    throw new UnauthorizedAccessException("No tienes permisos para ver este paciente.");

                var tieneTurno = await _dbContext.Turnos
                    .AnyAsync(t => t.DoctorId == doctorId && t.PacienteId == id);

                if (!tieneTurno)
                    throw new UnauthorizedAccessException("No tienes permisos para ver este paciente.");

                return _mapper.Map<PacienteReadDto>(paciente);
            }

            // Pacientes solo pueden ver su propio perfil o el de sus dependientes.
            if (userRole == "Paciente")
            {
                if (paciente.UserId != userId && paciente.ResponsableId != userId)
                    throw new UnauthorizedAccessException("No tienes permisos para ver este paciente.");
            }

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
            await _auditService.LogAsync(AuditAccion.Crear, "Paciente", createdPaciente.Id.ToString());
            return _mapper.Map<PacienteReadDto>(createdPaciente);
        }

        public async Task<PacienteReadDto> UpdateAsync(int id, PacienteUpdateDto dto)
        {
            var paciente = await _pacienteRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Paciente con ID {id} no encontrado.");

            _mapper.Map(dto, paciente);

            //Recalcular EsMayorDeEdad en base a FechaNacimiento actualizada
            var hoy = DateTime.UtcNow;
            var edad = hoy.Year - paciente.FechaNacimiento.Year;
            if (paciente.FechaNacimiento > hoy.AddYears(-edad)) edad--;
            paciente.EsMayorDeEdad = edad >= 18;

            await _pacienteRepository.UpdateAsync(paciente);
            await _auditService.LogAsync(AuditAccion.Actualizar, "Paciente", id.ToString());
            return _mapper.Map<PacienteReadDto>(paciente);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            if (!await _pacienteRepository.ExistAsync(id))
                throw new KeyNotFoundException($"Paciente con ID {id} no encontrado.");

            var tieneTurnos = await _dbContext.Turnos.AnyAsync(t => t.PacienteId == id);
            if (tieneTurnos)
                throw new InvalidOperationException(
                    "No se puede eliminar el paciente porque tiene turnos asociados. Cancele los turnos primero.");

            var deleted = await _pacienteRepository.DeleteAsync(id);
            if (deleted)
                await _auditService.LogAsync(AuditAccion.Eliminar, "Paciente", id.ToString());
            return deleted;
        }

        public async Task<bool> ExistAsync(int id)
            => await _pacienteRepository.ExistAsync(id);

        // ─────────────────────────────────────────────────────────────
        // DEPENDIENTES
        // ─────────────────────────────────────────────────────────────

        public async Task<PagedResultDto<PacienteReadDto>> GetMisDependientesAsync(int page, int pageSize)
        {
            pageSize = Math.Clamp(pageSize, 1, 100);
            var userId = _currentUserService.GetUserId();
            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("No se pudo obtener el ID del usuario autenticado.");

            var all = await _pacienteRepository.FindWithObraSocialAsync(p => p.ResponsableId == userId);
            var allList = all.ToList();
            var items = allList.Skip((page - 1) * pageSize).Take(pageSize);
            return new PagedResultDto<PacienteReadDto>
            {
                Items = _mapper.Map<IEnumerable<PacienteReadDto>>(items),
                Total = allList.Count,
                Page = page,
                PageSize = pageSize
            };
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

            // Evitar dependencia circular: un paciente que es dependiente de otro no puede tener sus propios dependientes
            var esDependiente = await _pacienteRepository.FindAsync(p => p.UserId == userId && p.ResponsableId != null);
            if (esDependiente.Any())
                throw new InvalidOperationException(
                    "Los pacientes dependientes no pueden registrar sus propios dependientes.");

            if (dto.ObraSocialId.HasValue)
            {
                var osExiste = await _dbContext.ObrasSociales.AnyAsync(o => o.Id == dto.ObraSocialId.Value);
                if (!osExiste)
                    throw new KeyNotFoundException($"La obra social con ID {dto.ObraSocialId} no existe.");
            }

            var dependiente = new Paciente
            {
                Dni = dto.Dni,
                Nombre = dto.Nombre,
                Apellido = dto.Apellido,
                FechaNacimiento = dto.FechaNacimiento,
                Telefono = dto.Telefono ?? string.Empty,
                ResponsableId = userId,
                EsMayorDeEdad = false,
                UserId = null, // Los dependientes no tienen cuenta de usuario
                TipoPago = dto.TipoPago,
                ObraSocialId = dto.ObraSocialId,
                NumeroAfiliado = dto.NumeroAfiliado,
                PlanAfiliado = dto.PlanAfiliado
            };

            var creado = await _pacienteRepository.AddAsync(dependiente);
            return _mapper.Map<PacienteReadDto>(creado);
        }

        public async Task<PacienteReadDto> UpdateDependienteAsync(int id, DependienteUpdateDto dto)
        {
            var userId = _currentUserService.GetUserId();
            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("No se pudo obtener el ID del usuario autenticado.");

            var dependiente = await _pacienteRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Dependiente con ID {id} no encontrado.");

            if (dependiente.ResponsableId != userId)
                throw new UnauthorizedAccessException("No tienes permisos para modificar este dependiente.");

            dependiente.Nombre          = dto.Nombre;
            dependiente.Apellido        = dto.Apellido;
            dependiente.FechaNacimiento = dto.FechaNacimiento;
            dependiente.Telefono        = dto.Telefono ?? string.Empty;
            dependiente.TipoPago        = dto.TipoPago;
            dependiente.ObraSocialId    = dto.ObraSocialId;
            dependiente.NumeroAfiliado  = dto.NumeroAfiliado;
            dependiente.PlanAfiliado    = dto.PlanAfiliado;

            // Recalcular mayoría de edad por si cambió la fecha
            var hoy = DateTime.UtcNow;
            var edad = hoy.Year - dependiente.FechaNacimiento.Year;
            if (dependiente.FechaNacimiento > hoy.AddYears(-edad)) edad--;
            dependiente.EsMayorDeEdad = edad >= 18;

            await _pacienteRepository.UpdateAsync(dependiente);
            await _auditService.LogAsync(AuditAccion.Actualizar, "Dependiente", id.ToString());
            return _mapper.Map<PacienteReadDto>(dependiente);
        }

        public async Task<bool> DeleteDependienteAsync(int id)
        {
            var userId = _currentUserService.GetUserId();
            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("No se pudo obtener el ID del usuario autenticado.");

            var dependiente = await _pacienteRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Dependiente con ID {id} no encontrado.");

            var esAdmin = _currentUserService.GetUserRole() == "Admin";
            if (!esAdmin && dependiente.ResponsableId != userId)
                throw new UnauthorizedAccessException("No tienes permisos para eliminar este dependiente.");

            var tieneTurnos = await _dbContext.Turnos.AnyAsync(t => t.PacienteId == id);
            if (tieneTurnos)
                throw new InvalidOperationException(
                    "No se puede eliminar el dependiente porque tiene turnos asociados. Cancele los turnos primero.");

            var deleted = await _pacienteRepository.DeleteAsync(id);
            if (deleted)
                await _auditService.LogAsync(AuditAccion.Eliminar, "Dependiente", id.ToString());
            return deleted;
        }

        // ─────────────────────────────────────────────────────────────
        // GDPR: exportar todos los datos del paciente autenticado
        // ─────────────────────────────────────────────────────────────

        public async Task<PacienteExportDto?> ExportarMisDatosAsync()
        {
            var userId = _currentUserService.GetUserId();
            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("No se pudo obtener el ID del usuario autenticado.");

            var pacientes = await _pacienteRepository.FindWithObraSocialAsync(p => p.UserId == userId);
            var paciente = pacientes.FirstOrDefault();
            if (paciente == null)
                return null;

            var turnos = await _dbContext.Turnos
                .Include(t => t.Especialidad)
                .Include(t => t.Doctor)
                .Where(t => t.PacienteId == paciente.Id)
                .OrderByDescending(t => t.CreatedAt)
                .AsNoTracking()
                .ToListAsync();

            await _auditService.LogAsync(AuditAccion.ExportarDatos, "Paciente", paciente.Id.ToString());

            var tipoPagoLabel = paciente.TipoPago switch
            {
                TipoPago.ObraSocial => "ObraSocial",
                _                   => "Particular"
            };

            return new PacienteExportDto
            {
                FechaExportacion = DateTime.UtcNow,
                Id               = paciente.Id,
                Dni              = paciente.Dni,
                Nombre           = paciente.Nombre,
                Apellido         = paciente.Apellido,
                Email            = paciente.Email ?? string.Empty,
                Telefono         = paciente.Telefono,
                FechaNacimiento  = paciente.FechaNacimiento,
                EsMayorDeEdad    = paciente.EsMayorDeEdad,
                TipoPago         = tipoPagoLabel,
                ObraSocialNombre = paciente.ObraSocial?.Nombre,
                NumeroAfiliado   = paciente.NumeroAfiliado,
                PlanAfiliado     = paciente.PlanAfiliado,
                Turnos = turnos.Select(t => new TurnoExportItem
                {
                    Id                 = t.Id,
                    FechaHora          = t.FechaHora,
                    Motivo             = t.Motivo,
                    Estado             = t.Estado,
                    EspecialidadNombre = t.Especialidad?.Nombre ?? string.Empty,
                    DoctorNombre       = t.Doctor != null
                        ? $"{t.Doctor.Nombre} {t.Doctor.Apellido}"
                        : string.Empty,
                    CreadoEn           = t.CreatedAt,
                    ObservacionClinica = t.ObservacionClinica
                })
            };
        }
    }
}
