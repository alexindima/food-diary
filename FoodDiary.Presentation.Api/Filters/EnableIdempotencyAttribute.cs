using Microsoft.AspNetCore.Mvc.Filters;

namespace FoodDiary.Presentation.Api.Filters;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class EnableIdempotencyAttribute : Attribute, IFilterMetadata;
