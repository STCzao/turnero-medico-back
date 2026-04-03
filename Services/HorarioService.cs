using Microsoft.EntityFrameworkCore;
using turnero_medico_backend.Data;
using turnero_medico_backend.DTOs.HorarioDTOs;
using turnero_medico_backend.Models.Entities;
using turnero_medico_backend.Repositories.Interfaces;
using turnero_medico_backend.Services.Interfaces;

namespace turnero_medico_backend.Services
{
    public class HorarioService(
        ApplicationDbContext dbContext,
        IRepository<Doctor> doctorRepository,
        IAuditService auditService) : IHorarioService
    {
        private readonly ApplicationDbContext _dbContext = dbContext;
        private readonly IRepository<Doctor> _doctorRepository = doctorRepository;
        private readonly IAuditService _auditService = auditService;

        private static readonly string[] DiasNombre =
            ["Domingo", "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado"];

        public async Task<IEnumerable<HorarioReadDto>> GetByDoctorAsync(int doctorId)
        {
            var horarios = await _dbContext.Horarios
                .Include(h => h.Doctor)
                .Where(h => h.DoctorId == doctorId)
                .OrderBy(h => h.DiaSemana)
                .ThenBy(h => h.HoraInicio)
                .ToListAsync();

            return horarios.Select(h => new HorarioReadDto
            {
                Id = h.Id,
                DoctorId = h.DoctorId,
                DoctorNombre = h.Doctor != null ? $"{h.Doctor.Nombre} {h.Doctor.Apellido}" : "Sin asignar",
                DiaSemana = h.DiaSemana,
                DiaSemanaTexto = DiasNombre[h.DiaSemana],
                HoraInicio = h.HoraInicio,
                HoraFin = h.HoraFin,
                DuracionMinutos = h.DuracionMinutos
            });
        }

        public async Task<HorarioReadDto> CreateAsync(HorarioCreateDto dto)
        {
            var doctor = await _doctorRepository.GetByIdAsync(dto.DoctorId)
                ?? throw new InvalidOperationException($"El doctor con ID {dto.DoctorId} no existe.");

            if (dto.HoraFin <= dto.HoraInicio)
                throw new InvalidOperationException("La hora de fin debe ser posterior a la hora de inicio.");

            // Verificar superposición con horarios existentes del mismo doctor y día
            var superpuesto = await _dbContext.Horarios.AnyAsync(h =>
                h.DoctorId == dto.DoctorId &&
                h.DiaSemana == dto.DiaSemana &&
                h.HoraInicio < dto.HoraFin &&
                h.HoraFin > dto.HoraInicio);

            if (superpuesto)
                throw new InvalidOperationException(
                    $"El doctor ya tiene un horario que se superpone en {DiasNombre[dto.DiaSemana]} entre {dto.HoraInicio} y {dto.HoraFin}.");

            var horario = new Horario
            {
                DoctorId = dto.DoctorId,
                DiaSemana = dto.DiaSemana,
                HoraInicio = dto.HoraInicio,
                HoraFin = dto.HoraFin,
                DuracionMinutos = dto.DuracionMinutos
            };

            _dbContext.Horarios.Add(horario);
            await _dbContext.SaveChangesAsync();
            await _auditService.LogAsync(AuditAccion.Crear, "Horario", horario.Id.ToString());

            return new HorarioReadDto
            {
                Id = horario.Id,
                DoctorId = horario.DoctorId,
                DoctorNombre = $"{doctor.Nombre} {doctor.Apellido}",
                DiaSemana = horario.DiaSemana,
                DiaSemanaTexto = DiasNombre[horario.DiaSemana],
                HoraInicio = horario.HoraInicio,
                HoraFin = horario.HoraFin,
                DuracionMinutos = horario.DuracionMinutos
            };
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var horario = await _dbContext.Horarios.FindAsync(id)
                ?? throw new KeyNotFoundException($"Horario con ID {id} no encontrado.");

            //Verificar que no haya turnos futuros confirmados que dependan de este horario
            var tieneTurnosFuturos = await _dbContext.Turnos.AnyAsync(t =>
            t.DoctorId == horario.DoctorId &&
            t.Estado == EstadoTurno.Confirmado &&
            t.FechaHora.HasValue &&
            t.FechaHora.Value >= DateTime.UtcNow
            );

            if (tieneTurnosFuturos)
                throw new InvalidOperationException(
                    "No se puede eliminar el horario porque el doctor tiene turnos futuros confirmados.");

            _dbContext.Horarios.Remove(horario);
            await _dbContext.SaveChangesAsync();
            await _auditService.LogAsync(AuditAccion.Eliminar, "Horario", id.ToString());
            return true;
        }

        // Calcula slots disponibles para un doctor en una fecha específica.
        // Compara los horarios configurados del doctor contra los turnos ya confirmados.
        public async Task<IEnumerable<SlotDisponibleDto>> GetDisponibilidadAsync(int doctorId, DateTime fecha)
        {
            var doctor = await _doctorRepository.GetByIdAsync(doctorId)
                ?? throw new InvalidOperationException($"El doctor con ID {doctorId} no existe.");

            var diaSemana = (int)fecha.DayOfWeek;

            // Obtener horarios del doctor para ese día de la semana
            var horarios = await _dbContext.Horarios
                .Where(h => h.DoctorId == doctorId && h.DiaSemana == diaSemana)
                .OrderBy(h => h.HoraInicio)
                .ToListAsync();

            if (!horarios.Any())
                return [];

            // Usar fecha UTC para que la comparación contra timestamp with time zone sea correcta
            var fechaBase = DateTime.SpecifyKind(fecha.Date, DateTimeKind.Utc);
            var fechaFin = fechaBase.AddDays(1);
            var turnosOcupados = (await _dbContext.Turnos
                .Where(t =>
                    t.DoctorId == doctorId &&
                    t.FechaHora >= fechaBase &&
                    t.FechaHora < fechaFin &&
                    t.Estado == EstadoTurno.Confirmado)
                .Select(t => t.FechaHora)
                .ToListAsync())
                .Where(t => t.HasValue)
                .Select(t => t!.Value)
                .ToHashSet();

            var doctorNombre = $"{doctor.Nombre} {doctor.Apellido}";
            var slots = new List<SlotDisponibleDto>();

            foreach (var horario in horarios)
            {
                var slot = horario.HoraInicio;
                while (slot.AddMinutes(horario.DuracionMinutos) <= horario.HoraFin)
                {
                    var fechaHoraSlot = DateTime.SpecifyKind(fechaBase.Add(slot.ToTimeSpan()), DateTimeKind.Utc);

                    // Muestra todos los slots no ocupados del día.
                    // El filtro de "no pasado" se delega al frontend (min=today en el datepicker)
                    // para evitar problemas de timezone en producción (servidor UTC vs. cliente ART).
                    if (!turnosOcupados.Contains(fechaHoraSlot))
                    {
                        slots.Add(new SlotDisponibleDto
                        {
                            FechaHora = fechaHoraSlot,
                            DoctorId = doctorId,
                            DoctorNombre = doctorNombre,
                            DuracionMinutos = horario.DuracionMinutos
                        });
                    }
                    slot = slot.AddMinutes(horario.DuracionMinutos);
                }
            }

            return slots;
        }
    }
}
