namespace FoodDiary.Mediator;

/// <summary>
/// Represents the next request handler or pipeline component.
/// </summary>
/// <typeparam name="TResponse">The response type.</typeparam>
/// <param name="cancellationToken">A token that cancels the operation.</param>
/// <returns>The request response.</returns>
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>(CancellationToken cancellationToken = default);
