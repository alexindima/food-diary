using FoodDiary.Mediator;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
internal sealed class RecordingSender(object response) : ISender {
    public object? Request { get; private set; }
    public CancellationToken CancellationToken { get; private set; }

    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default) {
        Request = request;
        CancellationToken = cancellationToken;
        return Task.FromResult((TResponse)response);
    }

    public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : IRequest {
        Request = request;
        CancellationToken = cancellationToken;
        return Task.CompletedTask;
    }

    public Task<object?> Send(object request, CancellationToken cancellationToken = default) {
        Request = request;
        CancellationToken = cancellationToken;
        return Task.FromResult<object?>(response);
    }

    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
        IStreamRequest<TResponse> request,
        CancellationToken cancellationToken = default) {
        throw new NotSupportedException();
    }

    public IAsyncEnumerable<object?> CreateStream(
        object request,
        CancellationToken cancellationToken = default) {
        throw new NotSupportedException();
    }
}
