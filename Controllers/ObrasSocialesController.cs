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
        /// <summary>
        /// Obtiene todas las obras sociales
        /// Solo Admin
        /// </summary>
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

        /// <summary>
        /// Obtiene una obra social por ID
        /// Solo Admin
        /// </summary>
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

        /// <summary>
        /// Crea una nueva obra social
        /// Solo Admin
        /// </summary>
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

        /// <summary>
        /// Elimina una obra social por ID
        /// Solo Admin
        /// </summary>
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
