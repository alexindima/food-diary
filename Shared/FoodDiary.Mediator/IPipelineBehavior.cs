namespace FoodDiary.Mediator;

/// <summary>
/// Wraps request handling with cross-cutting behavior.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public interface IPipelineBehavior<in TRequest, TResponse>
    where TRequest : notnull {
    /// <summary>
    /// Handles the request or delegates it to the next pipeline component.
    /// </summary>
    /// <param name="request">The request being handled.</param>
    /// <param name="next">The next pipeline component.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>The request response.</returns>
    Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken);
}
