using FoodDiary.Domain.Common;

namespace FoodDiary.Domain.ValueObjects;

public readonly record struct ProtectedWearableToken {
    private const string ProtectedPrefix = "fdp1:";
    private const int MaxLength = 8192;

    private ProtectedWearableToken(string value) {
        Value = value;
    }

    public string Value { get; }
    public bool IsProtected => Value?.StartsWith(ProtectedPrefix, StringComparison.Ordinal) == true;
    public bool IsCleared => string.Equals(Value, "cleared", StringComparison.Ordinal);

    internal static ProtectedWearableToken FromProtectedValue(string value) {
        string normalized = DomainGuard.RequiredText(value, MaxLength, nameof(value));
        if (!normalized.StartsWith(ProtectedPrefix, StringComparison.Ordinal)) {
            throw new ArgumentException("Wearable token must be protected before persistence.", nameof(value));
        }

        return new ProtectedWearableToken(normalized);
    }

    internal static ProtectedWearableToken FromStoredValue(string value) =>
        new(DomainGuard.RequiredText(value, MaxLength, nameof(value)));

    internal static ProtectedWearableToken Cleared => new("cleared");
}
