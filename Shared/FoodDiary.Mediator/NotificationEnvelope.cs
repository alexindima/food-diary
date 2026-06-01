namespace FoodDiary.Mediator;

public sealed record NotificationEnvelope<TNotification>(TNotification Value) : INotification;
