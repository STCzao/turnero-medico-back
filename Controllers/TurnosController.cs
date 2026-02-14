using Microsoft.AspNetCore.Mvc;
using turnero_medico_backend.DTOs.TurnoDTOs;
using turnero_medico_backend.Services;
using turnero_medico_backend.Services.Interfaces;

namespace turnero_medico_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TurnosController(TurnoService _service) : ControllerBase
    {

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TurnoReadDto>>> GetAll()
        {
            try
            {
                var turnos = await _service.GetAllAsync();
                return Ok(turnos);
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }


        [HttpGet("paciente/{pacienteId}")]
        public async Task<ActionResult<IEnumerable<TurnoReadDto>>> GetByPaciente(int pacienteId)
        {
            try
            {
                var turnos = await _service.GetByPacienteAsync(pacienteId);
                if (!turnos.Any())
                    return NotFound(new { mensaje = $"El paciente {pacienteId} no tiene turnos registrados" });

                return Ok(turnos);
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        [HttpGet("doctor/{doctorId}")]
        public async Task<ActionResult<IEnumerable<TurnoReadDto>>> GetByDoctor(int doctorId)
        {
            try
            {
                var turnos = await _service.GetByDoctorAsync(doctorId);
                if (!turnos.Any())
                    return NotFound(new { mensaje = $"El doctor {doctorId} no tiene turnos registrados" });

                return Ok(turnos);
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }


        [HttpGet("{id}")]
        public async Task<ActionResult<TurnoReadDto>> GetById(int id)
        {
            try
            {
                var turno = await _service.GetByIdAsync(id);
                if (turno == null)
                    return NotFound(new { mensaje = $"Turno con ID {id} no encontrado" });

                return Ok(turno);
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }


        [HttpPost]
        public async Task<ActionResult<TurnoReadDto>> Create(TurnoCreateDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var turno = await _service.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = turno.Id }, turno);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }


        [HttpPatch("{id}")]
        public async Task<ActionResult<TurnoReadDto>> Update(int id, TurnoUpdateDto dto)
        {
            try
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
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            try
            {
                var result = await _service.DeleteAsync(id);
                if (!result)
                    return NotFound(new { mensaje = $"Turno con ID {id} no encontrado" });

                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }
    }
}
