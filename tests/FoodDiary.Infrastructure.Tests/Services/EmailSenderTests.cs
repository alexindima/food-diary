using System.Diagnostics.Metrics;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Authentication.Services;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Application.Abstractions.Email.Common;

namespace FoodDiary.Infrastructure.Tests.Services;

[ExcludeFromCodeCoverage]
public sealed class EmailSenderTests {
    private const string EmailMeterName = "FoodDiary.Application.Email";

    [Fact]
    public async Task SendEmailVerificationAsync_WhenTransportSucceeds_RecordsSuccessMetric() {
        long? count = null;
        string? outcome = null;
        string? template = null;
        using MeterListener listener = CreateInfrastructureListener((value, tags) => {
            count = value;
            outcome = GetTagValue(tags, "fooddiary.email.outcome");
            template = GetTagValue(tags, "fooddiary.email.template");
        });

        EmailSender sender = CreateSender(CreateSuccessfulTransport());

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
        using MeterListener listener = CreateInfrastructureListener((value, tags) => {
            count = value;
            outcome = GetTagValue(tags, "fooddiary.email.outcome");
            errorType = GetTagValue(tags, "error.type");
        });

        EmailSender sender = CreateSender(CreateThrowingTransport(new InvalidOperationException("boom")));

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
        IEmailTransport transport = CreateCapturingTransport(out Func<string> getBody);
        EmailSender sender = CreateSender(
            transport,
            new EmailOptions {
                FromAddress = "noreply@example.com",
                FromName = "FoodDiary",
                FrontendBaseUrl = "https://fooddiary.club",
                AllowedFrontendBaseUrls = ["https://fooddiary.club", "https://дневникеды.рф"],
                VerificationPath = "/verify-email",
                PasswordResetPath = "/reset-password",
            });

        await sender.SendPasswordResetAsync(
            new PasswordResetMessage("user@example.com", User.Create("user@example.com", "hash").Id.Value.ToString(), "token", "ru", "https://xn--b1adbcbrouc8l.xn--p1ai"),
            CancellationToken.None);

        Assert.Contains("https://дневникеды.рф/reset-password?", getBody(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task SendEmailVerificationAsync_WithUntrustedClientOrigin_FallsBackToDefaultFrontendBaseUrl() {
        IEmailTransport transport = CreateCapturingTransport(out Func<string> getBody);
        EmailSender sender = CreateSender(
            transport,
            new EmailOptions {
                FromAddress = "noreply@example.com",
                FromName = "FoodDiary",
                FrontendBaseUrl = "https://fooddiary.club",
                AllowedFrontendBaseUrls = ["https://fooddiary.club", "https://Ð´Ð½ÐµÐ²Ð½Ð¸ÐºÐµÐ´Ñ‹.Ñ€Ñ„"],
                VerificationPath = "/verify-email",
                PasswordResetPath = "/reset-password",
            });

        await sender.SendEmailVerificationAsync(
            new EmailVerificationMessage("user@example.com", User.Create("user@example.com", "hash").Id.Value.ToString(), "token", "en", "https://evil.example.com"),
            CancellationToken.None);

        Assert.Contains("https://fooddiary.club/verify-email?", getBody(), StringComparison.Ordinal);
        Assert.DoesNotContain("evil.example.com", getBody(), StringComparison.Ordinal);
    }

    private static EmailSender CreateSender(IEmailTransport transport, EmailOptions? options = null) {
        return new EmailSender(
            options ?? new EmailOptions {
                FromAddress = "noreply@example.com",
                FromName = "FoodDiary",
                FrontendBaseUrl = "https://app.example.com",
                VerificationPath = "/verify-email",
                PasswordResetPath = "/reset-password",
            },
            CreateTemplateProvider(),
            transport);
    }

    private static IEmailTemplateProvider CreateTemplateProvider() {
        IEmailTemplateProvider templateProvider = Substitute.For<IEmailTemplateProvider>();
        templateProvider
            .GetActiveTemplateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<EmailTemplateContent?>(null));
        return templateProvider;
    }

    private static IEmailTransport CreateSuccessfulTransport() {
        IEmailTransport transport = Substitute.For<IEmailTransport>();
        transport
            .SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        return transport;
    }

    private static IEmailTransport CreateCapturingTransport(out Func<string> getBody) {
        IEmailTransport transport = Substitute.For<IEmailTransport>();
        string body = string.Empty;
        transport
            .SendAsync(Arg.Do<EmailMessage>(message => body = message.HtmlBody), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        getBody = () => body;
        return transport;
    }

    private static IEmailTransport CreateThrowingTransport(Exception exception) {
        IEmailTransport transport = Substitute.For<IEmailTransport>();
        transport
            .SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(exception));
        return transport;
    }

    private static MeterListener CreateInfrastructureListener(
        Action<long, ReadOnlySpan<KeyValuePair<string, object?>>> onDispatch) {
        var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) => {
            if (!string.Equals(instrument.Meter.Name, EmailMeterName, StringComparison.Ordinal)) {
                return;
            }

            if (string.Equals(instrument.Name, "fooddiary.email.dispatch.events", StringComparison.Ordinal)) {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) => {
            if (string.Equals(instrument.Name, "fooddiary.email.dispatch.events", StringComparison.Ordinal)) {
                onDispatch(value, tags);
            }
        });
        listener.Start();
        return listener;
    }

    private static string? GetTagValue(ReadOnlySpan<KeyValuePair<string, object?>> tags, string key) {
        foreach (KeyValuePair<string, object?> tag in tags) {
            if (string.Equals(tag.Key, key, StringComparison.Ordinal)) {
                return tag.Value?.ToString();
            }
        }

        return null;
    }

}
