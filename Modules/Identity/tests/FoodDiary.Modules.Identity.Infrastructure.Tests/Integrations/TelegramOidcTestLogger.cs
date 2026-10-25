using Microsoft.Extensions.Logging;

namespace FoodDiary.Modules.Identity.Infrastructure.Tests.Integrations;

[ExcludeFromCodeCoverage]
internal sealed class TelegramOidcTestLogger<T> : ILogger<T> {
    public List<Entry> Entries { get; } = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) {
        var fields = Assert.IsAssignableFrom<IEnumerable<KeyValuePair<string, object?>>>(state).ToDictionary(StringComparer.Ordinal);
        Entries.Add(new Entry(logLevel, fields, formatter(state, exception), exception));
    }

    public void AssertRejection(string stage, string reason, params string[] privateValues) {
        Entry entry = Assert.Single(Entries);
        Assert.Equal(LogLevel.Information, entry.Level);
        Assert.Equal(stage, entry.Fields["Stage"]);
        Assert.Equal(reason, entry.Fields["Reason"]);
        Assert.Null(entry.Exception);
        Assert.All(entry.Fields.Keys, key => Assert.Contains(key, new[] { "Stage", "Reason", "HttpStatusCode", "ExceptionType", "{OriginalFormat}" }, StringComparer.Ordinal));
        foreach (string value in privateValues) {
            Assert.DoesNotContain(value, entry.Message, StringComparison.Ordinal);
            Assert.All(entry.Fields.Values, field => Assert.DoesNotContain(value, field?.ToString() ?? string.Empty, StringComparison.Ordinal));
        }
    }

    internal sealed record Entry(LogLevel Level, Dictionary<string, object?> Fields, string Message, Exception? Exception);
}
