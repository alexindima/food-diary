namespace FoodDiary.Mediator;

public interface IRequest : IRequest<Unit>;

public interface IRequest<out TResponse>;

public interface IStreamRequest<out TResponse>;

public interface IRequestHandler<in TRequest, TResponse>
    where TRequest : IRequest<TResponse> {
    Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}

public interface INotification;

public interface INotificationHandler<in TNotification>
    where TNotification : INotification {
    Task Handle(TNotification notification, CancellationToken cancellationToken);
}

public delegate Task<TResponse> RequestHandlerDelegate<TResponse>(CancellationToken cancellationToken = default);

public interface IPipelineBehavior<in TRequest, TResponse>
    where TRequest : notnull {
    Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken);
}

public readonly record struct Unit {
    public static Unit Value { get; } = new();
}

public interface ISender {
    Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);

    Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : IRequest;

    Task<object?> Send(object request, CancellationToken cancellationToken = default);

    IAsyncEnumerable<TResponse> CreateStream<TResponse>(
        IStreamRequest<TResponse> request,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default);
}

public interface IPublisher {
    Task Publish(object notification, CancellationToken cancellationToken = default);

    Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification;
}

public interface IMediator : ISender, IPublisher;
