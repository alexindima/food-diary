using System.Net;
using System.Net.Sockets;
using FoodDiary.MailInbox.Application.Messages.Models;
using FoodDiary.MailInbox.Domain.Messages;
using FoodDiary.MailInbox.Infrastructure.Services;
using Npgsql;

namespace FoodDiary.MailInbox.Infrastructure.Tests;

[ExcludeFromCodeCoverage]
public sealed class MailInboxCoverageGapTests {
    [Fact]
    public async Task LocalTlsHealthCheck_WhenServerNameIsBlank_ReturnsFalse() {
        Assert.False(await MailInboxLocalTlsHealthCheck.IsReadyAsync(" ", CancellationToken.None));
    }

    [Fact]
    public void StoredMessageLimits_ThrowIfInvalid_WhenMessageIsNull_Throws() {
        Assert.Throws<ArgumentNullException>(() => MailInboxStoredMessageLimits.ThrowIfInvalid(null!));
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("postgres", false)]
    [InlineData("mailinbox_runtime", true)]
    public void RuntimeRoleProvisioner_HasValidRoleName_ReturnsExpectedResult(string? roleName, bool expected) {
        Assert.Equal(expected, NpgsqlMailInboxRuntimeRoleProvisioner.HasValidRoleName(roleName));
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("short", false)]
    [InlineData("01234567890123456789012345678901", true)]
    public void RuntimeRoleProvisioner_HasValidPassword_ReturnsExpectedResult(string? password, bool expected) {
        Assert.Equal(expected, NpgsqlMailInboxRuntimeRoleProvisioner.HasValidPassword(password));
    }

    [Fact]
    public async Task RuntimeRoleProvisioner_WhenRoleNameIsInvalid_ThrowsBeforeConnecting() {
        await using var dataSource = NpgsqlDataSource.Create(
            "Host=localhost;Database=fooddiary_mailinbox;Username=test;Password=test");
        var provisioner = new NpgsqlMailInboxRuntimeRoleProvisioner(dataSource);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            provisioner.ProvisionAsync("postgres", new string('p', 32), CancellationToken.None));
    }

    [Fact]
    public async Task RuntimeRoleProvisioner_WhenPasswordIsInvalid_ThrowsBeforeConnecting() {
        await using var dataSource = NpgsqlDataSource.Create(
            "Host=localhost;Database=fooddiary_mailinbox;Username=test;Password=test");
        var provisioner = new NpgsqlMailInboxRuntimeRoleProvisioner(dataSource);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            provisioner.ProvisionAsync("mailinbox_runtime", "short", CancellationToken.None));
    }

    [Fact]
    public async Task RuntimeRoleValidator_WhenRoleNameIsInvalid_ThrowsBeforeConnecting() {
        await using var dataSource = NpgsqlDataSource.Create(
            "Host=localhost;Database=fooddiary_mailinbox;Username=test;Password=test");
        var validator = new NpgsqlMailInboxRuntimeRoleValidator(dataSource);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            validator.ValidateAsync("postgres", CancellationToken.None));
    }

    [Fact]
    public void StoredMessageLimits_ThrowIfInvalid_WhenMetadataIsWithinLimits_DoesNotThrow() {
        InboundMailMessage message = CreateMessage(subject: "subject");

        MailInboxStoredMessageLimits.ThrowIfInvalid(message);
        MailInboxStoredMessageLimits.ThrowIfInvalid(message, InboundMailAdmission.Untrusted);
    }

    [Fact]
    public void StoredMessageLimits_ThrowIfInvalid_WhenMetadataExceedsLimits_Throws() {
        InboundMailMessage message = CreateMessage(
            subject: new string('s', MailInboxStoredMessageLimits.MaxSubjectCharacters + 1));

        Assert.Throws<ArgumentException>(() => MailInboxStoredMessageLimits.ThrowIfInvalid(message));
        Assert.Throws<ArgumentException>(() =>
            MailInboxStoredMessageLimits.ThrowIfInvalid(message, InboundMailAdmission.Untrusted));
    }

    [Fact]
    public async Task LocalTlsHealthCheck_WhenLoopbackPortIsUnavailable_ReturnsFalse() {
        bool result = await MailInboxLocalTlsHealthCheck.IsReadyAsync("localhost", CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task LocalTlsHealthCheck_WhenLoopbackAcceptsConnectionButTlsFails_ReturnsFalse() {
        var listener = new TcpListener(IPAddress.Loopback, 5098);
        listener.Start();
        try {
            Task<TcpClient> acceptTask = listener.AcceptTcpClientAsync(CancellationToken.None).AsTask();
            Task<bool> readinessTask = MailInboxLocalTlsHealthCheck.IsReadyAsync("localhost", CancellationToken.None);
            using TcpClient accepted = await acceptTask.WaitAsync(TimeSpan.FromSeconds(5));
            accepted.Close();

            Assert.False(await readinessTask.WaitAsync(TimeSpan.FromSeconds(5)));
        } finally {
            listener.Stop();
        }
    }

    [Fact]
    public async Task LocalTlsHealthCheck_WhenLoopbackDoesNotRespond_TimesOutAndReturnsFalse() {
        var listener = new TcpListener(IPAddress.Loopback, 5098);
        listener.Start();
        try {
            Task<TcpClient> acceptTask = listener.AcceptTcpClientAsync(CancellationToken.None).AsTask();
            Task<bool> readinessTask = MailInboxLocalTlsHealthCheck.IsReadyAsync("localhost", CancellationToken.None);
            using TcpClient accepted = await acceptTask.WaitAsync(TimeSpan.FromSeconds(5));

            Assert.False(await readinessTask.WaitAsync(TimeSpan.FromSeconds(6)));
        } finally {
            listener.Stop();
        }
    }

    private static InboundMailMessage CreateMessage(string subject) => InboundMailMessage.Receive(
        "message-id",
        "sender@example.com",
        ["recipient@example.com"],
        subject,
        "text",
        "<p>text</p>",
        "raw",
        DateTimeOffset.UtcNow);
}
