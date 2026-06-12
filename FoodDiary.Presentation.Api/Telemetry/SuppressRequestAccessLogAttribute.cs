namespace FoodDiary.Presentation.Api.Telemetry;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true)]
public sealed class SuppressRequestAccessLogAttribute : Attribute;
