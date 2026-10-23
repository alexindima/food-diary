namespace FoodDiary.Modules.Marketing.Application.Common;

public static class MarketingLandingPath {
    private const int MaximumLength = 512;
    private static readonly Uri ParsingOrigin = new("https://marketing.invalid");

    public static string Sanitize(string? value) {
        if (string.IsNullOrWhiteSpace(value) ||
            !Uri.TryCreate(ParsingOrigin, value.Trim(), out Uri? url) ||
            (!string.Equals(url.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) &&
             !string.Equals(url.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase))) {
            return "/";
        }

        // Only campaign parameters belong in attribution. Return URLs can nest auth credentials.
        IEnumerable<string> query = url.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(static part => part.Split('=', 2))
            .Where(static pair => IsCampaignKey(DecodeQueryPart(pair[0])))
            .Select(static pair => $"{EncodeQueryPart(DecodeQueryPart(pair[0]))}={EncodeQueryPart(pair.Length > 1 ? DecodeQueryPart(pair[1]) : string.Empty)}");
        string search = string.Join('&', query);
        string result = search.Length > 0 ? $"{url.AbsolutePath}?{search}" : url.AbsolutePath;
        return result.Length <= MaximumLength ? result : result[..MaximumLength];
    }

    private static bool IsCampaignKey(string key) =>
        key is "utm_source" or "utm_medium" or "utm_campaign" or "utm_content" or "utm_term";

    private static string DecodeQueryPart(string value) => Uri.UnescapeDataString(value.Replace('+', ' '));

    private static string EncodeQueryPart(string value) => Uri.EscapeDataString(value).Replace("%20", "+", StringComparison.Ordinal);
}
