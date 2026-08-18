namespace FoodDiary.Mediator;

/// <summary>
/// Handles notifications of the specified type.
/// </summary>
/// <typeparam name="TNotification">The notification type.</typeparam>
public interface INotificationHandler<in TNotification>
    where TNotification : INotification {
    /// <summary>
    /// Handles the supplied notification.
    /// </summary>
    /// <param name="notification">The notification to handle.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    Task Handle(TNotification notification, CancellationToken cancellationToken);
}
