using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Mediator.Tests;

public sealed partial class MediatorTests {
    [Fact]
    public async Task Publish_WithTypedNotification_InvokesAllHandlers() {
        await using ServiceProvider provider = CreateProvider(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(MediatorTests).Assembly));
        IPublisher publisher = provider.GetRequiredService<IPublisher>();
        NotificationLog.Entries.Clear();

        await publisher.Publish(new SampleNotification("typed"));

        Assert.Equal(["first:typed", "second:typed"], NotificationLog.Entries);
    }

    [Fact]
    public async Task Publish_WithObjectNotification_InvokesAllHandlers() {
        await using ServiceProvider provider = CreateProvider(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(MediatorTests).Assembly));
        IPublisher publisher = provider.GetRequiredService<IPublisher>();
        NotificationLog.Entries.Clear();

        await publisher.Publish((object)new SampleNotification("object"));

        Assert.Equal(["first:object", "second:object"], NotificationLog.Entries);
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
        await using ServiceProvider provider = CreateProvider(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(MediatorTests).Assembly));
        IPublisher publisher = provider.GetRequiredService<IPublisher>();

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => publisher.Publish(new object()));

        Assert.Contains("does not implement INotification", exception.Message, StringComparison.Ordinal);
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
}
