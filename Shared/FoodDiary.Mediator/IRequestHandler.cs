namespace FoodDiary.Mediator;

/// <summary>
/// Handles a request of the specified type.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public interface IRequestHandler<in TRequest, TResponse>
    where TRequest : IRequest<TResponse> {
    /// <summary>
    /// Handles the supplied request.
    /// </summary>
    /// <param name="request">The request to handle.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>The request response.</returns>
    Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}
