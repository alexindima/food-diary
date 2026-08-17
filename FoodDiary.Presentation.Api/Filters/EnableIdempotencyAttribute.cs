using Microsoft.AspNetCore.Mvc.Filters;

namespace FoodDiary.Presentation.Api.Filters;

[AttributeUsage(AttributeTargets.Method)]
public sealed class EnableIdempotencyAttribute(bool requireKey = false) : Attribute, IFilterMetadata {
    public bool RequireKey { get; } = requireKey;
}
