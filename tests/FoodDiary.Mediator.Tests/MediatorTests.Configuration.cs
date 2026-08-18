using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Mediator.Tests;

public sealed partial class MediatorTests {
    [Fact]
    public void AddOpenBehavior_WithClosedBehaviorType_ThrowsArgumentException() {
        var configuration = new MediatorServiceConfiguration();

        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => configuration.AddOpenBehavior(typeof(ClosedBehavior)));

        Assert.Contains("Behavior type must be an open generic type definition", exception.Message, StringComparison.Ordinal);
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
    public void AddFoodDiaryMediator_WithDuplicateRequestHandlers_ThrowsInvalidOperationException() {
        var services = new ServiceCollection();
        services.AddTransient<IRequestHandler<DuplicateRequest, string>, DuplicateRequestHandler>();
        services.AddTransient<IRequestHandler<DuplicateRequest, string>, DuplicateRequestHandler>();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddFoodDiaryMediator(static _ => { }));

        Assert.Contains("multiple registrations", exception.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(DuplicateRequest), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddFoodDiaryMediator_WithDuplicateStreamHandlers_ThrowsInvalidOperationException() {
        var services = new ServiceCollection();
        services.AddTransient<IStreamRequestHandler<DuplicateStreamRequest, string>, DuplicateStreamRequestHandler>();
        services.AddTransient<IStreamRequestHandler<DuplicateStreamRequest, string>, DuplicateStreamRequestHandler>();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddFoodDiaryMediator(static _ => { }));

        Assert.Contains("multiple registrations", exception.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(DuplicateStreamRequest), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Send_WithHandlersRegisteredAfterMediator_RejectsAmbiguousRuntimeResolution() {
        var services = new ServiceCollection();
        services.AddFoodDiaryMediator(static _ => { });
        services.AddTransient<IRequestHandler<DuplicateRequest, string>, DuplicateRequestHandler>();
        services.AddTransient<IRequestHandler<DuplicateRequest, string>, DuplicateRequestHandler>();
        await using ServiceProvider provider = services.BuildServiceProvider();

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            provider.GetRequiredService<ISender>().Send(new DuplicateRequest()));

        Assert.Contains("Multiple mediator handlers", exception.Message, StringComparison.Ordinal);
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
}
