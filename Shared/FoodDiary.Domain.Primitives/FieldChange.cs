namespace FoodDiary.Domain.Primitives;

public sealed record FieldChange<T> where T : notnull {
    private enum ChangeKind { Unchanged = 0, Set = 1, Clear = 2 }

    private readonly ChangeKind _kind;

    private FieldChange(ChangeKind kind, T? value) {
        _kind = kind;
        Value = value;
    }

    internal static FieldChange<T> Unchanged { get; } = new(ChangeKind.Unchanged, default);
    internal static FieldChange<T> Clear { get; } = new(ChangeKind.Clear, default);
    public bool IsUnchanged => _kind == ChangeKind.Unchanged;
    public bool IsSet => _kind == ChangeKind.Set;
    public bool IsClear => _kind == ChangeKind.Clear;
    public T Value => IsSet ? field! : throw new InvalidOperationException("Only a set field change has a value.");

    internal static FieldChange<T> Set(T value) {
        ArgumentNullException.ThrowIfNull(value);
        return new FieldChange<T>(ChangeKind.Set, value);
    }
}
