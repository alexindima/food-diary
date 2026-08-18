namespace FoodDiary.Mediator;

/// <summary>
/// Marks a request that returns no application value.
/// </summary>
public interface IRequest : IRequest<Unit>;

/// <summary>
/// Marks a request that returns a response of the specified type.
/// </summary>
/// <typeparam name="TResponse">The response type.</typeparam>
public interface IRequest<out TResponse>;
