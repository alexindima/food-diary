namespace FoodDiary.Mediator;

/// <summary>
/// Adapts an arbitrary non-null value to the mediator notification contract.
/// </summary>
/// <typeparam name="TNotification">The wrapped value type.</typeparam>
public sealed record NotificationEnvelope<TNotification>(TNotification Value) : INotification {
    /// <summary>
    /// Gets the wrapped notification value.
    /// </summary>
    public TNotification Value {
        get;
        init {
            ArgumentNullException.ThrowIfNull(value);
            field = value;
        }
    } = EnsureValue(Value);

    private static TNotification EnsureValue(TNotification value) {
        ArgumentNullException.ThrowIfNull(value);
        return value;
    }
}
