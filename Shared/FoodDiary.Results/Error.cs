using System.Collections;

namespace FoodDiary.Results;

/// <summary>
/// Describes a failure without coupling application code to a transport or provider.
/// </summary>
/// <param name="Code">The stable, machine-readable error code.</param>
/// <param name="Message">The human-readable error message.</param>
/// <param name="Kind">The optional transport-neutral error classification.</param>
/// <param name="Details">Optional field-specific error messages.</param>
public sealed record Error(
    string Code,
    string Message,
    ErrorKind? Kind = null,
    IReadOnlyDictionary<string, string[]>? Details = null) {
    /// <summary>
    /// Gets the stable, machine-readable error code.
    /// </summary>
    public string Code {
        get;
        init => field = value ?? throw new ArgumentNullException(nameof(value));
    } = Code ?? throw new ArgumentNullException(nameof(Code));

    /// <summary>
    /// Gets the human-readable error message.
    /// </summary>
    public string Message {
        get;
        init => field = value ?? throw new ArgumentNullException(nameof(value));
    } = Message ?? throw new ArgumentNullException(nameof(Message));

    /// <summary>
    /// Gets a read-only snapshot of the field-specific error messages.
    /// </summary>
    public IReadOnlyDictionary<string, string[]>? Details {
        get;
        init => field = SnapshotDetails(value);
    } = SnapshotDetails(Details);

    /// <summary>
    /// Represents the absence of an error for a successful result.
    /// </summary>
    public static readonly Error None = new(string.Empty, string.Empty);

    /// <summary>
    /// Returns the machine-readable error code.
    /// </summary>
    /// <param name="error">The error to convert.</param>
    [Obsolete("Use Error.Code explicitly.")]
    public static implicit operator string(Error error) => error.Code;

    private static IReadOnlyDictionary<string, string[]>? SnapshotDetails(
        IReadOnlyDictionary<string, string[]>? details) =>
        details is null ? null : new ErrorDetailsSnapshot(details);

    private sealed class ErrorDetailsSnapshot : IReadOnlyDictionary<string, string[]>, IEquatable<ErrorDetailsSnapshot> {
        private readonly Dictionary<string, string[]> _details;

        public ErrorDetailsSnapshot(IReadOnlyDictionary<string, string[]> details) {
            _details = new Dictionary<string, string[]>(details.Count, StringComparer.Ordinal);
            foreach ((string key, string[] messages) in details) {
                if (messages is null) {
                    throw new ArgumentException("Detail message collections cannot be null.", nameof(details));
                }

                if (messages.Any(static message => message is null)) {
                    throw new ArgumentException("Detail messages cannot contain null values.", nameof(details));
                }

                _details.Add(key, [.. messages]);
            }
        }

        public int Count => _details.Count;
        public IEnumerable<string> Keys => _details.Keys;
        public IEnumerable<string[]> Values => _details.Values.Select(static messages => messages.ToArray());
        public string[] this[string key] => [.. _details[key]];

        public bool ContainsKey(string key) => _details.ContainsKey(key);

        public bool TryGetValue(string key, out string[] value) {
            if (!_details.TryGetValue(key, out string[]? messages)) {
                value = [];
                return false;
            }

            value = [.. messages];
            return true;
        }

        public bool Equals(ErrorDetailsSnapshot? other) {
            if (ReferenceEquals(this, other)) {
                return true;
            }

            if (other is null || _details.Count != other._details.Count) {
                return false;
            }

            foreach ((string key, string[] messages) in _details) {
                if (!other._details.TryGetValue(key, out string[]? otherMessages) ||
                    !messages.SequenceEqual(otherMessages, StringComparer.Ordinal)) {
                    return false;
                }
            }

            return true;
        }

        public override bool Equals(object? obj) => Equals(obj as ErrorDetailsSnapshot);

        public override int GetHashCode() {
            var hash = new HashCode();
            foreach ((string key, string[] messages) in _details.OrderBy(static pair => pair.Key, StringComparer.Ordinal)) {
                hash.Add(key, StringComparer.Ordinal);
                hash.Add(messages.Length);
                foreach (string message in messages) {
                    hash.Add(message, StringComparer.Ordinal);
                }
            }

            return hash.ToHashCode();
        }

        public IEnumerator<KeyValuePair<string, string[]>> GetEnumerator() =>
            _details
                .Select(static pair => new KeyValuePair<string, string[]>(pair.Key, [.. pair.Value]))
                .GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
