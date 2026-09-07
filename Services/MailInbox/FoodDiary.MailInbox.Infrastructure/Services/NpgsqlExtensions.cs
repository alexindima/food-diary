using Npgsql;

namespace FoodDiary.MailInbox.Infrastructure.Services;

internal static class NpgsqlExtensions {
    public static void AddWithNullableValue(this NpgsqlParameterCollection parameters, string parameterName, string? value) {
        parameters.AddWithValue(parameterName, string.IsNullOrWhiteSpace(value) ? DBNull.Value : value);
    }

    public static string? GetNullableString(this NpgsqlDataReader reader, int ordinal) {
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    public static async Task<DateTimeOffset?> GetNullableDateTimeOffsetAsync(
        this NpgsqlDataReader reader,
        int ordinal,
        CancellationToken cancellationToken) {
        return await reader.IsDBNullAsync(ordinal, cancellationToken).ConfigureAwait(false)
            ? null
            : await reader.GetFieldValueAsync<DateTimeOffset>(ordinal, cancellationToken).ConfigureAwait(false);
    }
}
