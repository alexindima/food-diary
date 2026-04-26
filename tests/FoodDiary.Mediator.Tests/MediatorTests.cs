using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Mediator.Tests;

public sealed class MediatorTests {
    [Fact]
    public async Task Send_WithTypedRequest_InvokesMatchingHandler() {
        using var provider = CreateProvider(configuration => configuration.RegisterServicesFromAssembly(typeof(MediatorTests).Assembly));
        var sender = provider.GetRequiredService<ISender>();

        var response = await sender.Send(new EchoQuery("value"));

        Assert.Equal("handled:value", response.Value);
    }

    [Fact]
    public async Task Send_WithObjectRequest_ReturnsHandlerResponse() {
        using var provider = CreateProvider(configuration => configuration.RegisterServicesFromAssembly(typeof(MediatorTests).Assembly));
        var sender = provider.GetRequiredService<ISender>();

        var response = await sender.Send((object)new EchoQuery("object-value"));

        var echoResponse = Assert.IsType<EchoResponse>(response);
        Assert.Equal("handled:object-value", echoResponse.Value);
    }

    [Fact]
    public async Task Send_WithUnitRequest_InvokesHandlerAndReturnsTask() {
        using var provider = CreateProvider(configuration => configuration.RegisterServicesFromAssembly(typeof(MediatorTests).Assembly));
        var sender = provider.GetRequiredService<ISender>();
        UnitCommandHandler.Handled = false;

        await sender.Send(new UnitCommand());

        Assert.True(UnitCommandHandler.Handled);
    }

    [Fact]
    public async Task Send_PassesCancellationTokenToHandler() {
        using var provider = CreateProvider(configuration => configuration.RegisterServicesFromAssembly(typeof(MediatorTests).Assembly));
        var sender = provider.GetRequiredService<ISender>();
        using var cancellationTokenSource = new CancellationTokenSource();

        await sender.Send(new CapturingTokenQuery(), cancellationTokenSource.Token);

        Assert.Equal(cancellationTokenSource.Token, CapturingTokenHandler.CapturedToken);
    }

    [Fact]
    public async Task Send_AppliesOpenBehaviorsInRegistrationOrder() {
        using var provider = CreateProvider(configuration => {
            configuration.RegisterServicesFromAssembly(typeof(MediatorTests).Assembly);
            configuration.AddOpenBehavior(typeof(OuterBehavior<,>));
            configuration.AddOpenBehavior(typeof(InnerBehavior<,>));
        });
        var sender = provider.GetRequiredService<ISender>();
        BehaviorLog.Entries.Clear();

        var response = await sender.Send(new EchoQuery("pipeline"));

        Assert.Equal("handled:pipeline", response.Value);
        Assert.Equal(
            [
                "outer-before",
                "inner-before",
                "handler",
                "inner-after",
                "outer-after"
            ],
            BehaviorLog.Entries);
    }

    [Fact]
    public async Task Send_WhenBehaviorShortCircuits_DoesNotInvokeHandler() {
        using var provider = CreateProvider(configuration => {
            configuration.RegisterServicesFromAssembly(typeof(MediatorTests).Assembly);
            configuration.AddOpenBehavior(typeof(ShortCircuitBehavior<,>));
        });
        var sender = provider.GetRequiredService<ISender>();
        BehaviorLog.Entries.Clear();

        var response = await sender.Send(new EchoQuery("ignored"));

        Assert.Equal("short-circuited", response.Value);
        Assert.Equal(["short-circuit"], BehaviorLog.Entries);
    }

    [Fact]
    public async Task Send_WhenOpenBehaviorConstraintDoesNotMatch_SkipsBehavior() {
        using var provider = CreateProvider(configuration => {
            configuration.RegisterServicesFromAssembly(typeof(MediatorTests).Assembly);
            configuration.AddOpenBehavior(typeof(CommandOnlyBehavior<,>));
        });
        var sender = provider.GetRequiredService<ISender>();
        BehaviorLog.Entries.Clear();

        var response = await sender.Send(new EchoQuery("query"));

        Assert.Equal("handled:query", response.Value);
        Assert.Equal(["handler"], BehaviorLog.Entries);
    }

    [Fact]
    public async Task Send_WhenOpenBehaviorConstraintMatches_AppliesBehavior() {
        using var provider = CreateProvider(configuration => {
            configuration.RegisterServicesFromAssembly(typeof(MediatorTests).Assembly);
            configuration.AddOpenBehavior(typeof(CommandOnlyBehavior<,>));
        });
        var sender = provider.GetRequiredService<ISender>();
        BehaviorLog.Entries.Clear();

        var response = await sender.Send(new CommandRequest("command"));

        Assert.Equal("command:command", response.Value);
        Assert.Equal(["command-behavior-before", "command-handler", "command-behavior-after"], BehaviorLog.Entries);
    }

    [Fact]
    public async Task Publish_WithTypedNotification_InvokesAllHandlers() {
        using var provider = CreateProvider(configuration => configuration.RegisterServicesFromAssembly(typeof(MediatorTests).Assembly));
        var publisher = provider.GetRequiredService<IPublisher>();
        NotificationLog.Entries.Clear();

        await publisher.Publish(new SampleNotification("typed"));

        Assert.Equal(["first:typed", "second:typed"], NotificationLog.Entries.Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task Publish_WithObjectNotification_InvokesAllHandlers() {
        using var provider = CreateProvider(configuration => configuration.RegisterServicesFromAssembly(typeof(MediatorTests).Assembly));
        var publisher = provider.GetRequiredService<IPublisher>();
        NotificationLog.Entries.Clear();

        await publisher.Publish((object)new SampleNotification("object"));

        Assert.Equal(["first:object", "second:object"], NotificationLog.Entries.Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task Publish_WithNonNotificationObject_ThrowsInvalidOperationException() {
        using var provider = CreateProvider(configuration => configuration.RegisterServicesFromAssembly(typeof(MediatorTests).Assembly));
        var publisher = provider.GetRequiredService<IPublisher>();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => publisher.Publish(new object()));

        Assert.Contains("does not implement INotification", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Send_WhenHandlerIsMissing_ThrowsInvalidOperationException() {
        using var provider = CreateProvider(static _ => { });
        var sender = provider.GetRequiredService<ISender>();

        await Assert.ThrowsAsync<InvalidOperationException>(() => sender.Send(new EchoQuery("missing")));
    }

    [Fact]
    public async Task Send_WithObjectThatIsNotRequest_ThrowsInvalidOperationException() {
        using var provider = CreateProvider(static _ => { });
        var sender = provider.GetRequiredService<ISender>();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sender.Send(new object()));

        Assert.Contains("does not implement IRequest<TResponse>", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CreateStream_ThrowsNotSupportedException() {
        using var provider = CreateProvider(static _ => { });
        var sender = provider.GetRequiredService<ISender>();

        Assert.Throws<NotSupportedException>(() => sender.CreateStream(new SampleStreamRequest()));
        Assert.Throws<NotSupportedException>(() => sender.CreateStream((object)new SampleStreamRequest()));
    }

    private static ServiceProvider CreateProvider(Action<MediatorServiceConfiguration> configure) {
        var services = new ServiceCollection();
        services.AddFoodDiaryMediator(configure);
        return services.BuildServiceProvider();
    }

    private sealed record EchoQuery(string Value) : IRequest<EchoResponse>;

    private sealed record EchoResponse(string Value);

    private sealed class EchoQueryHandler : IRequestHandler<EchoQuery, EchoResponse> {
        public Task<EchoResponse> Handle(EchoQuery request, CancellationToken cancellationToken) {
            BehaviorLog.Entries.Add("handler");
            return Task.FromResult(new EchoResponse($"handled:{request.Value}"));
        }
    }

    private sealed record UnitCommand : IRequest;

    private sealed class UnitCommandHandler : IRequestHandler<UnitCommand, Unit> {
        public static bool Handled { get; set; }

        public Task<Unit> Handle(UnitCommand request, CancellationToken cancellationToken) {
            Handled = true;
            return Task.FromResult(Unit.Value);
        }
    }

    private sealed record CapturingTokenQuery : IRequest<Unit>;

    private sealed class CapturingTokenHandler : IRequestHandler<CapturingTokenQuery, Unit> {
        public static CancellationToken CapturedToken { get; private set; }

        public Task<Unit> Handle(CapturingTokenQuery request, CancellationToken cancellationToken) {
            CapturedToken = cancellationToken;
            return Task.FromResult(Unit.Value);
        }
    }

    private sealed record CommandRequest(string Value) : ICommandRequest<EchoResponse>;

    private interface ICommandRequest<out TResponse> : IRequest<TResponse>;

    private sealed class CommandRequestHandler : IRequestHandler<CommandRequest, EchoResponse> {
        public Task<EchoResponse> Handle(CommandRequest request, CancellationToken cancellationToken) {
            BehaviorLog.Entries.Add("command-handler");
            return Task.FromResult(new EchoResponse($"command:{request.Value}"));
        }
    }

    private sealed class OuterBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull {
        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken) {
            BehaviorLog.Entries.Add("outer-before");
            var response = await next(cancellationToken);
            BehaviorLog.Entries.Add("outer-after");
            return response;
        }
    }

    private sealed class InnerBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull {
        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken) {
            BehaviorLog.Entries.Add("inner-before");
            var response = await next(cancellationToken);
            BehaviorLog.Entries.Add("inner-after");
            return response;
        }
    }

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

    private sealed class CommandOnlyBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : ICommandRequest<TResponse> {
        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken) {
            BehaviorLog.Entries.Add("command-behavior-before");
            var response = await next(cancellationToken);
            BehaviorLog.Entries.Add("command-behavior-after");
            return response;
        }
    }

    private sealed record SampleNotification(string Value) : INotification;

    private sealed class FirstNotificationHandler : INotificationHandler<SampleNotification> {
        public Task Handle(SampleNotification notification, CancellationToken cancellationToken) {
            NotificationLog.Entries.Add($"first:{notification.Value}");
            return Task.CompletedTask;
        }
    }

    private sealed class SecondNotificationHandler : INotificationHandler<SampleNotification> {
        public Task Handle(SampleNotification notification, CancellationToken cancellationToken) {
            NotificationLog.Entries.Add($"second:{notification.Value}");
            return Task.CompletedTask;
        }
    }

    private sealed record SampleStreamRequest : IStreamRequest<string>;

    private static class BehaviorLog {
        public static List<string> Entries { get; } = [];
    }

    private static class NotificationLog {
        public static List<string> Entries { get; } = [];
    }
}
