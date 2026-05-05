namespace FoodDiary.Application.Authentication.Services.UserAgents;

internal static class UserAgentParser {
    public static ParsedUserAgent Parse(string? userAgent) {
        if (string.IsNullOrWhiteSpace(userAgent)) {
            return new ParsedUserAgent(null, null, null, null);
        }

        var value = userAgent.Trim();
        var browser = ResolveBrowser(value);
        return new ParsedUserAgent(
            browser.Name,
            browser.Version,
            ResolveOperatingSystem(value),
            ResolveDeviceType(value));
    }

    private static (string? Name, string? Version) ResolveBrowser(string value) {
        if (value.Contains("Edg/", StringComparison.OrdinalIgnoreCase)) {
            return ("Edge", ExtractVersion(value, "Edg/"));
        }

        if (value.Contains("OPR/", StringComparison.OrdinalIgnoreCase)) {
            return ("Opera", ExtractVersion(value, "OPR/"));
        }

        if (value.Contains("Firefox/", StringComparison.OrdinalIgnoreCase)) {
            return ("Firefox", ExtractVersion(value, "Firefox/"));
        }

        if (value.Contains("Chrome/", StringComparison.OrdinalIgnoreCase) &&
            !value.Contains("Chromium/", StringComparison.OrdinalIgnoreCase)) {
            return ("Chrome", ExtractVersion(value, "Chrome/"));
        }

        if (value.Contains("Safari/", StringComparison.OrdinalIgnoreCase) &&
            value.Contains("Version/", StringComparison.OrdinalIgnoreCase)) {
            return ("Safari", ExtractVersion(value, "Version/"));
        }

        return ("Other", null);
    }

    private static string ResolveOperatingSystem(string value) {
        if (value.Contains("Windows", StringComparison.OrdinalIgnoreCase)) {
            return "Windows";
        }

        if (value.Contains("Android", StringComparison.OrdinalIgnoreCase)) {
            return "Android";
        }

        if (value.Contains("iPhone", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("iPad", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("iOS", StringComparison.OrdinalIgnoreCase)) {
            return "iOS";
        }

        if (value.Contains("Mac OS X", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("Macintosh", StringComparison.OrdinalIgnoreCase)) {
            return "macOS";
        }

        if (value.Contains("Linux", StringComparison.OrdinalIgnoreCase)) {
            return "Linux";
        }

        return "Other";
    }

    private static string ResolveDeviceType(string value) {
        if (value.Contains("Tablet", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("iPad", StringComparison.OrdinalIgnoreCase)) {
            return "Tablet";
        }

        if (value.Contains("Mobile", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("Android", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("iPhone", StringComparison.OrdinalIgnoreCase)) {
            return "Mobile";
        }

        return "Desktop";
    }

    private static string? ExtractVersion(string value, string marker) {
        var markerIndex = value.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (markerIndex < 0) {
            return null;
        }

        var start = markerIndex + marker.Length;
        var end = start;
        while (end < value.Length && !char.IsWhiteSpace(value[end]) && value[end] != ';' && value[end] != ')') {
            end++;
        }

        return end > start ? value[start..end] : null;
    }
}

internal sealed record ParsedUserAgent(
    string? BrowserName,
    string? BrowserVersion,
    string? OperatingSystem,
    string? DeviceType);
