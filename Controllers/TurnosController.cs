using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using turnero_medico_backend.DTOs.Common;
using turnero_medico_backend.DTOs.TurnoDTOs;
using turnero_medico_backend.Models.Entities;
using turnero_medico_backend.Services.Interfaces;

namespace turnero_medico_backend.Controllers
{
    [ApiVersion("1.0")]
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TurnosController(ITurnoService _service) : ControllerBase
    {

        [HttpGet]
        [Authorize(Roles = "Admin,Secretaria")]
        public async Task<ActionResult<PagedResultDto<TurnoReadDto>>> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? estado = null)
        {
            if (!ValidarEstado(estado, out var errorResult))
                return errorResult!;

            var turnos = await _service.GetAllPagedAsync(page, pageSize, estado);
            return Ok(turnos);
        }

        [HttpGet("paciente/{pacienteId}")]
        [Authorize(Roles = "Admin,Secretaria")]
        public async Task<ActionResult<IEnumerable<TurnoReadDto>>> GetByPaciente(
            int pacienteId,
            [FromQuery] string? estado = null)
        {
            if (!ValidarEstado(estado, out var errorResult))
                return errorResult!;

            var turnos = await _service.GetByPacienteAsync(pacienteId, estado);
            return Ok(turnos);
        }

        [HttpGet("doctor/{doctorId}")]
        [Authorize(Roles = "Admin,Secretaria")]
        public async Task<ActionResult<IEnumerable<TurnoReadDto>>> GetByDoctor(
            int doctorId,
            [FromQuery] string? estado = null)
        {
            if (!ValidarEstado(estado, out var errorResult))
                return errorResult!;

            var turnos = await _service.GetByDoctorAsync(doctorId, estado);
            return Ok(turnos);
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Secretaria")]
        public async Task<ActionResult<TurnoReadDto>> GetById(int id)
        {
            var turno = await _service.GetByIdAsync(id);
            if (turno == null)
                return NotFound(new { mensaje = $"Turno con ID {id} no encontrado" });

            return Ok(turno);
        }

        // Paciente / Secretaria / Admin crean una solicitud (sin fecha)
        [HttpPost]
        [Authorize(Roles = "Paciente,Secretaria,Admin")]
        public async Task<ActionResult<TurnoReadDto>> Create(TurnoCreateDto dto)
        {

            var turno = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = turno.Id }, turno);
        }

        // Secretaria / Admin: asignan fecha, doctor y confirman la solicitud
        [HttpPost("{id}/confirmar")]
        [Authorize(Roles = "Secretaria,Admin")]
        public async Task<ActionResult<TurnoReadDto>> Confirmar(int id, ConfirmarTurnoDto dto)
        {

            var turno = await _service.ConfirmarAsync(id, dto);
            return Ok(turno);
        }

        // Secretaria / Admin: rechazan la solicitud con motivo obligatorio
        [HttpPost("{id}/rechazar")]
        [Authorize(Roles = "Secretaria,Admin")]
        public async Task<ActionResult<TurnoReadDto>> Rechazar(int id, RechazarTurnoDto dto)
        {

            var turno = await _service.RechazarAsync(id, dto);
            return Ok(turno);
        }

        // Cancelacion: Paciente, Doctor, Secretaria o Admin con reglas propias
        [HttpPost("{id}/cancelar")]
        [Authorize(Roles = "Paciente,Doctor,Secretaria,Admin")]
        public async Task<ActionResult<TurnoReadDto>> Cancelar(int id, CancelarTurnoDto dto)
        {

            var turno = await _service.CancelarAsync(id, dto);
            return Ok(turno);
        }

        // Doctor: marca Completado/Ausente y agrega observacion clinica
        [HttpPatch("{id}")]
        [Authorize(Roles = "Doctor,Admin")]
        public async Task<ActionResult<TurnoReadDto>> Update(int id, TurnoUpdateDto dto)
        {

            if (id != dto.Id)
                return BadRequest(new { mensaje = "El ID de la URL no coincide con el ID del DTO" });

            var turno = await _service.UpdateAsync(id, dto);
            return Ok(turno);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Delete(int id)
        {
            await _service.DeleteAsync(id);
            return NoContent();
        }

        // Mis turnos — el paciente/doctor autenticado ve sus turnos sin conocer su ID numérico
        [HttpGet("me")]
        [Authorize(Roles = "Paciente,Doctor")]
        public async Task<ActionResult<IEnumerable<TurnoReadDto>>> GetMyTurnos([FromQuery] string? estado = null)
        {
            if (!ValidarEstado(estado, out var errorResult))
                return errorResult!;

            var turnos = await _service.GetMyTurnosAsync(estado);
            return Ok(turnos);
        }

        // Agenda del doctor autenticado para una fecha
        [HttpGet("doctor/me/agenda")]
        [Authorize(Roles = "Doctor")]
        public async Task<ActionResult<IEnumerable<TurnoReadDto>>> GetMyAgenda([FromQuery] DateTime fecha)
        {
            var turnos = await _service.GetMyAgendaAsync(fecha);
            return Ok(turnos);
        }

        // Turnos pendientes de gestión — Secretaria/Admin
        [HttpGet("pendientes")]
        [Authorize(Roles = "Secretaria,Admin")]
        public async Task<ActionResult<PagedResultDto<TurnoReadDto>>> GetPendientes(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var turnos = await _service.GetPendientesAsync(page, pageSize);
            return Ok(turnos);
        }

        // Historial clínico — turnos completados de un paciente
        [HttpGet("paciente/{pacienteId}/historial")]
        [Authorize(Roles = "Admin,Secretaria,Paciente,Doctor")]
        public async Task<ActionResult<IEnumerable<TurnoReadDto>>> GetHistorial(int pacienteId)
        {
            var turnos = await _service.GetHistorialAsync(pacienteId);
            return Ok(turnos);
        }

        // Helper: valida que el parámetro ?estado sea uno de los 6 valores válidos
        private static bool ValidarEstado(string? estado, out ActionResult? errorResult)
        {
            errorResult = null;
            if (estado == null) return true;

            if (!EstadoTurno.Todos.Contains(estado))
            {
                errorResult = new BadRequestObjectResult(new
                {
                    mensaje = $"Estado '{estado}' no es válido.",
                    estadosValidos = EstadoTurno.Todos
                });
                return false;
            }
            return true;
        }
    }
}
