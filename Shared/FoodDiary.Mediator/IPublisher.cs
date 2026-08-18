namespace FoodDiary.Mediator;

/// <summary>
/// Publishes notifications to registered handlers.
/// </summary>
public interface IPublisher {
    /// <summary>
    /// Publishes a notification using its runtime type.
    /// </summary>
    /// <param name="notification">The notification to publish.</param>
    /// <param name="cancellationToken">A token that cancels publication.</param>
    Task Publish(object notification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes a strongly typed notification.
    /// </summary>
    /// <typeparam name="TNotification">The notification type.</typeparam>
    /// <param name="notification">The notification to publish.</param>
    /// <param name="cancellationToken">A token that cancels publication.</param>
    Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification;
}
