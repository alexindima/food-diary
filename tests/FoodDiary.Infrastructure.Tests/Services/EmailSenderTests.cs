using System.Diagnostics.Metrics;
using System.Net.Mail;
using FoodDiary.Application.Authentication.Common;
using FoodDiary.Application.Authentication.Services;
using FoodDiary.Application.Email.Common;
using FoodDiary.Domain.Entities.Users;

namespace FoodDiary.Infrastructure.Tests.Services;

public sealed class EmailSenderTests {
    private const string EmailMeterName = "FoodDiary.Application.Email";

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

    [Fact]
    public async Task SendPasswordResetAsync_WithAllowedClientOrigin_UsesThatOriginInLink() {
        var transport = new RecordingEmailTransport();
        var sender = CreateSender(
            transport,
            new EmailOptions {
                FromAddress = "noreply@example.com",
                FromName = "FoodDiary",
                FrontendBaseUrl = "https://fooddiary.club",
                AllowedFrontendBaseUrls = ["https://fooddiary.club", "https://дневникеды.рф"],
                VerificationPath = "/verify-email",
                PasswordResetPath = "/reset-password"
            });

        await sender.SendPasswordResetAsync(
            new PasswordResetMessage("user@example.com", User.Create("user@example.com", "hash").Id.Value.ToString(), "token", "ru", "https://xn--b1adbcbrouc8l.xn--p1ai"),
            CancellationToken.None);

        Assert.Contains("https://дневникеды.рф/reset-password?", transport.Body);
    }

    [Fact]
    public async Task SendEmailVerificationAsync_WithUntrustedClientOrigin_FallsBackToDefaultFrontendBaseUrl() {
        var transport = new RecordingEmailTransport();
        var sender = CreateSender(
            transport,
            new EmailOptions {
                FromAddress = "noreply@example.com",
                FromName = "FoodDiary",
                FrontendBaseUrl = "https://fooddiary.club",
                AllowedFrontendBaseUrls = ["https://fooddiary.club", "https://дневникеды.рф"],
                VerificationPath = "/verify-email",
                PasswordResetPath = "/reset-password"
            });

        await sender.SendEmailVerificationAsync(
            new EmailVerificationMessage("user@example.com", User.Create("user@example.com", "hash").Id.Value.ToString(), "token", "en", "https://evil.example.com"),
            CancellationToken.None);

        Assert.Contains("https://fooddiary.club/verify-email?", transport.Body);
        Assert.DoesNotContain("evil.example.com", transport.Body);
    }

    private static EmailSender CreateSender(IEmailTransport transport, EmailOptions? options = null) {
        return new EmailSender(
            options ?? new EmailOptions {
                FromAddress = "noreply@example.com",
                FromName = "FoodDiary",
                FrontendBaseUrl = "https://app.example.com",
                VerificationPath = "/verify-email",
                PasswordResetPath = "/reset-password"
            },
            new StubEmailTemplateProvider(),
            transport);
    }

    private static MeterListener CreateInfrastructureListener(
        Action<long, ReadOnlySpan<KeyValuePair<string, object?>>> onDispatch) {
        var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) => {
            if (instrument.Meter.Name != EmailMeterName) {
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
        public string Body { get; private set; } = string.Empty;

        public Task SendAsync(MailMessage message, CancellationToken cancellationToken) {
            Body = message.Body;
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingEmailTransport(Exception exception) : IEmailTransport {
        public Task SendAsync(MailMessage message, CancellationToken cancellationToken) => Task.FromException(exception);
    }
}
