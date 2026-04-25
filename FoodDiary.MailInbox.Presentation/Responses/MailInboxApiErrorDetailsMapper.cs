namespace FoodDiary.MailInbox.Presentation.Responses;

public static class MailInboxApiErrorDetailsMapper {
    public static string ToCamelCasePath(string path) {
        if (string.IsNullOrWhiteSpace(path)) {
            return "request";
        }

        var segments = path.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return string.Join(".", segments.Select(ToCamelCaseSegment));
    }

    private static string ToCamelCaseSegment(string segment) =>
        string.IsNullOrWhiteSpace(segment) || char.IsLower(segment[0])
            ? segment
            : char.ToLowerInvariant(segment[0]) + segment[1..];
}
