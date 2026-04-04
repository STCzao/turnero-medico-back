using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using turnero_medico_backend.DTOs.Common;
using turnero_medico_backend.DTOs.ObraSocialDTOs;
using turnero_medico_backend.Services.Interfaces;

namespace turnero_medico_backend.Controllers
{
    // Catálogo de obras sociales.
    // GET es accesible por cualquier usuario autenticado.
    // CREATE/UPDATE es Admin o Secretaria. DELETE es solo Admin.
    // El listado está cacheado en ObraSocialService — 60 minutos en IMemoryCache.
    [ApiVersion("1.0")]
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ObrasSocialesController(IObraSocialService _service) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<PagedResultDto<ObraSocialReadDto>>> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var obras = await _service.GetAllPagedAsync(page, pageSize);
            return Ok(obras);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ObraSocialReadDto>> GetById(int id)
        {
            var obra = await _service.GetByIdAsync(id);
            if (obra == null)
                return NotFound(new { mensaje = $"Obra social con ID {id} no encontrada" });
            return Ok(obra);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Secretaria")]
        public async Task<ActionResult<ObraSocialReadDto>> Create(ObraSocialCreateDto dto)
        {

            var obra = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = obra.Id }, obra);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Secretaria")]
        public async Task<ActionResult<ObraSocialReadDto>> Update(int id, ObraSocialUpdateDto dto)
        {

            var obra = await _service.UpdateAsync(id, dto);
            if (obra == null)
                return NotFound(new { mensaje = $"Obra social con ID {id} no encontrada" });
            return Ok(obra);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Delete(int id)
        {
            var success = await _service.DeleteAsync(id);
            if (!success)
                return NotFound(new { mensaje = $"Obra social con ID {id} no encontrada" });
            return NoContent();
        }
    }
}
