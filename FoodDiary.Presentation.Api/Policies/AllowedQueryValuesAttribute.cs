using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace FoodDiary.Presentation.Api.Policies;

[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
public sealed class AllowedQueryValuesAttribute(params string[] values) : ValidationAttribute {
    public IReadOnlyList<string> Values { get; } = values;

    public override bool IsValid(object? value) =>
        value is null || (value is string text && Values.Contains(text, StringComparer.OrdinalIgnoreCase));

    public override string FormatErrorMessage(string name) =>
        string.Format(
            CultureInfo.InvariantCulture,
            "The {0} field must be one of: {1}.",
            name,
            string.Join(", ", Values));
}
