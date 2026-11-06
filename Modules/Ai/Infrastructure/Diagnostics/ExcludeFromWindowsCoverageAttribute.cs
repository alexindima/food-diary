namespace FoodDiary.Modules.Ai.Infrastructure.Diagnostics;

/// <summary>Marks methods omitted only by Windows coverage collectors; Linux still measures them.</summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
internal sealed class ExcludeFromWindowsCoverageAttribute : Attribute;
