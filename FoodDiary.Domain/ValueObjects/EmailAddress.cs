using System.Net.Mail;

namespace FoodDiary.Domain.ValueObjects;

public readonly record struct EmailAddress {
    public string Value { get; }

    private EmailAddress(string value) {
        Value = value;
    }

    public static EmailAddress Create(string value) {
        if (string.IsNullOrWhiteSpace(value)) {
            throw new ArgumentException("Email is required.", nameof(value));
        }

        var normalized = value.Trim().ToLowerInvariant();
        try {
            var parsed = new MailAddress(normalized);
            if (!string.Equals(parsed.Address, normalized, StringComparison.Ordinal)) {
                throw new ArgumentException("Email format is invalid.", nameof(value));
            }
        } catch (FormatException) {
            throw new ArgumentException("Email format is invalid.", nameof(value));
        }

        return new EmailAddress(normalized);
    }

    public override string ToString() => Value;
}
