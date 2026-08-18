namespace FoodDiary.Mediator;

/// <summary>
/// Represents the absence of an application response value.
/// </summary>
public readonly record struct Unit {
    /// <summary>
    /// Gets the single unit value.
    /// </summary>
    public static Unit Value { get; }
}
