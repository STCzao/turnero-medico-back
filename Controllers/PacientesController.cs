using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using turnero_medico_backend.DTOs.Common;
using turnero_medico_backend.DTOs.PacienteDTOs;
using turnero_medico_backend.Models.Entities;
using turnero_medico_backend.Services.Interfaces;

namespace turnero_medico_backend.Controllers
{
    [ApiVersion("1.0")]
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PacientesController(IPacienteService _service) : ControllerBase
    {

        [HttpGet]
        [Authorize(Roles = "Admin,Secretaria")]
        public async Task<ActionResult<PagedResultDto<PacienteReadDto>>> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var pacientes = await _service.GetAllPagedAsync(page, pageSize);
            return Ok(pacientes);
        }

        // Obtiene el perfil del paciente autenticado actual (sin necesitar conocer el ID numérico)
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

        // Creación directa de un registro de paciente (sin cuenta de usuario).
        // Solo Admin y Secretaria pueden hacerlo. El auto-registro con cuenta va por /api/auth/register-paciente.
        [HttpPost]
        [Authorize(Roles = "Admin,Secretaria")]
        public async Task<ActionResult<PacienteReadDto>> Create(PacienteCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var paciente = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = paciente.Id }, paciente);
        }


        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<PacienteReadDto>> Update(int id, PacienteUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (id != dto.Id)
                return BadRequest(new { mensaje = "El ID de la URL no coincide con el ID del DTO" });

            var paciente = await _service.UpdateAsync(id, dto);
            return Ok(paciente);
        }


        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Delete(int id)
        {
            await _service.DeleteAsync(id);
            return NoContent();
        }

        // Dependientes del paciente autenticado
        [HttpGet("mis-dependientes")]
        [Authorize(Roles = "Paciente")]
        public async Task<ActionResult<PagedResultDto<PacienteReadDto>>> GetMisDependientes([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var dependientes = await _service.GetMisDependientesAsync(page, pageSize);
            return Ok(dependientes);
        }

        // Registrar un dependiente (menor sin cuenta de usuario)
        [HttpPost("dependientes")]
        [Authorize(Roles = "Paciente")]
        public async Task<ActionResult<PacienteReadDto>> CreateDependiente(DependienteCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var dependiente = await _service.CreateDependienteAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = dependiente.Id }, dependiente);
        }

        [HttpPut("dependientes/{id}")]
        [Authorize(Roles = "Paciente")]
        public async Task<ActionResult<PacienteReadDto>> UpdateDependiente(int id, DependienteUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (id != dto.Id)
                return BadRequest(new { mensaje = "El ID de la URL no coincide con el ID del DTO" });

            var dependiente = await _service.UpdateDependienteAsync(id, dto);
            return Ok(dependiente);
        }

        [HttpDelete("dependientes/{id}")]
        [Authorize(Roles = "Paciente")]
        public async Task<ActionResult> DeleteDependiente(int id)
        {
            await _service.DeleteDependienteAsync(id);
            return NoContent();
        }

        // Exporta todos los datos personales del paciente autenticado (cumplimiento GDPR/LGPD).
        // Incluye datos del perfil, cobertura y el historial completo de turnos.
        [HttpGet("me/export")]
        [Authorize(Roles = "Paciente")]
        public async Task<ActionResult<PacienteExportDto>> ExportarMisDatos()
        {
            var export = await _service.ExportarMisDatosAsync();
            if (export == null)
                return NotFound(new { mensaje = "No se encontró un registro de paciente asociado a tu usuario." });

            return Ok(export);
        }
    }
}
