using Microsoft.AspNetCore.Mvc;
using turnero_medico_backend.DTOs.PacienteDTOs;
using turnero_medico_backend.Services.Interfaces;

namespace turnero_medico_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PacientesController : ControllerBase
    {
        private readonly IPacienteService _service;

        public PacientesController(IPacienteService service)
        {
            _service = service;
        }

        /// <summary>
        /// Obtiene todos los pacientes registrados
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PacienteReadDto>>> GetAll()
        {
            try
            {
                var pacientes = await _service.GetAllAsync();
                return Ok(pacientes);
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        /// <summary>
        /// Obtiene un paciente por su ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<PacienteReadDto>> GetById(int id)
        {
            try
            {
                var paciente = await _service.GetByIdAsync(id);
                if (paciente == null)
                    return NotFound(new { mensaje = $"Paciente con ID {id} no encontrado" });

                return Ok(paciente);
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        /// <summary>
        /// Crea un nuevo paciente
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<PacienteReadDto>> Create(PacienteCreateDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var paciente = await _service.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = paciente.Id }, paciente);
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        /// <summary>
        /// Actualiza un paciente existente
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<PacienteReadDto>> Update(int id, PacienteUpdateDto dto)
        {
            try
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
            catch (Exception ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        /// <summary>
        /// Elimina un paciente
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            try
            {
                var result = await _service.DeleteAsync(id);
                if (!result)
                    return NotFound(new { mensaje = $"Paciente con ID {id} no encontrado" });

                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }
    }
}
