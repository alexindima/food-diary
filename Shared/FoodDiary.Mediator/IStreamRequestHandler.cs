namespace FoodDiary.Mediator;

/// <summary>
/// Handles a streaming request and produces an asynchronous response sequence.
/// </summary>
/// <typeparam name="TRequest">The streaming request type.</typeparam>
/// <typeparam name="TResponse">The streamed response type.</typeparam>
public interface IStreamRequestHandler<in TRequest, TResponse>
    where TRequest : IStreamRequest<TResponse> {
    /// <summary>
    /// Handles the supplied streaming request.
    /// </summary>
    /// <param name="request">The request to handle.</param>
    /// <param name="cancellationToken">A token that cancels stream enumeration.</param>
    /// <returns>The asynchronous response sequence.</returns>
    IAsyncEnumerable<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}
