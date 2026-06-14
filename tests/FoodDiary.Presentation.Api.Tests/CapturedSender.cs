using FoodDiary.Mediator;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
internal sealed class CapturedSender : ISender {
    private ISender _sender = null!;

    private CapturedSender() {
    }

    public object? Request { get; private set; }

    public static CapturedSender Create<TResponse>(TResponse response) {
        CapturedSender captured = new();
        captured._sender = SubstituteSender.Create(response, request => captured.Request = request);
        return captured;
    }

    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default) =>
        _sender.Send(request, cancellationToken);

    public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : IRequest =>
        _sender.Send(request, cancellationToken);

    public Task<object?> Send(object request, CancellationToken cancellationToken = default) =>
        _sender.Send(request, cancellationToken);

    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
        IStreamRequest<TResponse> request,
        CancellationToken cancellationToken = default) =>
        _sender.CreateStream(request, cancellationToken);

    public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) =>
        _sender.CreateStream(request, cancellationToken);
}
