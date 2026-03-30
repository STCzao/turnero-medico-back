using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using turnero_medico_backend.DTOs.Common;
using turnero_medico_backend.DTOs.SecretariaDTOs;
using turnero_medico_backend.Services.Interfaces;

namespace turnero_medico_backend.Controllers;

// CRUD de secretarias — exclusivo para Admin.
// Las secretarias no tienen entidad de dominio propia; sus datos viven en AspNetUsers
// con el rol "Secretaria". El registro se hace vía /api/auth/register-secretaria.
[ApiVersion("1.0")]
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class SecretariasController(ISecretariaService _service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResultDto<SecretariaReadDto>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _service.GetAllPagedAsync(page, pageSize);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SecretariaReadDto>> GetById(string id)
    {
        var secretaria = await _service.GetByIdAsync(id);
        if (secretaria == null)
            return NotFound(new { mensaje = $"Secretaria con ID {id} no encontrada" });

        return Ok(secretaria);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<SecretariaReadDto>> Update(string id, SecretariaUpdateDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (id != dto.Id)
            return BadRequest(new { mensaje = "El ID de la URL no coincide con el ID del DTO" });

        var updated = await _service.UpdateAsync(id, dto);
        return Ok(updated);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(string id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }
}
