using AutoMapper;
using Microsoft.AspNetCore.Identity;
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
        IAuditService auditService,
        UserManager<ApplicationUser> userManager
    ) : IPacienteService
    {
        private readonly IPacienteRepository _pacienteRepository = pacienteRepository;
        private readonly ApplicationDbContext _dbContext = dbContext;
        private readonly IMapper _mapper = mapper;
        private readonly ICurrentUserService _currentUserService = currentUserService;
        private readonly IAuditService _auditService = auditService;
        private readonly UserManager<ApplicationUser> _userManager = userManager;

        public async Task<PagedResultDto<PacienteReadDto>> GetAllPagedAsync(int page, int pageSize)
        {
            pageSize = Math.Clamp(pageSize, 1, 100);
            var (items, total) = await _pacienteRepository.GetAllPagedAsync(page, pageSize);
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
            var userId = _currentUserService.GetUserId();

            var paciente = await _pacienteRepository.GetByIdAsync(id);
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
                    .AnyAsync(t => t.DoctorId == doctorId && t.PacienteId == id &&
                        (t.Estado == EstadoTurno.Confirmado || t.Estado == EstadoTurno.Completado));

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

            var paciente = await _pacienteRepository.FindFirstAsync(p => p.UserId == userId);

            if (paciente == null)
                return null;

            return _mapper.Map<PacienteReadDto>(paciente);
        }

        public async Task<PacienteReadDto> CreateAsync(PacienteCreateDto dto)
        {
            // IgnoreQueryFilters para detectar también soft-deleted con el mismo DNI
            var existente = await _dbContext.Pacientes
                .IgnoreQueryFilters()
                .AnyAsync(p => p.Dni == dto.Dni.Trim());
            if (existente)
                throw new InvalidOperationException("Ya existe un paciente con ese DNI.");

            var paciente = _mapper.Map<Paciente>(dto);
            var createdPaciente = await _pacienteRepository.AddAsync(paciente);
            await _auditService.LogAsync(AuditAccion.Crear, "Paciente", createdPaciente.Id.ToString());
            return _mapper.Map<PacienteReadDto>(createdPaciente);
        }

        public async Task<PacienteReadDto> UpdateAsync(int id, PacienteUpdateDto dto)
        {
            var userRole = _currentUserService.GetUserRole();
            var userId = _currentUserService.GetUserId();

            var paciente = await _pacienteRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Paciente con ID {id} no encontrado.");

            var valoresAnteriores = AuditSnapshot.ToJson(new { paciente.Nombre, paciente.Apellido, paciente.Email, paciente.Telefono, paciente.FechaNacimiento });

            if (userRole == "Paciente" && paciente.UserId != userId)
                throw new UnauthorizedAccessException("No tienes permisos para modificar este paciente.");

            _mapper.Map(dto, paciente);
            paciente.FechaNacimiento = DateTime.SpecifyKind(paciente.FechaNacimiento, DateTimeKind.Utc);

            paciente.EsMayorDeEdad = EdadHelper.EsMayorDeEdad(paciente.FechaNacimiento);

            await _pacienteRepository.UpdateAsync(paciente);
            await _auditService.LogAsync(AuditAccion.Actualizar, "Paciente", id.ToString(),
                valoresAnteriores, AuditSnapshot.ToJson(new { paciente.Nombre, paciente.Apellido, paciente.Email, paciente.Telefono, paciente.FechaNacimiento }));
            return _mapper.Map<PacienteReadDto>(paciente);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var paciente = await _pacienteRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Paciente con ID {id} no encontrado.");

            var tieneTurnosActivos = await _dbContext.Turnos.AnyAsync(t =>
                t.PacienteId == id &&
                (t.Estado == EstadoTurno.Confirmado || t.Estado == EstadoTurno.SolicitudPendiente)
            );

            if (tieneTurnosActivos)
                throw new InvalidOperationException("No se puede eliminar un paciente con turnos activos.");

            await using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                paciente.IsDeleted = true;
                paciente.DeletedAt = DateTime.UtcNow;
                await _pacienteRepository.UpdateAsync(paciente);

                if (!string.IsNullOrEmpty(paciente.UserId))
                    await UserLockoutHelper.LockUserAsync(_userManager, paciente.UserId);

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            await _auditService.LogAsync(AuditAccion.Eliminar, "Paciente", id.ToString(),
                AuditSnapshot.ToJson(new { paciente.Nombre, paciente.Apellido, paciente.Dni, paciente.Email }));
            return true;
        }

        public async Task<PacienteReadDto> ReactivarAsync(int id)
        {
            var paciente = await _pacienteRepository.GetByIdUnscopedAsync(id)
                ?? throw new KeyNotFoundException($"Paciente con ID {id} no encontrado.");

            if (!paciente.IsDeleted)
                throw new InvalidOperationException("El paciente ya se encuentra activo.");

            await using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                paciente.IsDeleted = false;
                paciente.DeletedAt = null;
                await _pacienteRepository.UpdateAsync(paciente);

                if (!string.IsNullOrEmpty(paciente.UserId))
                    await UserLockoutHelper.UnlockUserAsync(_userManager, paciente.UserId);

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            await _auditService.LogAsync(AuditAccion.Actualizar, "Paciente", id.ToString());
            return _mapper.Map<PacienteReadDto>(paciente);
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

            var (items, total) = await _pacienteRepository.GetDependientesPagedAsync(userId, page, pageSize);
            return new PagedResultDto<PacienteReadDto>
            {
                Items = _mapper.Map<IEnumerable<PacienteReadDto>>(items),
                Total = total,
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

            var fechaNacimientoUtc = DateTime.SpecifyKind(dto.FechaNacimiento, DateTimeKind.Utc);

            var dependiente = new Paciente
            {
                Dni = dto.Dni,
                Nombre = dto.Nombre,
                Apellido = dto.Apellido,
                FechaNacimiento = fechaNacimientoUtc,
                Telefono = dto.Telefono ?? string.Empty,
                ResponsableId = userId,
                EsMayorDeEdad = EdadHelper.EsMayorDeEdad(fechaNacimientoUtc),
                UserId = null  // Los dependientes no tienen cuenta de usuario
            };

            var creado = await _pacienteRepository.AddAsync(dependiente);
            await _auditService.LogAsync(AuditAccion.Crear, "Dependiente", creado.Id.ToString());
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

            dependiente.Nombre = dto.Nombre;
            dependiente.Apellido = dto.Apellido;
            dependiente.FechaNacimiento = DateTime.SpecifyKind(dto.FechaNacimiento, DateTimeKind.Utc);
            dependiente.Telefono = dto.Telefono ?? string.Empty;

            dependiente.EsMayorDeEdad = EdadHelper.EsMayorDeEdad(dependiente.FechaNacimiento);

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

            var rol = _currentUserService.GetUserRole();
            if (rol != "Admin" && rol != "Secretaria" && dependiente.ResponsableId != userId)
                throw new UnauthorizedAccessException("No tienes permisos para eliminar este dependiente.");

            dependiente.IsDeleted = true;
            dependiente.DeletedAt = DateTime.UtcNow;

            await _pacienteRepository.UpdateAsync(dependiente);
            await _auditService.LogAsync(AuditAccion.Eliminar, "Dependiente", id.ToString());
            return true;
        }

        // ─────────────────────────────────────────────────────────────
        // GDPR: exportar todos los datos del paciente autenticado
        // ─────────────────────────────────────────────────────────────

        public async Task<PacienteExportDto?> ExportarMisDatosAsync()
        {
            var userId = _currentUserService.GetUserId();
            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("No se pudo obtener el ID del usuario autenticado.");

            var paciente = await _pacienteRepository.FindFirstAsync(p => p.UserId == userId);
            if (paciente == null)
                return null;

            var turnos = await _dbContext.Turnos
                .Include(t => t.Especialidad)
                .Include(t => t.Doctor)
                .Include(t => t.ObraSocial)
                .Where(t => t.PacienteId == paciente.Id)
                .OrderByDescending(t => t.CreatedAt)
                .AsNoTracking()
                .ToListAsync();

            await _auditService.LogAsync(AuditAccion.ExportarDatos, "Paciente", paciente.Id.ToString());

            return new PacienteExportDto
            {
                FechaExportacion = DateTime.UtcNow,
                Id = paciente.Id,
                Dni = paciente.Dni,
                Nombre = paciente.Nombre,
                Apellido = paciente.Apellido,
                Email = paciente.Email ?? string.Empty,
                Telefono = paciente.Telefono,
                FechaNacimiento = paciente.FechaNacimiento,
                EsMayorDeEdad = paciente.EsMayorDeEdad,
                Turnos = turnos.Select(t => new TurnoExportItem
                {
                    Id = t.Id,
                    FechaHora = t.FechaHora,
                    Motivo = t.Motivo,
                    Estado = t.Estado,
                    EspecialidadNombre = t.Especialidad?.Nombre ?? string.Empty,
                    DoctorNombre = t.Doctor != null
                        ? $"{t.Doctor.Nombre} {t.Doctor.Apellido}"
                        : string.Empty,
                    ObraSocialNombre = t.ObraSocial?.Nombre,
                    NumeroAfiliadoDeclarado = t.NumeroAfiliadoDeclarado,
                    PlanAfiliadoDeclarado = t.PlanAfiliadoDeclarado,
                    CreadoEn = t.CreatedAt,
                    ObservacionClinica = t.ObservacionClinica
                })
            };
        }
    }
}
