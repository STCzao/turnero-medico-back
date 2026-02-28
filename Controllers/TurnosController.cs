using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using turnero_medico_backend.DTOs.Common;
using turnero_medico_backend.DTOs.TurnoDTOs;
using turnero_medico_backend.Services.Interfaces;

namespace turnero_medico_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TurnosController(ITurnoService _service) : ControllerBase
    {

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<PagedResultDto<TurnoReadDto>>> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var turnos = await _service.GetAllPagedAsync(page, pageSize);
            return Ok(turnos);
        }


        [HttpGet("paciente/{pacienteId}")]
        public async Task<ActionResult<IEnumerable<TurnoReadDto>>> GetByPaciente(int pacienteId)
        {
            var turnos = await _service.GetByPacienteAsync(pacienteId);
            if (!turnos.Any())
                return NotFound(new { mensaje = $"El paciente {pacienteId} no tiene turnos registrados" });

            return Ok(turnos);
        }

        [HttpGet("doctor/{doctorId}")]
        public async Task<ActionResult<IEnumerable<TurnoReadDto>>> GetByDoctor(int doctorId)
        {
            var turnos = await _service.GetByDoctorAsync(doctorId);
            if (!turnos.Any())
                return NotFound(new { mensaje = $"El doctor {doctorId} no tiene turnos registrados" });

            return Ok(turnos);
        }


        [HttpGet("{id}")]
        public async Task<ActionResult<TurnoReadDto>> GetById(int id)
        {
            var turno = await _service.GetByIdAsync(id);
            if (turno == null)
                return NotFound(new { mensaje = $"Turno con ID {id} no encontrado" });

            return Ok(turno);
        }


        [HttpPost]
        public async Task<ActionResult<TurnoReadDto>> Create(TurnoCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var turno = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = turno.Id }, turno);
        }


        [HttpPatch("{id}")]
        public async Task<ActionResult<TurnoReadDto>> Update(int id, TurnoUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (id != dto.Id)
                return BadRequest(new { mensaje = "El ID de la URL no coincide con el ID del DTO" });

            var turno = await _service.UpdateAsync(id, dto);
            if (turno == null)
                return NotFound(new { mensaje = $"Turno con ID {id} no encontrado" });

            return Ok(turno);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            var result = await _service.DeleteAsync(id);
            if (!result)
                return NotFound(new { mensaje = $"Turno con ID {id} no encontrado" });

            return NoContent();
        }
    }
}
