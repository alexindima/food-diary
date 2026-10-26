using FoodDiary.Modules.Users.Contracts.Models;

namespace FoodDiary.Modules.Identity.Application.Authentication.Models;

public sealed record BrowserSessionIdentityModel(Guid SessionId, DateTime ExpiresAtUtc, UserSessionPrincipalModel Principal);
