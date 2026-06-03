using System.Reflection;
using FoodDiary.MailInbox.Application.Messages.Models;
using FoodDiary.MailInbox.Infrastructure.Services;
using Npgsql;

namespace FoodDiary.MailInbox.Tests;

[ExcludeFromCodeCoverage]
public sealed class NpgsqlInboundMailStoreHelperTests {
    [Theory]
    [InlineData("dmarc@fooddiary.club", "Hello", InboundMailMessageCategories.DmarcReport)]
    [InlineData("DMARC@FOODDIARY.CLUB", "Hello", InboundMailMessageCategories.DmarcReport)]
    [InlineData("admin@fooddiary.club", "DMARC aggregate report", InboundMailMessageCategories.DmarcReport)]
    [InlineData("admin@fooddiary.club", "Report Domain: fooddiary.club", InboundMailMessageCategories.DmarcReport)]
    [InlineData("admin@fooddiary.club", "Hello", InboundMailMessageCategories.General)]
    [InlineData("admin@fooddiary.club", null, InboundMailMessageCategories.General)]
    public void GetCategory_ReturnsExpectedCategory(string recipient, string? subject, string expectedCategory) {
        var method = typeof(NpgsqlInboundMailStore).GetMethod(
            "GetCategory",
            BindingFlags.NonPublic | BindingFlags.Static);

        var category = (string)method!.Invoke(null, [new[] { recipient }, subject])!;

        Assert.Equal(expectedCategory, category);
    }

    [Fact]
    public void GetCategory_WhenAnyRecipientIsDmarc_ReturnsDmarcReport() {
        var method = typeof(NpgsqlInboundMailStore).GetMethod(
            "GetCategory",
            BindingFlags.NonPublic | BindingFlags.Static);

        var category = (string)method!.Invoke(null, [new[] { "admin@fooddiary.club", "dmarc@fooddiary.club" }, "Hello"])!;

        Assert.Equal(InboundMailMessageCategories.DmarcReport, category);
    }

    [Fact]
    public void DeserializeRecipients_WhenJsonIsNull_ReturnsEmptyList() {
        var method = typeof(NpgsqlInboundMailStore).GetMethod(
            "DeserializeRecipients",
            BindingFlags.NonPublic | BindingFlags.Static);

        var recipients = Assert.IsAssignableFrom<IReadOnlyList<string>>(
            method!.Invoke(null, ["null"])!);

        Assert.Empty(recipients);
    }

    [Fact]
    public void DeserializeRecipients_WhenJsonContainsRecipients_ReturnsRecipients() {
        var method = typeof(NpgsqlInboundMailStore).GetMethod(
            "DeserializeRecipients",
            BindingFlags.NonPublic | BindingFlags.Static);

        var recipients = Assert.IsAssignableFrom<IReadOnlyList<string>>(
            method!.Invoke(null, ["""["admin@fooddiary.club","support@fooddiary.club"]"""])!);

        Assert.Equal(["admin@fooddiary.club", "support@fooddiary.club"], recipients);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void AddWithNullableValue_WhenValueIsBlank_AddsDbNull(string? value) {
        using var command = new NpgsqlCommand();
        var method = GetNpgsqlExtensionMethod("AddWithNullableValue");

        method.Invoke(null, [command.Parameters, "value", value]);

        Assert.Equal(DBNull.Value, command.Parameters["value"].Value);
    }

    [Fact]
    public void AddWithNullableValue_WhenValueIsPresent_AddsString() {
        using var command = new NpgsqlCommand();
        var method = GetNpgsqlExtensionMethod("AddWithNullableValue");

        method.Invoke(null, [command.Parameters, "value", "hello"]);

        Assert.Equal("hello", command.Parameters["value"].Value);
    }

    private static MethodInfo GetNpgsqlExtensionMethod(string name) {
        var type = typeof(NpgsqlInboundMailStore).Assembly.GetType("FoodDiary.MailInbox.Infrastructure.Services.NpgsqlExtensions");
        return type!.GetMethod(name, BindingFlags.Public | BindingFlags.Static)!;
    }
}
