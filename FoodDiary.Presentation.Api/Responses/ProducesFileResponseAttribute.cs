namespace FoodDiary.Presentation.Api.Responses;

[AttributeUsage(AttributeTargets.Method)]
public sealed class ProducesFileResponseAttribute(params string[] contentTypes) : Attribute {
    public IReadOnlyList<string> ContentTypes { get; } = Array.AsReadOnly((string[])contentTypes.Clone());
}
