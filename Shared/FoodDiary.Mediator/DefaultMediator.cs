using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Mediator;

internal sealed class DefaultMediator(IServiceProvider serviceProvider) : IMediator {
    private static readonly ConcurrentDictionary<(Type Request, Type Response), RequestHandlerWrapper> RequestHandlers = new();
    private static readonly ConcurrentDictionary<(Type Request, Type Response), StreamRequestHandlerWrapper> StreamRequestHandlers = new();
    private static readonly ConcurrentDictionary<Type, NotificationHandlerWrapper> NotificationHandlers = new();

    public async Task<TResponse> Send<TResponse>(
        IRequest<TResponse> request,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(request);

        RequestHandlerWrapper handler = GetRequestHandler(request.GetType(), typeof(TResponse));
        object? response = await handler
            .Handle(request, serviceProvider, cancellationToken)
            .ConfigureAwait(false);
        return (TResponse)response!;
    }

    public async Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : IRequest {
        await Send<Unit>(request, cancellationToken).ConfigureAwait(false);
    }

    public Task<object?> Send(object request, CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(request);

        Type responseType = GetSingleResponseType(request.GetType(), typeof(IRequest<>));
        RequestHandlerWrapper handler = GetRequestHandler(request.GetType(), responseType);
        return handler.Handle(request, serviceProvider, cancellationToken);
    }

    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
        IStreamRequest<TResponse> request,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(request);

        StreamRequestHandlerWrapper handler = GetStreamRequestHandler(request.GetType(), typeof(TResponse));
        return CastStream<TResponse>(handler.Handle(request, serviceProvider, cancellationToken), cancellationToken);
    }

    public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(request);

        Type responseType = GetSingleResponseType(request.GetType(), typeof(IStreamRequest<>));
        StreamRequestHandlerWrapper handler = GetStreamRequestHandler(request.GetType(), responseType);
        return handler.Handle(request, serviceProvider, cancellationToken);
    }

    public Task Publish<TNotification>(
        TNotification notification,
        CancellationToken cancellationToken = default)
        where TNotification : INotification {
        ArgumentNullException.ThrowIfNull(notification);

        IEnumerable<INotificationHandler<TNotification>> handlers = serviceProvider.GetServices<INotificationHandler<TNotification>>();
        return PublishToHandlers(handlers, notification, cancellationToken);
    }

    public Task Publish(object notification, CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(notification);

        if (notification is not INotification) {
            throw new InvalidOperationException($"Notification type {notification.GetType().Name} does not implement INotification.");
        }

        NotificationHandlerWrapper handler = NotificationHandlers.GetOrAdd(
            notification.GetType(),
            static notificationType => (NotificationHandlerWrapper)Activator.CreateInstance(
                typeof(NotificationHandlerWrapper<>).MakeGenericType(notificationType))!);
        return handler.Handle(notification, serviceProvider, cancellationToken);
    }

    private static RequestHandlerWrapper GetRequestHandler(Type requestType, Type responseType) {
        return RequestHandlers.GetOrAdd(
            (requestType, responseType),
            static key => (RequestHandlerWrapper)Activator.CreateInstance(
                typeof(RequestHandlerWrapper<,>).MakeGenericType(key.Request, key.Response))!);
    }

    private static StreamRequestHandlerWrapper GetStreamRequestHandler(Type requestType, Type responseType) {
        return StreamRequestHandlers.GetOrAdd(
            (requestType, responseType),
            static key => (StreamRequestHandlerWrapper)Activator.CreateInstance(
                typeof(StreamRequestHandlerWrapper<,>).MakeGenericType(key.Request, key.Response))!);
    }

    private static Type GetSingleResponseType(Type requestType, Type requestInterfaceDefinition) {
        string requestInterfaceName = requestInterfaceDefinition == typeof(IRequest<>)
            ? "IRequest<TResponse>"
            : "IStreamRequest<TResponse>";
        Type[] responseTypes = [.. requestType
            .GetInterfaces()
            .Where(interfaceType =>
                interfaceType.IsGenericType &&
                interfaceType.GetGenericTypeDefinition() == requestInterfaceDefinition)
            .Select(static interfaceType => interfaceType.GetGenericArguments()[0])
            .Distinct()];

        return responseTypes.Length switch {
            0 => throw new InvalidOperationException(
                $"Request type {requestType.Name} does not implement {requestInterfaceName}."),
            1 => responseTypes[0],
            _ => throw new InvalidOperationException(
                $"Request type {requestType.Name} implements {requestInterfaceName} with multiple response types."),
        };
    }

    private static async IAsyncEnumerable<TResponse> CastStream<TResponse>(
        IAsyncEnumerable<object?> responses,
        [EnumeratorCancellation] CancellationToken cancellationToken) {
        await foreach (object? response in responses
            .WithCancellation(cancellationToken)
            .ConfigureAwait(false)) {
            yield return (TResponse)response!;
        }
    }

    private static async Task PublishToHandlers<TNotification>(
        IEnumerable<INotificationHandler<TNotification>> handlers,
        TNotification notification,
        CancellationToken cancellationToken)
        where TNotification : INotification {
        foreach (INotificationHandler<TNotification> handler in handlers) {
            await handler.Handle(notification, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task PublishObjectToHandlers<TNotification>(
        IEnumerable<INotificationHandler<TNotification>> handlers,
        object notification,
        CancellationToken cancellationToken)
        where TNotification : INotification {
        foreach (INotificationHandler<TNotification> handler in handlers) {
            await handler.Handle((TNotification)notification, cancellationToken).ConfigureAwait(false);
        }
    }

    private abstract class RequestHandlerWrapper {
        public abstract Task<object?> Handle(
            object request,
            IServiceProvider provider,
            CancellationToken cancellationToken);
    }

    private sealed class RequestHandlerWrapper<TRequest, TResponse> : RequestHandlerWrapper
        where TRequest : IRequest<TResponse> {
        public override async Task<object?> Handle(
            object request,
            IServiceProvider provider,
            CancellationToken cancellationToken) {
            var typedRequest = (TRequest)request;
            IRequestHandler<TRequest, TResponse> handler = GetSingleHandler<IRequestHandler<TRequest, TResponse>>(provider);
            RequestHandlerDelegate<TResponse> handlerDelegate = token => handler.Handle(typedRequest, token);

            foreach (IPipelineBehavior<TRequest, TResponse> behavior in provider
                .GetServices<IPipelineBehavior<TRequest, TResponse>>()
                .Reverse()) {
                RequestHandlerDelegate<TResponse> next = handlerDelegate;
                handlerDelegate = token => behavior.Handle(typedRequest, next, token);
            }

            return await handlerDelegate(cancellationToken).ConfigureAwait(false);
        }
    }

    private abstract class StreamRequestHandlerWrapper {
        public abstract IAsyncEnumerable<object?> Handle(
            object request,
            IServiceProvider provider,
            CancellationToken cancellationToken);
    }

    private sealed class StreamRequestHandlerWrapper<TRequest, TResponse> : StreamRequestHandlerWrapper
        where TRequest : IStreamRequest<TResponse> {
        public override async IAsyncEnumerable<object?> Handle(
            object request,
            IServiceProvider provider,
            [EnumeratorCancellation] CancellationToken cancellationToken) {
            IStreamRequestHandler<TRequest, TResponse> handler =
                GetSingleHandler<IStreamRequestHandler<TRequest, TResponse>>(provider);

            await foreach (TResponse response in handler
                .Handle((TRequest)request, cancellationToken)
                .WithCancellation(cancellationToken)
                .ConfigureAwait(false)) {
                yield return response;
            }
        }
    }

    private abstract class NotificationHandlerWrapper {
        public abstract Task Handle(
            object notification,
            IServiceProvider provider,
            CancellationToken cancellationToken);
    }

    private sealed class NotificationHandlerWrapper<TNotification> : NotificationHandlerWrapper
        where TNotification : INotification {
        public override Task Handle(
            object notification,
            IServiceProvider provider,
            CancellationToken cancellationToken) {
            IEnumerable<INotificationHandler<TNotification>> handlers =
                provider.GetServices<INotificationHandler<TNotification>>();
            return PublishObjectToHandlers<TNotification>(handlers, notification, cancellationToken);
        }
    }

    private static THandler GetSingleHandler<THandler>(IServiceProvider provider) {
        THandler[] handlers = [.. provider.GetServices<THandler>()];

        return handlers.Length switch {
            0 => throw new InvalidOperationException($"No mediator handler is registered for {typeof(THandler)}."),
            1 => handlers[0],
            _ => throw new InvalidOperationException($"Multiple mediator handlers are registered for {typeof(THandler)}."),
        };
    }
}
