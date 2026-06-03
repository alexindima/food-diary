using System.Reflection;

namespace FoodDiary.MailInbox.Tests;

[ExcludeFromCodeCoverage]
public sealed class MailInboxInitializerCommandTests {
    [Fact]
    public void Parse_WhenArgsAreEmpty_ReturnsNull() {
        var command = Parse();

        Assert.Null(command);
    }

    [Theory]
    [InlineData("status", null)]
    [InlineData("update", "Host=localhost;Database=mailinbox")]
    public void Parse_WithKnownCommand_ReturnsCommand(string name, string? connectionString) {
        var args = connectionString is null
            ? new[] { name }
            : [name, "--connection-string", connectionString];

        var command = Parse(args);

        Assert.NotNull(command);
        Assert.Equal(name, GetProperty<string>(command, "Name"));
        Assert.Equal(connectionString, GetProperty<string?>(command, "ConnectionString"));
    }

    [Fact]
    public void Parse_WithShortConnectionStringOption_ReturnsCommand() {
        var command = Parse("update", "-c", "Host=localhost;Database=mailinbox");

        Assert.NotNull(command);
        Assert.Equal("update", GetProperty<string>(command, "Name"));
        Assert.Equal("Host=localhost;Database=mailinbox", GetProperty<string?>(command, "ConnectionString"));
    }

    [Fact]
    public void Parse_WhenConnectionStringValueIsMissing_Throws() {
        var exception = Assert.Throws<TargetInvocationException>(() => Parse("update", "--connection-string"));

        Assert.IsType<InvalidOperationException>(exception.InnerException);
    }

    [Fact]
    public void Parse_WhenUnexpectedArgumentExists_Throws() {
        var exception = Assert.Throws<TargetInvocationException>(() => Parse("status", "unexpected"));

        Assert.IsType<InvalidOperationException>(exception.InnerException);
    }

    private static object? Parse(params string[] args) {
        var type = Assembly.Load("FoodDiary.MailInbox.Initializer")
            .GetType("FoodDiary.MailInbox.Initializer.InitializerCommand");
        var method = type!.GetMethod("Parse", BindingFlags.Public | BindingFlags.Static);
        return method!.Invoke(null, [args]);
    }

    private static TValue? GetProperty<TValue>(object instance, string name) =>
        (TValue?)instance.GetType().GetProperty(name)!.GetValue(instance);
}
