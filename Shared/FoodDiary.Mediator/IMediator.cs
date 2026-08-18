namespace FoodDiary.Mediator;

/// <summary>
/// Combines request sending and notification publishing operations.
/// </summary>
public interface IMediator : ISender, IPublisher;
