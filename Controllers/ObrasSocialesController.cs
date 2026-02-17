using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using turnero_medico_backend.DTOs.ObraSocialDTOs;
using turnero_medico_backend.Services.Interfaces;

namespace turnero_medico_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ObrasSocialesController(IObraSocialService _service) : ControllerBase
    {
        /// Obtiene todas las obras sociales
        /// Solo Admin
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ObraSocialReadDto>>> GetAll()
        {
            try
            {
                var obras = await _service.GetAllAsync();
                return Ok(obras);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
        }

        /// Obtiene una obra social por ID
        /// Solo Admin
        
        [HttpGet("{id}")]
        public async Task<ActionResult<ObraSocialReadDto>> GetById(int id)
        {
            try
            {
                var obra = await _service.GetByIdAsync(id);
                if (obra == null)
                    return NotFound(new { mensaje = $"Obra social con ID {id} no encontrada" });

                return Ok(obra);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
        }

        /// Crea una nueva obra social
        /// Solo Admin
        
        [HttpPost]
        public async Task<ActionResult<ObraSocialReadDto>> Create(ObraSocialCreateDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var obra = await _service.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = obra.Id }, obra);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        /// Elimina una obra social por ID
        /// Solo Admin
        
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            try
            {
                var success = await _service.DeleteAsync(id);
                if (!success)
                    return NotFound(new { mensaje = $"Obra social con ID {id} no encontrada" });

                return NoContent();
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
        }
    }
}
