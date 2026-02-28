using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using turnero_medico_backend.DTOs.DoctorDTOs;
using turnero_medico_backend.Services.Interfaces;

namespace turnero_medico_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DoctoresController(IDoctorService _service) : ControllerBase
    {

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<DoctorReadDto>>> GetAll()
        {
            var doctores = await _service.GetAllAsync();
            return Ok(doctores);
        }

        /// Obtiene el perfil del doctor autenticado actual
        
        [HttpGet("me")]
        [Authorize(Roles = "Doctor")]
        public async Task<ActionResult<DoctorReadDto>> GetMyProfile()
        {
            var doctor = await _service.GetMyProfileAsync();
            if (doctor == null)
                return NotFound(new { mensaje = "No se encontró un registro de doctor asociado a tu usuario. Contacta con el administrador." });

            return Ok(doctor);
        }


        [HttpGet("especialidad/{especialidad}")]
        public async Task<ActionResult<IEnumerable<DoctorReadDto>>> GetByEspecialidad(string especialidad)
        {
            var doctores = await _service.GetByEspecialidadAsync(especialidad);
            if (!doctores.Any())
                return NotFound(new { mensaje = $"No hay doctores con especialidad '{especialidad}'" });

            return Ok(doctores);
        }


        [HttpGet("{id}")]
        public async Task<ActionResult<DoctorReadDto>> GetById(int id)
        {
            var doctor = await _service.GetByIdAsync(id);
            if (doctor == null)
                return NotFound(new { mensaje = $"Doctor con ID {id} no encontrado" });

            return Ok(doctor);
        }


        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<DoctorReadDto>> Create(DoctorCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var doctor = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = doctor.Id }, doctor);
        }


        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<DoctorReadDto>> Update(int id, DoctorUpdateDto dto)
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
        

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Delete(int id)
        {
            var result = await _service.DeleteAsync(id);
            if (!result)
                return NotFound(new { mensaje = $"Doctor con ID {id} no encontrado" });

            return NoContent();
        }
    }
}
