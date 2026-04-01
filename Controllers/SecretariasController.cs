using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using turnero_medico_backend.DTOs.Common;
using turnero_medico_backend.DTOs.SecretariaDTOs;
using turnero_medico_backend.Services.Interfaces;

namespace turnero_medico_backend.Controllers;

// CRUD de secretarias — exclusivo para Admin.
// Sigue el mismo patrón que DoctoresController:
// el Admin crea el registro (POST) y luego registra la cuenta vía /api/auth/register-secretaria.
// GET /me es accesible por la propia secretaria autenticada.
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

    [HttpGet("me")]
    [Authorize(Roles = "Secretaria")]
    public async Task<ActionResult<SecretariaReadDto>> GetMyProfile()
    {
        var secretaria = await _service.GetMyProfileAsync();
        if (secretaria == null)
            return NotFound(new { mensaje = "No se encontró un registro de secretaria asociado a tu usuario. Contacta con el administrador." });

        return Ok(secretaria);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SecretariaReadDto>> GetById(int id)
    {
        var secretaria = await _service.GetByIdAsync(id);
        if (secretaria == null)
            return NotFound(new { mensaje = $"Secretaria con ID {id} no encontrada" });

        return Ok(secretaria);
    }

    [HttpPost]
    public async Task<ActionResult<SecretariaReadDto>> Create(SecretariaCreateDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var secretaria = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = secretaria.Id }, secretaria);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<SecretariaReadDto>> Update(int id, SecretariaUpdateDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (id != dto.Id)
            return BadRequest(new { mensaje = "El ID de la URL no coincide con el ID del DTO" });

        var updated = await _service.UpdateAsync(id, dto);
        return Ok(updated);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }
}
