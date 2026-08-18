namespace FoodDiary.Mediator;

/// <summary>
/// Sends requests to their registered handlers.
/// </summary>
public interface ISender {
    /// <summary>
    /// Sends a strongly typed request.
    /// </summary>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="request">The request to send.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>The request response.</returns>
    Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a request that returns no application value.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <param name="request">The request to send.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : IRequest;

    /// <summary>
    /// Sends a request using its runtime type.
    /// </summary>
    /// <param name="request">The request to send.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>The boxed request response.</returns>
    Task<object?> Send(object request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a strongly typed asynchronous response stream.
    /// </summary>
    /// <typeparam name="TResponse">The streamed response type.</typeparam>
    /// <param name="request">The streaming request.</param>
    /// <param name="cancellationToken">A token that cancels stream enumeration.</param>
    /// <returns>The asynchronous response sequence.</returns>
    IAsyncEnumerable<TResponse> CreateStream<TResponse>(
        IStreamRequest<TResponse> request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates an asynchronous response stream using the request runtime type.
    /// </summary>
    /// <param name="request">The streaming request.</param>
    /// <param name="cancellationToken">A token that cancels stream enumeration.</param>
    /// <returns>The boxed asynchronous response sequence.</returns>
    IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default);
}
