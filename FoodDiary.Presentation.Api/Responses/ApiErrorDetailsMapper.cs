using System.Text.Json;

namespace FoodDiary.Presentation.Api.Responses;

public static class ApiErrorDetailsMapper {
    public static IReadOnlyDictionary<string, string[]>? Normalize(IReadOnlyDictionary<string, string[]>? details) {
        if (details is null || details.Count == 0) {
            return null;
        }

        return details.ToDictionary(
            static entry => ToCamelCasePath(entry.Key),
            static entry => entry.Value,
            StringComparer.Ordinal);
    }

    public static string ToCamelCasePath(string path) =>
        string.Join(".", path.Split('.', StringSplitOptions.RemoveEmptyEntries)
            .Select(static segment => JsonNamingPolicy.CamelCase.ConvertName(segment)));
}
