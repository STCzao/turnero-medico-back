using turnero_medico_backend.DTOs.HorarioDTOs;

namespace turnero_medico_backend.Services.Interfaces
{
    // Gestión de horarios de atención de doctores y cálculo de disponibilidad.
    // Un horario define la franja semanal del doctor (día + hora inicio/fin + duración de turno).
    // GetDisponibilidadAsync cruza los horarios contra los turnos confirmados de esa fecha
    // para devolver solo los slots libres.
    public interface IHorarioService
    {
        // Devuelve todos los horarios configurados para un doctor, ordenados por día y hora.
        Task<IEnumerable<HorarioReadDto>> GetByDoctorAsync(int doctorId);

        // Crea un bloque horario para un doctor. Valida superposición con horarios existentes.
        Task<HorarioReadDto> CreateAsync(HorarioCreateDto dto);

        // Elimina un horario. Bloquea si el doctor tiene turnos futuros confirmados en ese bloque.
        Task<bool> DeleteAsync(int id);

        // Calcula los slots disponibles (fecha + hora) para una fecha concreta.
        // Retorna solo slots futuros y sin turno confirmado asignado.
        Task<IEnumerable<SlotDisponibleDto>> GetDisponibilidadAsync(int doctorId, DateTime fecha);
    }
}
