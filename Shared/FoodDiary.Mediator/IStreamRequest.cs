namespace FoodDiary.Mediator;

/// <summary>
/// Marks a request that produces an asynchronous response sequence.
/// </summary>
/// <typeparam name="TResponse">The streamed response type.</typeparam>
public interface IStreamRequest<out TResponse>;
