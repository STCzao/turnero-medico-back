using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using turnero_medico_backend.DTOs.Common;
using turnero_medico_backend.DTOs.PacienteDTOs;
using turnero_medico_backend.Models.Entities;
using turnero_medico_backend.Services.Interfaces;

namespace turnero_medico_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PacientesController(IPacienteService _service) : ControllerBase
    {
        
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<PagedResultDto<PacienteReadDto>>> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var pacientes = await _service.GetAllPagedAsync(page, pageSize);
            return Ok(pacientes);
        }

        /// 
        /// Obtiene el perfil del paciente autenticado actual
        
        [HttpGet("me")]
        public async Task<ActionResult<PacienteReadDto>> GetMyProfile()
        {
            var paciente = await _service.GetMyProfileAsync();
            if (paciente == null)
                return NotFound(new { mensaje = "No se encontró un registro de paciente asociado a tu usuario. Contacta con el administrador." });

            return Ok(paciente);
        }


        [HttpGet("{id}")]
        public async Task<ActionResult<PacienteReadDto>> GetById(int id)
        {
            var paciente = await _service.GetByIdAsync(id);
            if (paciente == null)
                return NotFound(new { mensaje = $"Paciente con ID {id} no encontrado" });

            return Ok(paciente);
        }

        [HttpPost]
        public async Task<ActionResult<PacienteReadDto>> Create(PacienteCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var paciente = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = paciente.Id }, paciente);
        }


        [HttpPut("{id}")]
        public async Task<ActionResult<PacienteReadDto>> Update(int id, PacienteUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (id != dto.Id)
                return BadRequest(new { mensaje = "El ID de la URL no coincide con el ID del DTO" });

            var paciente = await _service.UpdateAsync(id, dto);
            if (paciente == null)
                return NotFound(new { mensaje = $"Paciente con ID {id} no encontrado" });

            return Ok(paciente);
        }


        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            var result = await _service.DeleteAsync(id);
            if (!result)
                return NotFound(new { mensaje = $"Paciente con ID {id} no encontrado" });

            return NoContent();
        }
    }
}
