using Microsoft.AspNetCore.Mvc.Filters;

namespace FoodDiary.Presentation.Api.Filters;

[AttributeUsage(AttributeTargets.Method)]
public sealed class EnableIdempotencyAttribute : Attribute, IFilterMetadata;
