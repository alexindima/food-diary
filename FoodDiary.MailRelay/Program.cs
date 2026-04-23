using FoodDiary.MailRelay.Options;
using FoodDiary.MailRelay.Services;
using Npgsql;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<MailRelayOptions>()
    .Bind(builder.Configuration.GetSection(MailRelayOptions.SectionName))
    .Validate(MailRelayOptions.HasValidListenApiKey, "MailRelay:ApiKey must be provided when RequireApiKey is enabled.")
    .ValidateOnStart();
builder.Services.AddOptions<MailRelaySmtpOptions>()
    .Bind(builder.Configuration.GetSection(MailRelaySmtpOptions.SectionName))
    .Validate(static options => options.Port > 0, "RelaySmtp:Port must be greater than zero.")
    .ValidateOnStart();
builder.Services.AddOptions<MailRelayDkimOptions>()
    .Bind(builder.Configuration.GetSection(MailRelayDkimOptions.SectionName))
    .Validate(MailRelayDkimOptions.HasValidConfiguration,
        "MailRelayDkim requires Domain, Selector, and exactly one of PrivateKeyPem or PrivateKeyPath when enabled.")
    .ValidateOnStart();
builder.Services.AddOptions<MailRelayQueueOptions>()
    .Bind(builder.Configuration.GetSection(MailRelayQueueOptions.SectionName))
    .Validate(MailRelayQueueOptions.HasValidConfiguration,
        "MailRelayQueue configuration requires positive poll interval, batch size, retry delays, and lock timeout.")
    .ValidateOnStart();
builder.Services.AddOptions<MailRelayBrokerOptions>()
    .Bind(builder.Configuration.GetSection(MailRelayBrokerOptions.SectionName))
    .Validate(MailRelayBrokerOptions.HasSupportedBackend,
        "MailRelayBroker:Backend must be either PostgresPolling or RabbitMq.")
    .Validate(MailRelayBrokerOptions.HasValidConfiguration,
        "MailRelayBroker configuration is invalid.")
    .ValidateOnStart();
builder.Services.AddOptions<OpenTelemetryOptions>()
    .Bind(builder.Configuration.GetSection(OpenTelemetryOptions.SectionName))
    .Validate(OpenTelemetryOptions.HasValidOtlpEndpoint,
        "OpenTelemetry:Otlp:Endpoint must be a valid absolute URI when provided.")
    .ValidateOnStart();
builder.Services.AddSingleton(sp => {
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                           ?? throw new InvalidOperationException("DefaultConnection is not configured.");
    return new NpgsqlDataSourceBuilder(connectionString).Build();
});
builder.Services.AddSingleton<MailRelayQueueStore>();
builder.Services.AddSingleton<MailRelayDeliveryEventIngestionService>();
builder.Services.AddSingleton<DkimSigningService>();
builder.Services.AddSingleton<IRelayDeliveryTransport, SmtpRelayDeliveryTransport>();
builder.Services.AddSingleton<SmtpSubmissionService>();
builder.Services.AddSingleton<MailRelayMessageProcessor>();
builder.Services.AddSingleton<RabbitMqMailRelayBroker>();
builder.Services.AddSingleton<IMailRelayDispatchNotifier, RabbitMqMailRelayDispatchNotifier>();
builder.Services.AddHostedService<MailRelaySchemaInitializerHostedService>();
builder.Services.AddHostedService<RabbitMqMailRelayBootstrapHostedService>();
builder.Services.AddHostedService<MailRelayOutboxPublisherHostedService>();
builder.Services.AddHostedService<RabbitMqMailRelayConsumerHostedService>();
builder.Services.AddHostedService<MailRelayQueueProcessorHostedService>();
builder.Services.AddSingleton<MeterProvider>(static serviceProvider => {
    var options = serviceProvider.GetRequiredService<IOptions<OpenTelemetryOptions>>().Value;
    if (string.IsNullOrWhiteSpace(options.Otlp.Endpoint)) {
        return null!;
    }

    var endpointUri = new Uri(options.Otlp.Endpoint, UriKind.Absolute);

    return Sdk.CreateMeterProviderBuilder()
        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("FoodDiary.MailRelay"))
        .AddMeter(MailRelayTelemetry.MeterName)
        .AddOtlpExporter(exporterOptions => exporterOptions.Endpoint = endpointUri)
        .Build();
});

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapGet("/health/ready", async (NpgsqlDataSource dataSource, CancellationToken cancellationToken) => {
    await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
    await using var command = new NpgsqlCommand("select 1", connection);
    await command.ExecuteScalarAsync(cancellationToken);
    return Results.Ok(new { status = "ready" });
});
app.MapGet("/api/email/queue/stats", async (
    HttpContext httpContext,
    IOptions<MailRelayOptions> relayOptions,
    MailRelayQueueStore queueStore,
    CancellationToken cancellationToken) => {
        if (!RelayRequestAuthorizer.IsAuthorized(httpContext.Request, relayOptions.Value)) {
            return Results.Unauthorized();
        }

        var stats = await queueStore.GetStatsAsync(cancellationToken);
        return Results.Ok(stats);
    });
app.MapGet("/api/email/suppressions", async (
    string? email,
    HttpContext httpContext,
    IOptions<MailRelayOptions> relayOptions,
    MailRelayQueueStore queueStore,
    CancellationToken cancellationToken) => {
        if (!RelayRequestAuthorizer.IsAuthorized(httpContext.Request, relayOptions.Value)) {
            return Results.Unauthorized();
        }

        var suppressions = await queueStore.GetSuppressionsAsync(email, cancellationToken);
        return Results.Ok(suppressions);
    });
app.MapGet("/api/email/events", async (
    string? email,
    HttpContext httpContext,
    IOptions<MailRelayOptions> relayOptions,
    MailRelayQueueStore queueStore,
    CancellationToken cancellationToken) => {
        if (!RelayRequestAuthorizer.IsAuthorized(httpContext.Request, relayOptions.Value)) {
            return Results.Unauthorized();
        }

        var events = await queueStore.GetDeliveryEventsAsync(email, cancellationToken);
        return Results.Ok(events);
    });
app.MapPost("/api/email/suppressions", async (
    CreateSuppressionRequest request,
    HttpContext httpContext,
    IOptions<MailRelayOptions> relayOptions,
    MailRelayQueueStore queueStore,
    CancellationToken cancellationToken) => {
        if (!RelayRequestAuthorizer.IsAuthorized(httpContext.Request, relayOptions.Value)) {
            return Results.Unauthorized();
        }

        await queueStore.UpsertSuppressionAsync(request, cancellationToken);
        return Results.Created($"/api/email/suppressions?email={Uri.EscapeDataString(request.Email)}", new { status = "suppressed" });
    });
app.MapPost("/api/email/events", async (
    IngestMailEventRequest request,
    HttpContext httpContext,
    IOptions<MailRelayOptions> relayOptions,
    MailRelayDeliveryEventIngestionService ingestionService,
    CancellationToken cancellationToken) => {
        if (!RelayRequestAuthorizer.IsAuthorized(httpContext.Request, relayOptions.Value)) {
            return Results.Unauthorized();
        }

        try {
            var deliveryEvent = await ingestionService.IngestAsync(request, cancellationToken);
            return Results.Created($"/api/email/events?email={Uri.EscapeDataString(request.Email)}", deliveryEvent);
        } catch (InvalidOperationException ex) {
            return Results.BadRequest(new { error = ex.Message });
        }
    });
app.MapPost("/api/email/providers/aws-ses/sns", async (
    AwsSesSnsWebhookRequest request,
    HttpContext httpContext,
    IOptions<MailRelayOptions> relayOptions,
    MailRelayDeliveryEventIngestionService ingestionService,
    CancellationToken cancellationToken) => {
        if (!RelayRequestAuthorizer.IsAuthorized(httpContext.Request, relayOptions.Value)) {
            return Results.Unauthorized();
        }

        if (!AwsSesSnsEventMapper.TryMap(request, out var events, out var error)) {
            return Results.BadRequest(new { error });
        }

        var createdEvents = await ingestionService.IngestManyAsync(events, cancellationToken);
        return Results.Created("/api/email/providers/aws-ses/sns", new { accepted = createdEvents.Count });
    });
app.MapPost("/api/email/providers/mailgun/events", async (
    MailgunWebhookRequest request,
    HttpContext httpContext,
    IOptions<MailRelayOptions> relayOptions,
    MailRelayDeliveryEventIngestionService ingestionService,
    CancellationToken cancellationToken) => {
        if (!RelayRequestAuthorizer.IsAuthorized(httpContext.Request, relayOptions.Value)) {
            return Results.Unauthorized();
        }

        if (!MailgunEventMapper.TryMap(request, out var deliveryEvent, out var error) || deliveryEvent is null) {
            return Results.BadRequest(new { error });
        }

        var createdEvent = await ingestionService.IngestAsync(deliveryEvent, cancellationToken);
        return Results.Created("/api/email/providers/mailgun/events", createdEvent);
    });
app.MapDelete("/api/email/suppressions/{email}", async (
    string email,
    HttpContext httpContext,
    IOptions<MailRelayOptions> relayOptions,
    MailRelayQueueStore queueStore,
    CancellationToken cancellationToken) => {
        if (!RelayRequestAuthorizer.IsAuthorized(httpContext.Request, relayOptions.Value)) {
            return Results.Unauthorized();
        }

        var removed = await queueStore.RemoveSuppressionAsync(email, cancellationToken);
        return removed ? Results.NoContent() : Results.NotFound();
    });
app.MapGet("/api/email/messages/{id:guid}", async (
    Guid id,
    HttpContext httpContext,
    IOptions<MailRelayOptions> relayOptions,
    MailRelayQueueStore queueStore,
    CancellationToken cancellationToken) => {
        if (!RelayRequestAuthorizer.IsAuthorized(httpContext.Request, relayOptions.Value)) {
            return Results.Unauthorized();
        }

        var message = await queueStore.GetMessageDetailsAsync(id, cancellationToken);
        return message is null ? Results.NotFound() : Results.Ok(message);
    });

app.MapPost("/api/email/send", async (
    RelayEmailMessageRequest request,
    HttpContext httpContext,
    IOptions<MailRelayOptions> relayOptions,
    MailRelayQueueStore queueStore,
    CancellationToken cancellationToken) => {
        if (!RelayRequestAuthorizer.IsAuthorized(httpContext.Request, relayOptions.Value)) {
            return Results.Unauthorized();
        }

        var queuedEmailId = await queueStore.EnqueueAsync(request, cancellationToken);
        var dispatchNotifier = httpContext.RequestServices.GetRequiredService<IMailRelayDispatchNotifier>();
        await dispatchNotifier.NotifyQueuedAsync(queuedEmailId, cancellationToken);
        return Results.Accepted($"/api/email/messages/{queuedEmailId}", new { id = queuedEmailId, status = "queued" });
    });

app.Run();

public partial class Program;
