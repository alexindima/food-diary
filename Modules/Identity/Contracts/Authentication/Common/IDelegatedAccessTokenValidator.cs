using System.Security.Claims;

namespace FoodDiary.Modules.Identity.Contracts.Authentication.Common;

public interface IDelegatedAccessTokenValidator {
    Task<bool> IsCurrentAsync(ClaimsPrincipal principal, CancellationToken cancellationToken);
}
