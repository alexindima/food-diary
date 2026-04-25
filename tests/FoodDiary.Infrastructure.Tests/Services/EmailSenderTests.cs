using System.Diagnostics.Metrics;
using System.Net.Mail;
using FoodDiary.Application.Authentication.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Infrastructure.Options;
using FoodDiary.Infrastructure.Services;

namespace FoodDiary.Infrastructure.Tests.Services;

public sealed class EmailSenderTests {
    private const string InfrastructureMeterName = "FoodDiary.Infrastructure";

    [Fact]
    public async Task SendEmailVerificationAsync_WhenTransportSucceeds_RecordsSuccessMetric() {
        long? count = null;
        string? outcome = null;
        string? template = null;
        using var listener = CreateInfrastructureListener((value, tags) => {
            count = value;
            outcome = GetTagValue(tags, "fooddiary.email.outcome");
            template = GetTagValue(tags, "fooddiary.email.template");
        });

        var sender = CreateSender(new RecordingEmailTransport());

        await sender.SendEmailVerificationAsync(
            new EmailVerificationMessage("user@example.com", User.Create("user@example.com", "hash").Id.Value.ToString(), "token", "en"),
            CancellationToken.None);

        Assert.Equal(1, count);
        Assert.Equal("success", outcome);
        Assert.Equal("email_verification", template);
    }

    [Fact]
    public async Task SendPasswordResetAsync_WhenTransportFails_RecordsFailureMetric() {
        long? count = null;
        string? outcome = null;
        string? errorType = null;
        using var listener = CreateInfrastructureListener((value, tags) => {
            count = value;
            outcome = GetTagValue(tags, "fooddiary.email.outcome");
            errorType = GetTagValue(tags, "error.type");
        });

        var sender = CreateSender(new ThrowingEmailTransport(new InvalidOperationException("boom")));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sender.SendPasswordResetAsync(
                new PasswordResetMessage("user@example.com", User.Create("user@example.com", "hash").Id.Value.ToString(), "token", "ru"),
                CancellationToken.None));

        Assert.Equal(1, count);
        Assert.Equal("failure", outcome);
        Assert.Equal(nameof(InvalidOperationException), errorType);
    }

    private static EmailSender CreateSender(IEmailTransport transport) {
        return new EmailSender(
            Microsoft.Extensions.Options.Options.Create(new EmailOptions {
                FromAddress = "noreply@example.com",
                FromName = "FoodDiary",
                FrontendBaseUrl = "https://app.example.com",
                VerificationPath = "/verify-email",
                PasswordResetPath = "/reset-password"
            }),
            new StubEmailTemplateProvider(),
            transport);
    }

    private static MeterListener CreateInfrastructureListener(
        Action<long, ReadOnlySpan<KeyValuePair<string, object?>>> onDispatch) {
        var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) => {
            if (instrument.Meter.Name != InfrastructureMeterName) {
                return;
            }

            if (instrument.Name == "fooddiary.email.dispatch.events") {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) => {
            if (instrument.Name == "fooddiary.email.dispatch.events") {
                onDispatch(value, tags);
            }
        });
        listener.Start();
        return listener;
    }

    private static string? GetTagValue(ReadOnlySpan<KeyValuePair<string, object?>> tags, string key) {
        foreach (var tag in tags) {
            if (string.Equals(tag.Key, key, StringComparison.Ordinal)) {
                return tag.Value?.ToString();
            }
        }

        return null;
    }

    private sealed class StubEmailTemplateProvider : IEmailTemplateProvider {
        public Task<EmailTemplateContent?> GetActiveTemplateAsync(string key, string locale, CancellationToken cancellationToken = default) =>
            Task.FromResult<EmailTemplateContent?>(null);
    }

    private sealed class RecordingEmailTransport : IEmailTransport {
        public Task SendAsync(MailMessage message, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class ThrowingEmailTransport(Exception exception) : IEmailTransport {
        public Task SendAsync(MailMessage message, CancellationToken cancellationToken) => Task.FromException(exception);
    }
}
