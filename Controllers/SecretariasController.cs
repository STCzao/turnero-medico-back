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
[Authorize]
public class SecretariasController(ISecretariaService _service) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PagedResultDto<SecretariaReadDto>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _service.GetAllPagedAsync(page, pageSize);
        return Ok(result);
    }

    // Accesible exclusivamente por la propia secretaria autenticada.
    // Nota: no usar [Authorize(Roles = "Admin")] en la clase porque múltiples
    // [Authorize] se combinan con AND — un Admin y Secretaria simultáneos no existen.
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
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<SecretariaReadDto>> GetById(int id)
    {
        var secretaria = await _service.GetByIdAsync(id);
        if (secretaria == null)
            return NotFound(new { mensaje = $"Secretaria con ID {id} no encontrada" });

        return Ok(secretaria);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<SecretariaReadDto>> Create(SecretariaCreateDto dto)
    {
        var secretaria = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = secretaria.Id }, secretaria);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<SecretariaReadDto>> Update(int id, SecretariaUpdateDto dto)
    {
        if (id != dto.Id)
            return BadRequest(new { mensaje = "El ID de la URL no coincide con el ID del DTO" });

        var updated = await _service.UpdateAsync(id, dto);
        return Ok(updated);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }

    [HttpPut("{id}/reactivar")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<SecretariaReadDto>> Reactivar(int id)
    {
        var secretaria = await _service.ReactivarAsync(id);
        return Ok(secretaria);
    }
}
