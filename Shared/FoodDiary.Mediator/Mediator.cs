using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Mediator;

internal sealed class Mediator(IServiceProvider serviceProvider) : IMediator {
    public Task<TResponse> Send<TResponse>(
        IRequest<TResponse> request,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(request);

        Type requestType = request.GetType();
        Type responseType = typeof(TResponse);
        Type handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, responseType);
        object handler = serviceProvider.GetRequiredService(handlerType);

        RequestHandlerDelegate<TResponse> handlerDelegate = token => InvokeHandler<TResponse>(
            handler,
            request,
            cancellationToken: token);

        Type behaviorType = typeof(IPipelineBehavior<,>).MakeGenericType(requestType, responseType);
        object[] behaviors = serviceProvider
            .GetServices(behaviorType)
            .OfType<object>()
            .Reverse()
            .ToArray();

        foreach (object? behavior in behaviors) {
            RequestHandlerDelegate<TResponse> next = handlerDelegate;
            handlerDelegate = token => InvokeBehavior<TResponse>(
                behavior,
                request,
                next,
                token);
        }

        return handlerDelegate(cancellationToken);
    }

    public async Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : IRequest {
        await Send<Unit>(request, cancellationToken).ConfigureAwait(false);
    }

    public Task<object?> Send(object request, CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(request);

        Type responseType = GetRequestResponseType(request.GetType())
            ?? throw new InvalidOperationException($"Request type {request.GetType().Name} does not implement IRequest<TResponse>.");

        return SendObject(request, responseType, cancellationToken);
    }

    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
        IStreamRequest<TResponse> request,
        CancellationToken cancellationToken = default) {
        throw new NotSupportedException("Stream requests are not supported by FoodDiary.Mediator.");
    }

    public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) {
        throw new NotSupportedException("Stream requests are not supported by FoodDiary.Mediator.");
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

        Type handlerType = typeof(INotificationHandler<>).MakeGenericType(notification.GetType());
        IEnumerable<object?> handlers = serviceProvider.GetServices(handlerType);
        return PublishObjectToHandlers(handlers, notification, cancellationToken);
    }

    private static async Task<TResponse> InvokeHandler<TResponse>(
        object handler,
        object request,
        CancellationToken cancellationToken) {
        object? result = handler
            .GetType()
            .GetMethod(nameof(IRequestHandler<IRequest<TResponse>, TResponse>.Handle))!
            .Invoke(handler, [request, cancellationToken]);

        return await ((Task<TResponse>)result!).ConfigureAwait(false);
    }

    private static async Task<TResponse> InvokeBehavior<TResponse>(
        object behavior,
        object request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken) {
        object? result = behavior
            .GetType()
            .GetMethod(nameof(IPipelineBehavior<object, TResponse>.Handle))!
            .Invoke(behavior, [request, next, cancellationToken]);

        return await ((Task<TResponse>)result!).ConfigureAwait(false);
    }

    private static Type? GetRequestResponseType(Type requestType) {
        return requestType
            .GetInterfaces()
            .FirstOrDefault(static interfaceType =>
                interfaceType.IsGenericType &&
                interfaceType.GetGenericTypeDefinition() == typeof(IRequest<>))
            ?.GetGenericArguments()[0];
    }

    private async Task<object?> SendObject(
        object request,
        Type responseType,
        CancellationToken cancellationToken) {
        MethodInfo method = typeof(Mediator)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Single(static method =>
                string.Equals(method.Name, nameof(Send), StringComparison.Ordinal) &&
                method.IsGenericMethodDefinition &&
                method.GetParameters() is [{ ParameterType.IsGenericType: true } firstParameter, _] &&
                firstParameter.ParameterType.GetGenericTypeDefinition() == typeof(IRequest<>))
            .MakeGenericMethod(responseType);

        var task = (Task)method.Invoke(this, [request, cancellationToken])!;
        await task.ConfigureAwait(false);

        return task.GetType().GetProperty(nameof(Task<object>.Result))?.GetValue(task);
    }

    private static Task PublishToHandlers<TNotification>(
        IEnumerable<INotificationHandler<TNotification>> handlers,
        TNotification notification,
        CancellationToken cancellationToken)
        where TNotification : INotification {
        return Task.WhenAll(handlers.Select(handler => handler.Handle(notification, cancellationToken)));
    }

    private static Task PublishObjectToHandlers(
        IEnumerable<object?> handlers,
        object notification,
        CancellationToken cancellationToken) {
        IEnumerable<Task> tasks = handlers
            .OfType<object>()
            .Select(handler => (Task)handler
                .GetType()
                .GetMethod(nameof(INotificationHandler<INotification>.Handle))!
                .Invoke(handler, [notification, cancellationToken])!);

        return Task.WhenAll(tasks);
    }
}
