using Microsoft.AspNetCore.Mvc.Filters;

namespace FoodDiary.Modules.Identity.Presentation.Filters;

/// <summary>Leaves the public browser refresh session unchanged for independent bearer flows.</summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class SuppressRefreshTokenCookieAttribute : Attribute, IFilterMetadata;
