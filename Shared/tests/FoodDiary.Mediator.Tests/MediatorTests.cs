using System.Globalization;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Mediator.Tests;

[ExcludeFromCodeCoverage]
public sealed partial class MediatorTests {
    private static ServiceProvider CreateProvider(Action<MediatorServiceConfiguration> configure) {
        var services = new ServiceCollection();
        services.AddFoodDiaryMediator(configure);
        return services.BuildServiceProvider();
    }

    private static ServiceProvider CreateSequentialNotificationProvider() {
        var services = new ServiceCollection();
        services.AddFoodDiaryMediator(static _ => { });
        services.AddScoped<NotificationExecutionProbe>();
        services.AddTransient<INotificationHandler<SequentialNotification>, SequentialFirstNotificationHandler>();
        services.AddTransient<INotificationHandler<SequentialNotification>, SequentialSecondNotificationHandler>();
        return services.BuildServiceProvider();
    }

    private static ServiceProvider CreateFailingNotificationProvider() {
        var services = new ServiceCollection();
        services.AddFoodDiaryMediator(static _ => { });
        services.AddScoped<NotificationFailureProbe>();
        services.AddTransient<INotificationHandler<FailingNotification>, SuccessfulNotificationHandler>();
        services.AddTransient<INotificationHandler<FailingNotification>, FailingNotificationHandler>();
        services.AddTransient<INotificationHandler<FailingNotification>, NeverStartedNotificationHandler>();
        return services.BuildServiceProvider();
    }

    [ExcludeFromCodeCoverage]
    private sealed record EchoQuery(string Value) : IRequest<EchoResponse>;

    [ExcludeFromCodeCoverage]
    private sealed record EchoResponse(string Value);

    [ExcludeFromCodeCoverage]
    private sealed class EchoQueryHandler : IRequestHandler<EchoQuery, EchoResponse> {
        public Task<EchoResponse> Handle(EchoQuery request, CancellationToken cancellationToken) {
            BehaviorLog.Entries.Add("handler");
            return Task.FromResult(new EchoResponse($"handled:{request.Value}"));
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed record UnitCommand : IRequest;

    [ExcludeFromCodeCoverage]
    private sealed class UnitCommandHandler : IRequestHandler<UnitCommand, Unit> {
        public static bool Handled { get; set; }

        public Task<Unit> Handle(UnitCommand request, CancellationToken cancellationToken) {
            Handled = true;
            return Task.FromResult(Unit.Value);
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed record CapturingTokenQuery : IRequest<Unit>;

    [ExcludeFromCodeCoverage]
    private sealed class CapturingTokenHandler : IRequestHandler<CapturingTokenQuery, Unit> {
        public static CancellationToken CapturedToken { get; private set; }

        public Task<Unit> Handle(CapturingTokenQuery request, CancellationToken cancellationToken) {
            CapturedToken = cancellationToken;
            return Task.FromResult(Unit.Value);
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed record ExplicitRequest(string Value) : IRequest<string>;

    [ExcludeFromCodeCoverage]
    private sealed class ExplicitRequestHandler : IRequestHandler<ExplicitRequest, string> {
        Task<string> IRequestHandler<ExplicitRequest, string>.Handle(
            ExplicitRequest request,
            CancellationToken cancellationToken) =>
            Task.FromResult($"explicit:{request.Value}");
    }

    [ExcludeFromCodeCoverage]
    private sealed record ConcurrentRequest(int Value) : IRequest<int>;

    [ExcludeFromCodeCoverage]
    private sealed class ConcurrentRequestHandler : IRequestHandler<ConcurrentRequest, int> {
        public Task<int> Handle(ConcurrentRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(request.Value);
    }

    [ExcludeFromCodeCoverage]
    private sealed record DuplicateRequest : IRequest<string>;

    [ExcludeFromCodeCoverage]
    private sealed class DuplicateRequestHandler : IRequestHandler<DuplicateRequest, string> {
        public Task<string> Handle(DuplicateRequest request, CancellationToken cancellationToken) =>
            Task.FromResult("handled");
    }

    [ExcludeFromCodeCoverage]
    private sealed record FirstMultiRequest : IRequest<string>;

    [ExcludeFromCodeCoverage]
    private sealed record SecondMultiRequest : IRequest<int>;

    [ExcludeFromCodeCoverage]
    private sealed class MultipleRequestHandler :
        IRequestHandler<FirstMultiRequest, string>,
        IRequestHandler<SecondMultiRequest, int> {
        public Task<string> Handle(FirstMultiRequest request, CancellationToken cancellationToken) =>
            Task.FromResult("first");

        public Task<int> Handle(SecondMultiRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(2);
    }

    [ExcludeFromCodeCoverage]
    private sealed record SynchronousFailureRequest : IRequest<string>;

    [ExcludeFromCodeCoverage]
    private sealed class SynchronousFailureRequestHandler : IRequestHandler<SynchronousFailureRequest, string> {
        public Task<string> Handle(
            SynchronousFailureRequest request,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("synchronous request failure");
    }

    [ExcludeFromCodeCoverage]
    private sealed record MultipleResponseRequest : IRequest<string>, IRequest<int>;

    [ExcludeFromCodeCoverage]
    private sealed record CommandRequest(string Value) : ICommandRequest<EchoResponse>;

    private interface ICommandRequest<out TResponse> : IRequest<TResponse>;

    [ExcludeFromCodeCoverage]
    private sealed class CommandRequestHandler : IRequestHandler<CommandRequest, EchoResponse> {
        public Task<EchoResponse> Handle(CommandRequest request, CancellationToken cancellationToken) {
            BehaviorLog.Entries.Add("command-handler");
            return Task.FromResult(new EchoResponse($"command:{request.Value}"));
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class OuterBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull {
        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken) {
            BehaviorLog.Entries.Add("outer-before");
            TResponse? response = await next(cancellationToken).ConfigureAwait(false);
            BehaviorLog.Entries.Add("outer-after");
            return response;
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class InnerBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull {
        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken) {
            BehaviorLog.Entries.Add("inner-before");
            TResponse? response = await next(cancellationToken).ConfigureAwait(false);
            BehaviorLog.Entries.Add("inner-after");
            return response;
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class ShortCircuitBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull {
        public Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken) {
            BehaviorLog.Entries.Add("short-circuit");
            return typeof(TResponse) == typeof(EchoResponse)
                ? Task.FromResult((TResponse)(object)new EchoResponse("short-circuited"))
                : next(cancellationToken);
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class CommandOnlyBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : ICommandRequest<TResponse> {
        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken) {
            BehaviorLog.Entries.Add("command-behavior-before");
            TResponse? response = await next(cancellationToken).ConfigureAwait(false);
            BehaviorLog.Entries.Add("command-behavior-after");
            return response;
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class ClosedBehavior : IPipelineBehavior<EchoQuery, EchoResponse> {
        public Task<EchoResponse> Handle(
            EchoQuery request,
            RequestHandlerDelegate<EchoResponse> next,
            CancellationToken cancellationToken) =>
            next(cancellationToken);
    }

    [ExcludeFromCodeCoverage]
    private sealed class ReorderedBehavior<TResponse, TRequest> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull {
        public Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken) =>
            next(cancellationToken);
    }

    private interface OpenBehaviorInterface<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull;

    [ExcludeFromCodeCoverage]
    private abstract class AbstractBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull {
        public abstract Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken);
    }

    [ExcludeFromCodeCoverage]
    private sealed record SampleNotification(string Value) : INotification;

    [ExcludeFromCodeCoverage]
    private sealed record UnhandledNotification : INotification;

    [ExcludeFromCodeCoverage]
    private sealed class FirstNotificationHandler : INotificationHandler<SampleNotification> {
        public Task Handle(SampleNotification notification, CancellationToken cancellationToken) {
            NotificationLog.Entries.Add($"first:{notification.Value}");
            return Task.CompletedTask;
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class SecondNotificationHandler : INotificationHandler<SampleNotification> {
        public Task Handle(SampleNotification notification, CancellationToken cancellationToken) {
            NotificationLog.Entries.Add($"second:{notification.Value}");
            return Task.CompletedTask;
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed record ExplicitNotification(string Value) : INotification;

    [ExcludeFromCodeCoverage]
    private sealed class ExplicitNotificationHandler : INotificationHandler<ExplicitNotification> {
        public static string? LastValue { get; set; }

        Task INotificationHandler<ExplicitNotification>.Handle(
            ExplicitNotification notification,
            CancellationToken cancellationToken) {
            LastValue = notification.Value;
            return Task.CompletedTask;
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed record SynchronousFailureNotification : INotification;

    [ExcludeFromCodeCoverage]
    private sealed class SynchronousFailureNotificationHandler :
        INotificationHandler<SynchronousFailureNotification> {
        public Task Handle(
            SynchronousFailureNotification notification,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("synchronous notification failure");
    }

    [ExcludeFromCodeCoverage]
    private sealed record SequentialNotification : INotification;

    [ExcludeFromCodeCoverage]
    private sealed class SequentialFirstNotificationHandler(NotificationExecutionProbe probe)
        : INotificationHandler<SequentialNotification> {
        public Task Handle(SequentialNotification notification, CancellationToken cancellationToken) =>
            probe.ExecuteAsync("first", cancellationToken);
    }

    [ExcludeFromCodeCoverage]
    private sealed class SequentialSecondNotificationHandler(NotificationExecutionProbe probe)
        : INotificationHandler<SequentialNotification> {
        public Task Handle(SequentialNotification notification, CancellationToken cancellationToken) =>
            probe.ExecuteAsync("second", cancellationToken);
    }

    [ExcludeFromCodeCoverage]
    private sealed class NotificationExecutionProbe {
        private readonly Lock _sync = new();
        private int _activeHandlers;

        public List<string> Entries { get; } = [];

        public List<CancellationToken> CapturedTokens { get; } = [];

        public int MaxConcurrency { get; private set; }

        public async Task ExecuteAsync(string handlerName, CancellationToken cancellationToken) {
            lock (_sync) {
                _activeHandlers++;
                MaxConcurrency = Math.Max(MaxConcurrency, _activeHandlers);
                Entries.Add($"{handlerName}:start");
                CapturedTokens.Add(cancellationToken);
            }

            try {
                await Task.Delay(TimeSpan.FromMilliseconds(20), cancellationToken);
            } finally {
                lock (_sync) {
                    Entries.Add($"{handlerName}:end");
                    _activeHandlers--;
                }
            }
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed record FailingNotification : INotification;

    [ExcludeFromCodeCoverage]
    private sealed class SuccessfulNotificationHandler(NotificationFailureProbe probe)
        : INotificationHandler<FailingNotification> {
        public Task Handle(FailingNotification notification, CancellationToken cancellationToken) {
            probe.Entries.Add("first");
            return Task.CompletedTask;
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class FailingNotificationHandler(NotificationFailureProbe probe)
        : INotificationHandler<FailingNotification> {
        public Task Handle(FailingNotification notification, CancellationToken cancellationToken) {
            probe.Entries.Add("failing");
            return Task.FromException(new InvalidOperationException("notification failed"));
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class NeverStartedNotificationHandler(NotificationFailureProbe probe)
        : INotificationHandler<FailingNotification> {
        public Task Handle(FailingNotification notification, CancellationToken cancellationToken) {
            probe.Entries.Add("unexpected");
            return Task.CompletedTask;
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class NotificationFailureProbe {
        public List<string> Entries { get; } = [];
    }

    [ExcludeFromCodeCoverage]
    private sealed record SampleStreamRequest(int Count) : IStreamRequest<string>;

    [ExcludeFromCodeCoverage]
    private sealed class SampleStreamRequestHandler : IStreamRequestHandler<SampleStreamRequest, string> {
        public static CancellationToken CapturedToken { get; private set; }

        public async IAsyncEnumerable<string> Handle(
            SampleStreamRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken) {
            CapturedToken = cancellationToken;

            for (int index = 0; index < request.Count; index++) {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Yield();
                yield return $"item-{index.ToString(CultureInfo.InvariantCulture)}";
            }
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed record ExplicitStreamRequest(string Value) : IStreamRequest<string>;

    [ExcludeFromCodeCoverage]
    private sealed class ExplicitStreamRequestHandler : IStreamRequestHandler<ExplicitStreamRequest, string> {
        IAsyncEnumerable<string> IStreamRequestHandler<ExplicitStreamRequest, string>.Handle(
            ExplicitStreamRequest request,
            CancellationToken cancellationToken) =>
            GetResponse(request.Value);

        private static async IAsyncEnumerable<string> GetResponse(string value) {
            yield return $"explicit:{value}";
            await Task.CompletedTask;
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed record DuplicateStreamRequest : IStreamRequest<string>;

    [ExcludeFromCodeCoverage]
    private sealed class DuplicateStreamRequestHandler : IStreamRequestHandler<DuplicateStreamRequest, string> {
        public async IAsyncEnumerable<string> Handle(
            DuplicateStreamRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken) {
            yield return "handled";
            await Task.CompletedTask;
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed record MultipleResponseStreamRequest : IStreamRequest<string>, IStreamRequest<int>;

    [ExcludeFromCodeCoverage]
    private static class BehaviorLog {
        public static List<string> Entries { get; } = [];
    }

    [ExcludeFromCodeCoverage]
    private static class NotificationLog {
        public static List<string> Entries { get; } = [];
    }
}
