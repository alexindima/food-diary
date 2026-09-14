using System.Reflection;
using System.Runtime.ExceptionServices;
using FoodDiary.Mediator;

namespace FoodDiary.Testing;

/// <summary>Dispatches to explicitly supplied handlers in focused tests, without runtime pipeline behaviors.</summary>
[ExcludeFromCodeCoverage]
public abstract class RequestTestSender : ISender {
    public abstract Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);

    public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest =>
        throw new NotSupportedException("Register a typed request handler in this fixture.");

    public Task<object?> Send(object request, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Use the typed request overload in this fixture.");

    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("This fixture does not dispatch streams.");

    public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("This fixture does not dispatch streams.");

    public static ISender Create(params object[] handlers) => new HandlerSender(handlers);

    public static ISender Route(params (ISender Sender, Type[] Requests)[] routes) =>
        new RoutedSender(routes.SelectMany(route => route.Requests.Select(request => (Request: request, route.Sender)))
            .ToDictionary(route => route.Request, route => route.Sender));

    protected static async Task<Unit> AsUnitAsync(Task operation) {
        await operation.ConfigureAwait(false);
        return Unit.Value;
    }

    [ExcludeFromCodeCoverage]
    private sealed class RoutedSender(IReadOnlyDictionary<Type, ISender> routes) : RequestTestSender {
        public override Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default) =>
            routes[request.GetType()].Send(request, cancellationToken);
    }

    [ExcludeFromCodeCoverage]
    private sealed class HandlerSender(object[] handlers) : RequestTestSender {
        public override Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default) {
            Type contract = typeof(IRequestHandler<,>).MakeGenericType(request.GetType(), typeof(TResponse));
            object handler = handlers.Single(contract.IsInstanceOfType);
            try {
                return (Task<TResponse>)contract.GetMethod("Handle")!.Invoke(handler, [request, cancellationToken])!;
            } catch (TargetInvocationException exception) when (exception.InnerException is not null) {
                ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
                throw;
            }
        }
    }
}
