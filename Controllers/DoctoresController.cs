using Microsoft.AspNetCore.Mvc;
using turnero_medico_backend.DTOs.DoctorDTOs;
using turnero_medico_backend.Services;
using turnero_medico_backend.Services.Interfaces;

namespace turnero_medico_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DoctoresController(DoctorService _service) : ControllerBase
    {

        [HttpGet]
        public async Task<ActionResult<IEnumerable<DoctorReadDto>>> GetAll()
        {
            try
            {
                var doctores = await _service.GetAllAsync();
                return Ok(doctores);
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }


        [HttpGet("especialidad/{especialidad}")]
        public async Task<ActionResult<IEnumerable<DoctorReadDto>>> GetByEspecialidad(string especialidad)
        {
            try
            {
                var doctores = await _service.GetByEspecialidadAsync(especialidad);
                if (!doctores.Any())
                    return NotFound(new { mensaje = $"No hay doctores con especialidad '{especialidad}'" });

                return Ok(doctores);
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }


        [HttpGet("{id}")]
        public async Task<ActionResult<DoctorReadDto>> GetById(int id)
        {
            try
            {
                var doctor = await _service.GetByIdAsync(id);
                if (doctor == null)
                    return NotFound(new { mensaje = $"Doctor con ID {id} no encontrado" });

                return Ok(doctor);
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }


        [HttpPost]
        public async Task<ActionResult<DoctorReadDto>> Create(DoctorCreateDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var doctor = await _service.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = doctor.Id }, doctor);
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }


        [HttpPut("{id}")]
        public async Task<ActionResult<DoctorReadDto>> Update(int id, DoctorUpdateDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                if (id != dto.Id)
                    return BadRequest(new { mensaje = "El ID de la URL no coincide con el ID del DTO" });

                var doctor = await _service.UpdateAsync(id, dto);
                if (doctor == null)
                    return NotFound(new { mensaje = $"Doctor con ID {id} no encontrado" });

                return Ok(doctor);
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }
        

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            try
            {
                var result = await _service.DeleteAsync(id);
                if (!result)
                    return NotFound(new { mensaje = $"Doctor con ID {id} no encontrado" });

                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }
    }
}
