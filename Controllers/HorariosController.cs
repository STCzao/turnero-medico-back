using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using turnero_medico_backend.DTOs.HorarioDTOs;
using turnero_medico_backend.Services.Interfaces;

namespace turnero_medico_backend.Controllers
{
    [ApiVersion("1.0")]
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class HorariosController(IHorarioService _service) : ControllerBase
    {
        // Consultar horarios de un doctor — accesible por todos los roles autenticados
        [HttpGet("doctor/{doctorId}")]
        public async Task<ActionResult<IEnumerable<HorarioReadDto>>> GetByDoctor(int doctorId)
        {
            var horarios = await _service.GetByDoctorAsync(doctorId);
            return Ok(horarios);
        }

        // Configurar horario — solo Admin
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<HorarioReadDto>> Create(HorarioCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var horario = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetByDoctor), new { doctorId = horario.DoctorId }, horario);
        }

        // Eliminar horario — solo Admin
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Delete(int id)
        {
            var result = await _service.DeleteAsync(id);
            if (!result)
                return NotFound(new { mensaje = $"Horario con ID {id} no encontrado" });

            return NoContent();
        }

        // Slots disponibles para un doctor en una fecha. Usado por secretaria al confirmar.
        [HttpGet("doctor/{doctorId}/disponibilidad")]
        [Authorize(Roles = "Admin,Secretaria,Paciente")]
        public async Task<ActionResult<IEnumerable<SlotDisponibleDto>>> GetDisponibilidad(
            int doctorId,
            [FromQuery] DateTime fecha)
        {
            if (fecha.Date < DateTime.UtcNow.Date)
                return BadRequest(new { mensaje = "La fecha no puede ser en el pasado" });

            var slots = await _service.GetDisponibilidadAsync(doctorId, fecha);
            return Ok(slots);
        }
    }
}
