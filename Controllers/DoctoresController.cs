using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using turnero_medico_backend.DTOs.Common;
using turnero_medico_backend.DTOs.DoctorDTOs;
using turnero_medico_backend.Services.Interfaces;

namespace turnero_medico_backend.Controllers
{
    // Gestión de doctores.
    // GET por especialidad y por ID son accesibles por todos los roles autenticados
    // (paciente necesita saber qué doctores atienden su especialidad).
    // El listado completo paginado es solo Admin/Secretaria.
    // CREATE/UPDATE/DELETE son exclusivos de Admin.
    [ApiVersion("1.0")]
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DoctoresController(IDoctorService _service) : ControllerBase
    {
        // Listado completo paginado con EspecialidadNombre — solo Admin/Secretaria
        [HttpGet]
        [Authorize(Roles = "Admin,Secretaria")]
        public async Task<ActionResult<PagedResultDto<DoctorReadDto>>> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var doctores = await _service.GetAllPagedAsync(page, pageSize);
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


        // Filtro por especialidad — accesible para Pacientes al elegir doctor al solicitar turno
        [HttpGet("especialidad/{especialidadId}")]
        public async Task<ActionResult<IEnumerable<DoctorReadDto>>> GetByEspecialidad(int especialidadId)
        {
            var doctores = await _service.GetByEspecialidadAsync(especialidadId);
            if (!doctores.Any())
                return NotFound(new { mensaje = $"No hay doctores con especialidad ID '{especialidadId}'" });

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
        [Authorize(Roles = "Admin,Doctor")]
        public async Task<ActionResult<DoctorReadDto>> Update(int id, DoctorUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (id != dto.Id)
                return BadRequest(new { mensaje = "El ID de la URL no coincide con el ID del DTO" });

            var doctor = await _service.UpdateAsync(id, dto);
            return Ok(doctor);
        }
        

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Delete(int id)
        {
            await _service.DeleteAsync(id);
            return NoContent();
        }
    }
}
