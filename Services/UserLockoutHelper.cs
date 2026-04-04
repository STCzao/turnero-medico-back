using Microsoft.AspNetCore.Identity;
using turnero_medico_backend.Models.Entities;

namespace turnero_medico_backend.Services
{
    internal static class UserLockoutHelper
    {
        /// Bloquea la cuenta del usuario de forma permanente (soft-delete de entidad vinculada).
        internal static async Task LockUserAsync(UserManager<ApplicationUser> userManager, string? userId)
        {
            if (string.IsNullOrEmpty(userId)) return;
            var user = await userManager.FindByIdAsync(userId);
            if (user == null) return;
            await userManager.SetLockoutEnabledAsync(user, true);
            await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
        }

        /// Desbloquea la cuenta del usuario (reactivación de entidad vinculada).
        internal static async Task UnlockUserAsync(UserManager<ApplicationUser> userManager, string? userId)
        {
            if (string.IsNullOrEmpty(userId)) return;
            var user = await userManager.FindByIdAsync(userId);
            if (user == null) return;
            await userManager.SetLockoutEndDateAsync(user, null);
        }
    }
}
