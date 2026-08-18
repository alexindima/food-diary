using System.Globalization;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Mediator.Tests;

[ExcludeFromCodeCoverage]
public sealed class MediatorTests {
    [Fact]
    public async Task Send_WithTypedRequest_InvokesMatchingHandler() {
        await using ServiceProvider provider = CreateProvider(configuration => configuration.RegisterServicesFromAssembly(typeof(MediatorTests).Assembly));
        ISender sender = provider.GetRequiredService<ISender>();

        EchoResponse response = await sender.Send(new EchoQuery("value"));

        Assert.Equal("handled:value", response.Value);
    }

    [Fact]
    public async Task Send_WithObjectRequest_ReturnsHandlerResponse() {
        await using ServiceProvider provider = CreateProvider(configuration => configuration.RegisterServicesFromAssembly(typeof(MediatorTests).Assembly));
        ISender sender = provider.GetRequiredService<ISender>();

        object? response = await sender.Send((object)new EchoQuery("object-value"));

        EchoResponse echoResponse = Assert.IsType<EchoResponse>(response);
        Assert.Equal("handled:object-value", echoResponse.Value);
    }

    [Fact]
    public async Task Mediator_WithObjectRequestAndNotification_UsesCombinedInterface() {
        await using ServiceProvider provider = CreateProvider(configuration => configuration.RegisterServicesFromAssembly(typeof(MediatorTests).Assembly));
        IMediator mediator = provider.GetRequiredService<IMediator>();
        NotificationLog.Entries.Clear();

        object? response = await mediator.Send((object)new EchoQuery("mediator-object"));
        await mediator.Publish((object)new SampleNotification("mediator-object"));

        EchoResponse echoResponse = Assert.IsType<EchoResponse>(response);
        Assert.Equal("handled:mediator-object", echoResponse.Value);
        Assert.Equal(["first:mediator-object", "second:mediator-object"], NotificationLog.Entries.Order(StringComparer.Ordinal), StringComparer.Ordinal);
    }

    [Fact]
    public async Task Send_WithObjectUnitRequest_InvokesHandlerAndReturnsUnit() {
        await using ServiceProvider provider = CreateProvider(configuration => configuration.RegisterServicesFromAssembly(typeof(MediatorTests).Assembly));
        ISender sender = provider.GetRequiredService<ISender>();
        UnitCommandHandler.Handled = false;

        object? response = await sender.Send((object)new UnitCommand());

        Assert.IsType<Unit>(response);
        Assert.True(UnitCommandHandler.Handled);
    }

    [Fact]
    public async Task Send_WithUnitRequest_InvokesHandlerAndReturnsTask() {
        await using ServiceProvider provider = CreateProvider(configuration => configuration.RegisterServicesFromAssembly(typeof(MediatorTests).Assembly));
        ISender sender = provider.GetRequiredService<ISender>();
        UnitCommandHandler.Handled = false;

        await sender.Send(new UnitCommand());

        Assert.True(UnitCommandHandler.Handled);
    }

    [Fact]
    public async Task Send_PassesCancellationTokenToHandler() {
        await using ServiceProvider provider = CreateProvider(configuration => configuration.RegisterServicesFromAssembly(typeof(MediatorTests).Assembly));
        ISender sender = provider.GetRequiredService<ISender>();
        using var cancellationTokenSource = new CancellationTokenSource();

        await sender.Send(new CapturingTokenQuery(), cancellationTokenSource.Token);

        Assert.Equal(cancellationTokenSource.Token, CapturingTokenHandler.CapturedToken);
    }

    [Fact]
    public async Task Send_AppliesOpenBehaviorsInRegistrationOrder() {
        await using ServiceProvider provider = CreateProvider(configuration => {
            configuration.RegisterServicesFromAssembly(typeof(MediatorTests).Assembly);
            configuration.AddOpenBehavior(typeof(OuterBehavior<,>));
            configuration.AddOpenBehavior(typeof(InnerBehavior<,>));
        });
        ISender sender = provider.GetRequiredService<ISender>();
        BehaviorLog.Entries.Clear();

        EchoResponse response = await sender.Send(new EchoQuery("pipeline"));

        Assert.Equal("handled:pipeline", response.Value);
        Assert.Equal(
            [
                "outer-before",
                "inner-before",
                "handler",
                "inner-after",
                "outer-after",
            ],
            BehaviorLog.Entries);
    }

    [Fact]
    public async Task Send_WhenBehaviorShortCircuits_DoesNotInvokeHandler() {
        await using ServiceProvider provider = CreateProvider(configuration => {
            configuration.RegisterServicesFromAssembly(typeof(MediatorTests).Assembly);
            configuration.AddOpenBehavior(typeof(ShortCircuitBehavior<,>));
        });
        ISender sender = provider.GetRequiredService<ISender>();
        BehaviorLog.Entries.Clear();

        EchoResponse response = await sender.Send(new EchoQuery("ignored"));

        Assert.Equal("short-circuited", response.Value);
        Assert.Equal(["short-circuit"], BehaviorLog.Entries);
    }

    [Fact]
    public async Task Send_WhenOpenBehaviorConstraintDoesNotMatch_SkipsBehavior() {
        await using ServiceProvider provider = CreateProvider(configuration => {
            configuration.RegisterServicesFromAssembly(typeof(MediatorTests).Assembly);
            configuration.AddOpenBehavior(typeof(CommandOnlyBehavior<,>));
        });
        ISender sender = provider.GetRequiredService<ISender>();
        BehaviorLog.Entries.Clear();

        EchoResponse response = await sender.Send(new EchoQuery("query"));

        Assert.Equal("handled:query", response.Value);
        Assert.Equal(["handler"], BehaviorLog.Entries);
    }

    [Fact]
    public async Task Send_WhenOpenBehaviorConstraintMatches_AppliesBehavior() {
        await using ServiceProvider provider = CreateProvider(configuration => {
            configuration.RegisterServicesFromAssembly(typeof(MediatorTests).Assembly);
            configuration.AddOpenBehavior(typeof(CommandOnlyBehavior<,>));
        });
        ISender sender = provider.GetRequiredService<ISender>();
        BehaviorLog.Entries.Clear();

        EchoResponse response = await sender.Send(new CommandRequest("command"));

        Assert.Equal("command:command", response.Value);
        Assert.Equal(["command-behavior-before", "command-handler", "command-behavior-after"], BehaviorLog.Entries);
    }

    [Fact]
    public void AddOpenBehavior_WithClosedBehaviorType_ThrowsArgumentException() {
        var configuration = new MediatorServiceConfiguration();

        ArgumentException ex = Assert.Throws<ArgumentException>(() => configuration.AddOpenBehavior(typeof(ClosedBehavior)));

        Assert.Contains("Behavior type must be an open generic type definition", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Publish_WithTypedNotification_InvokesAllHandlers() {
        await using ServiceProvider provider = CreateProvider(configuration => configuration.RegisterServicesFromAssembly(typeof(MediatorTests).Assembly));
        IPublisher publisher = provider.GetRequiredService<IPublisher>();
        NotificationLog.Entries.Clear();

        await publisher.Publish(new SampleNotification("typed"));

        Assert.Equal(["first:typed", "second:typed"], NotificationLog.Entries.Order(StringComparer.Ordinal), StringComparer.Ordinal);
    }

    [Fact]
    public async Task Publish_WithObjectNotification_InvokesAllHandlers() {
        await using ServiceProvider provider = CreateProvider(configuration => configuration.RegisterServicesFromAssembly(typeof(MediatorTests).Assembly));
        IPublisher publisher = provider.GetRequiredService<IPublisher>();
        NotificationLog.Entries.Clear();

        await publisher.Publish((object)new SampleNotification("object"));

        Assert.Equal(["first:object", "second:object"], NotificationLog.Entries.Order(StringComparer.Ordinal), StringComparer.Ordinal);
    }

    [Fact]
    public async Task Publish_WithTypedNotification_ExecutesHandlersSequentiallyInRegistrationOrder() {
        await using ServiceProvider provider = CreateSequentialNotificationProvider();
        IPublisher publisher = provider.GetRequiredService<IPublisher>();
        NotificationExecutionProbe probe = provider.GetRequiredService<NotificationExecutionProbe>();
        using var cancellationTokenSource = new CancellationTokenSource();

        await publisher.Publish(new SequentialNotification(), cancellationTokenSource.Token);

        Assert.Equal(1, probe.MaxConcurrency);
        Assert.Equal(["first:start", "first:end", "second:start", "second:end"], probe.Entries);
        Assert.All(probe.CapturedTokens, token => Assert.Equal(cancellationTokenSource.Token, token));
    }

    [Fact]
    public async Task Publish_WithObjectNotification_ExecutesHandlersSequentiallyInRegistrationOrder() {
        await using ServiceProvider provider = CreateSequentialNotificationProvider();
        IPublisher publisher = provider.GetRequiredService<IPublisher>();
        NotificationExecutionProbe probe = provider.GetRequiredService<NotificationExecutionProbe>();

        await publisher.Publish((object)new SequentialNotification());

        Assert.Equal(1, probe.MaxConcurrency);
        Assert.Equal(["first:start", "first:end", "second:start", "second:end"], probe.Entries);
    }

    [Fact]
    public async Task Publish_WhenHandlerFails_PropagatesAndDoesNotStartLaterHandlers() {
        await using ServiceProvider provider = CreateFailingNotificationProvider();
        IPublisher publisher = provider.GetRequiredService<IPublisher>();
        NotificationFailureProbe probe = provider.GetRequiredService<NotificationFailureProbe>();

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => publisher.Publish(new FailingNotification()));

        Assert.Equal("notification failed", exception.Message);
        Assert.Equal(["first", "failing"], probe.Entries);
    }

    [Fact]
    public async Task Publish_WhenNoHandlersRegistered_Completes() {
        await using ServiceProvider provider = CreateProvider(static _ => { });
        IPublisher publisher = provider.GetRequiredService<IPublisher>();

        await publisher.Publish(new UnhandledNotification());
        await publisher.Publish((object)new UnhandledNotification());
    }

    [Fact]
    public async Task Publish_WithNonNotificationObject_ThrowsInvalidOperationException() {
        await using ServiceProvider provider = CreateProvider(configuration => configuration.RegisterServicesFromAssembly(typeof(MediatorTests).Assembly));
        IPublisher publisher = provider.GetRequiredService<IPublisher>();

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(() => publisher.Publish(new object()));

        Assert.Contains("does not implement INotification", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Publish_WithNullTypedNotification_ThrowsArgumentNullException() {
        await using ServiceProvider provider = CreateProvider(static _ => { });
        IPublisher publisher = provider.GetRequiredService<IPublisher>();

        await Assert.ThrowsAsync<ArgumentNullException>(() => publisher.Publish<SampleNotification>(null!));
    }

    [Fact]
    public async Task Publish_WithNullObjectNotification_ThrowsArgumentNullException() {
        await using ServiceProvider provider = CreateProvider(static _ => { });
        IPublisher publisher = provider.GetRequiredService<IPublisher>();

        await Assert.ThrowsAsync<ArgumentNullException>(() => publisher.Publish(null!));
    }

    [Fact]
    public async Task Send_WhenHandlerIsMissing_ThrowsInvalidOperationException() {
        await using ServiceProvider provider = CreateProvider(static _ => { });
        ISender sender = provider.GetRequiredService<ISender>();

        await Assert.ThrowsAsync<InvalidOperationException>(() => sender.Send(new EchoQuery("missing")));
    }

    [Fact]
    public async Task Send_WithObjectThatIsNotRequest_ThrowsInvalidOperationException() {
        await using ServiceProvider provider = CreateProvider(static _ => { });
        ISender sender = provider.GetRequiredService<ISender>();

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sender.Send(new object()));

        Assert.Contains("does not implement IRequest<TResponse>", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Send_WithNullTypedRequest_ThrowsArgumentNullException() {
        await using ServiceProvider provider = CreateProvider(static _ => { });
        ISender sender = provider.GetRequiredService<ISender>();

        await Assert.ThrowsAsync<ArgumentNullException>(() => sender.Send<EchoResponse>(null!));
    }

    [Fact]
    public async Task Send_WithNullObjectRequest_ThrowsArgumentNullException() {
        await using ServiceProvider provider = CreateProvider(static _ => { });
        ISender sender = provider.GetRequiredService<ISender>();

        await Assert.ThrowsAsync<ArgumentNullException>(() => sender.Send(null!));
    }

    [Fact]
    public async Task Send_WithExplicitInterfaceHandler_SupportsTypedAndObjectDispatch() {
        await using ServiceProvider provider = CreateProvider(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(MediatorTests).Assembly));
        ISender sender = provider.GetRequiredService<ISender>();

        string typedResponse = await sender.Send(new ExplicitRequest("typed"));
        object? objectResponse = await sender.Send((object)new ExplicitRequest("object"));

        Assert.Equal("explicit:typed", typedResponse);
        Assert.Equal("explicit:object", objectResponse);
    }

    [Fact]
    public async Task Send_WithHandlerForMultipleRequestTypes_InvokesMatchingOverload() {
        await using ServiceProvider provider = CreateProvider(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(MediatorTests).Assembly));
        ISender sender = provider.GetRequiredService<ISender>();

        string firstResponse = await sender.Send(new FirstMultiRequest());
        int secondResponse = await sender.Send(new SecondMultiRequest());

        Assert.Equal("first", firstResponse);
        Assert.Equal(2, secondResponse);
    }

    [Fact]
    public async Task Send_WhenHandlerThrowsSynchronously_PropagatesOriginalException() {
        await using ServiceProvider provider = CreateProvider(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(MediatorTests).Assembly));
        ISender sender = provider.GetRequiredService<ISender>();

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sender.Send(new SynchronousFailureRequest()));

        Assert.Equal("synchronous request failure", exception.Message);
    }

    [Fact]
    public async Task Send_WithMultipleResponseContracts_ThrowsInvalidOperationException() {
        await using ServiceProvider provider = CreateProvider(static _ => { });
        ISender sender = provider.GetRequiredService<ISender>();

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sender.Send((object)new MultipleResponseRequest()));

        Assert.Contains("multiple response types", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Publish_WithExplicitInterfaceHandler_SupportsObjectDispatch() {
        await using ServiceProvider provider = CreateProvider(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(MediatorTests).Assembly));
        IPublisher publisher = provider.GetRequiredService<IPublisher>();
        ExplicitNotificationHandler.LastValue = null;

        await publisher.Publish((object)new ExplicitNotification("value"));

        Assert.Equal("value", ExplicitNotificationHandler.LastValue);
    }

    [Fact]
    public async Task PublishObject_WhenHandlerThrowsSynchronously_PropagatesOriginalException() {
        await using ServiceProvider provider = CreateProvider(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(MediatorTests).Assembly));
        IPublisher publisher = provider.GetRequiredService<IPublisher>();

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => publisher.Publish((object)new SynchronousFailureNotification()));

        Assert.Equal("synchronous notification failure", exception.Message);
    }

    [Fact]
    public async Task CreateStream_WithTypedAndObjectRequests_ReturnsHandlerResponses() {
        await using ServiceProvider provider = CreateProvider(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(MediatorTests).Assembly));
        ISender sender = provider.GetRequiredService<ISender>();
        using var cancellationTokenSource = new CancellationTokenSource();

        List<string> typedResponses = [];
        await foreach (string response in sender.CreateStream(
            new SampleStreamRequest(2),
            cancellationTokenSource.Token)) {
            typedResponses.Add(response);
        }

        List<object?> objectResponses = [];
        await foreach (object? response in sender.CreateStream(
            (object)new SampleStreamRequest(2),
            cancellationTokenSource.Token)) {
            objectResponses.Add(response);
        }

        Assert.Equal(["item-0", "item-1"], typedResponses);
        Assert.Equal(["item-0", "item-1"], objectResponses);
        Assert.Equal(cancellationTokenSource.Token, SampleStreamRequestHandler.CapturedToken);
    }

    [Fact]
    public async Task CreateStream_WithCancelledToken_PropagatesCancellation() {
        await using ServiceProvider provider = CreateProvider(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(MediatorTests).Assembly));
        ISender sender = provider.GetRequiredService<ISender>();
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => {
            await foreach (string _ in sender.CreateStream(
                new SampleStreamRequest(1),
                cancellationTokenSource.Token)) {
            }
        });
    }

    [Fact]
    public async Task CreateStream_WhenHandlerIsMissing_ThrowsInvalidOperationExceptionOnEnumeration() {
        await using ServiceProvider provider = CreateProvider(static _ => { });
        ISender sender = provider.GetRequiredService<ISender>();

        await Assert.ThrowsAsync<InvalidOperationException>(async () => {
            await foreach (string _ in sender.CreateStream(new SampleStreamRequest(1))) {
            }
        });
    }

    [Fact]
    public void CreateStream_WithInvalidOrAmbiguousObject_ThrowsInvalidOperationException() {
        using ServiceProvider provider = CreateProvider(static _ => { });
        ISender sender = provider.GetRequiredService<ISender>();

        InvalidOperationException invalidException = Assert.Throws<InvalidOperationException>(
            () => sender.CreateStream(new object()));
        InvalidOperationException ambiguousException = Assert.Throws<InvalidOperationException>(
            () => sender.CreateStream((object)new MultipleResponseStreamRequest()));

        Assert.Contains("does not implement IStreamRequest<TResponse>", invalidException.Message, StringComparison.Ordinal);
        Assert.Contains("multiple response types", ambiguousException.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CreateStream_WithNullRequests_ThrowsArgumentNullException() {
        using ServiceProvider provider = CreateProvider(static _ => { });
        ISender sender = provider.GetRequiredService<ISender>();

        Assert.Throws<ArgumentNullException>(() => sender.CreateStream<string>(null!));
        Assert.Throws<ArgumentNullException>(() => sender.CreateStream(null!));
    }

    [Fact]
    public void AddOpenBehavior_WithInvalidOpenGenericType_ThrowsArgumentException() {
        var configuration = new MediatorServiceConfiguration();

        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => configuration.AddOpenBehavior(typeof(Dictionary<,>)));

        Assert.Contains("must implement IPipelineBehavior", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddOpenBehavior_WithReorderedGenericParameters_ThrowsArgumentException() {
        var configuration = new MediatorServiceConfiguration();

        Assert.Throws<ArgumentException>(() =>
            configuration.AddOpenBehavior(typeof(ReorderedBehavior<,>)));
    }

    [Theory]
    [InlineData(typeof(AbstractBehavior<,>))]
    [InlineData(typeof(OpenBehaviorInterface<,>))]
    public void AddOpenBehavior_WithNonConcreteType_ThrowsArgumentException(Type behaviorType) {
        var configuration = new MediatorServiceConfiguration();

        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            configuration.AddOpenBehavior(behaviorType));

        Assert.Contains("must be a concrete class", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void MediatorConfiguration_WithNullArguments_ThrowsArgumentNullException() {
        var configuration = new MediatorServiceConfiguration();

        Assert.Throws<ArgumentNullException>(() => configuration.RegisterServicesFromAssembly(null!));
        Assert.Throws<ArgumentNullException>(() => configuration.AddOpenBehavior(null!));
    }

    [Fact]
    public void AddFoodDiaryMediator_WithNullArguments_ThrowsArgumentNullException() {
        IServiceCollection nullServices = null!;
        var services = new ServiceCollection();

        Assert.Throws<ArgumentNullException>(() => nullServices.AddFoodDiaryMediator(static _ => { }));
        Assert.Throws<ArgumentNullException>(() => services.AddFoodDiaryMediator(null!));
    }

    [Fact]
    public async Task AddFoodDiaryMediator_WithDuplicateConfiguration_DeduplicatesRegistrations() {
        var services = new ServiceCollection();

        for (int index = 0; index < 2; index++) {
            services.AddFoodDiaryMediator(configuration => {
                configuration.RegisterServicesFromAssembly(typeof(MediatorTests).Assembly);
                configuration.RegisterServicesFromAssembly(typeof(MediatorTests).Assembly);
                configuration.AddOpenBehavior(typeof(OuterBehavior<,>));
                configuration.AddOpenBehavior(typeof(OuterBehavior<,>));
            });
        }

        Assert.Single(services, static descriptor => descriptor.ServiceType == typeof(IMediator));
        Assert.Single(services, static descriptor => descriptor.ServiceType == typeof(ISender));
        Assert.Single(services, static descriptor => descriptor.ServiceType == typeof(IPublisher));
        Assert.Single(
            services,
            static descriptor =>
                descriptor.ServiceType == typeof(IPipelineBehavior<,>) &&
                descriptor.ImplementationType == typeof(OuterBehavior<,>));

        await using ServiceProvider provider = services.BuildServiceProvider();
        BehaviorLog.Entries.Clear();
        await provider.GetRequiredService<ISender>().Send(new EchoQuery("deduplicated"));

        Assert.Equal(["outer-before", "handler", "outer-after"], BehaviorLog.Entries);
    }

    [Fact]
    public void NotificationEnvelope_WithValue_PreservesValueAndRejectsNull() {
        var envelope = new NotificationEnvelope<string>("value");
        envelope.Deconstruct(out string deconstructedValue);
        NotificationEnvelope<string> updatedEnvelope = envelope with { Value = "updated" };

        Assert.Equal("value", envelope.Value);
        Assert.Equal("value", deconstructedValue);
        Assert.Equal("updated", updatedEnvelope.Value);
        Assert.Throws<ArgumentNullException>(() => new NotificationEnvelope<string>(null!));
        Assert.Throws<ArgumentNullException>(() => envelope with { Value = null! });
    }

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
