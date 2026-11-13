namespace FoodDiary.Domain.Primitives;

public closed record class FieldChange<T> where T : notnull {
    private FieldChange() { }

    internal static FieldChange<T> Unchanged { get; } = new UnchangedChange();
    internal static FieldChange<T> Clear { get; } = new ClearChange();
    public bool IsUnchanged => this is UnchangedChange;
    public bool IsSet => this is SetChange;
    public bool IsClear => this is ClearChange;
    public T Value => this is SetChange change ? change.NewValue : throw new InvalidOperationException("Only a set field change has a value.");

    internal static FieldChange<T> Set(T value) {
        ArgumentNullException.ThrowIfNull(value);
        return new SetChange(value);
    }

    private sealed record UnchangedChange : FieldChange<T>;
    private sealed record ClearChange : FieldChange<T>;
    private sealed record SetChange(T NewValue) : FieldChange<T>;
}
