using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using turnero_medico_backend.DTOs.Common;
using turnero_medico_backend.DTOs.SecretariaDTOs;
using turnero_medico_backend.Models.Entities;
using turnero_medico_backend.Services.Interfaces;

namespace turnero_medico_backend.Services;

public class SecretariaService(
    UserManager<ApplicationUser> userManager,
    IAuditService auditService) : ISecretariaService
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly IAuditService _auditService = auditService;

    public async Task<PagedResultDto<SecretariaReadDto>> GetAllPagedAsync(int page, int pageSize)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);

        var secretarias = await _userManager.GetUsersInRoleAsync("Secretaria");

        var ordered = secretarias
            .OrderBy(u => u.Apellido)
            .ThenBy(u => u.Nombre)
            .ToList();

        var total = ordered.Count;
        var items = ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ToDto)
            .ToList();

        return new PagedResultDto<SecretariaReadDto>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<SecretariaReadDto?> GetByIdAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return null;

        var roles = await _userManager.GetRolesAsync(user);
        if (!roles.Contains("Secretaria")) return null;

        return ToDto(user);
    }

    public async Task<SecretariaReadDto> UpdateAsync(string id, SecretariaUpdateDto dto)
    {
        var user = await _userManager.FindByIdAsync(id)
            ?? throw new KeyNotFoundException($"Secretaria con ID {id} no encontrada.");

        var roles = await _userManager.GetRolesAsync(user);
        if (!roles.Contains("Secretaria"))
            throw new KeyNotFoundException($"Secretaria con ID {id} no encontrada.");

        // Verificar que el nuevo email no esté en uso por otro usuario
        if (!string.Equals(user.Email, dto.Email, StringComparison.OrdinalIgnoreCase))
        {
            var existing = await _userManager.FindByEmailAsync(dto.Email);
            if (existing != null && existing.Id != id)
                throw new InvalidOperationException("El email ya está en uso por otro usuario.");
        }

        user.Nombre = dto.Nombre.Trim();
        user.Apellido = dto.Apellido.Trim();
        user.Email = dto.Email.Trim();
        user.UserName = dto.Email.Trim();
        user.NormalizedEmail = dto.Email.Trim().ToUpperInvariant();
        user.NormalizedUserName = dto.Email.Trim().ToUpperInvariant();

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Error al actualizar: {errors}");
        }

        await _auditService.LogAsync(AuditAccion.Actualizar, "Secretaria", id);
        return ToDto(user);
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id)
            ?? throw new KeyNotFoundException($"Secretaria con ID {id} no encontrada.");

        var roles = await _userManager.GetRolesAsync(user);
        if (!roles.Contains("Secretaria"))
            throw new KeyNotFoundException($"Secretaria con ID {id} no encontrada.");

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Error al eliminar: {errors}");
        }

        await _auditService.LogAsync(AuditAccion.Eliminar, "Secretaria", id);
        return true;
    }

    private static SecretariaReadDto ToDto(ApplicationUser user) => new()
    {
        Id = user.Id,
        Nombre = user.Nombre,
        Apellido = user.Apellido,
        Email = user.Email ?? string.Empty,
        FechaRegistro = user.FechaRegistro,
    };
}
