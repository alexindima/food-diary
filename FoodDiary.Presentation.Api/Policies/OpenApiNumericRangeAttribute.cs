namespace FoodDiary.Presentation.Api.Policies;

[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
public sealed class OpenApiNumericRangeAttribute(double minimum) : Attribute {
    public OpenApiNumericRangeAttribute(double minimum, double maximum)
        : this(minimum) {
        Maximum = maximum;
    }

    public double Minimum { get; } = minimum;

    public double? Maximum { get; }
}
