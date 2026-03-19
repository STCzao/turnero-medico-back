using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using turnero_medico_backend.DTOs.EspecialidadDTOs;
using turnero_medico_backend.Services.Interfaces;

namespace turnero_medico_backend.Controllers
{
    [ApiVersion("1.0")]
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class EspecialidadesController(IEspecialidadService _service) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<IEnumerable<EspecialidadReadDto>>> GetAll()
        {
            var especialidades = await _service.GetAllAsync();
            return Ok(especialidades);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<EspecialidadReadDto>> GetById(int id)
        {
            var especialidad = await _service.GetByIdAsync(id);
            if (especialidad == null)
                return NotFound(new { mensaje = $"Especialidad con ID {id} no encontrada" });
            return Ok(especialidad);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<EspecialidadReadDto>> Create(EspecialidadCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var especialidad = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = especialidad.Id }, especialidad);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<EspecialidadReadDto>> Update(int id, EspecialidadUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var especialidad = await _service.UpdateAsync(id, dto);
            if (especialidad == null)
                return NotFound(new { mensaje = $"Especialidad con ID {id} no encontrada" });
            return Ok(especialidad);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Delete(int id)
        {
            var success = await _service.DeleteAsync(id);
            if (!success)
                return NotFound(new { mensaje = $"Especialidad con ID {id} no encontrada" });
            return NoContent();
        }
    }
}
