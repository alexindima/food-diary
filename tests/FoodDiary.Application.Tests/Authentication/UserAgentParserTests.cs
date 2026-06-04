using System.Reflection;

namespace FoodDiary.Application.Tests.Authentication;

[ExcludeFromCodeCoverage]
public class UserAgentParserTests {
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Parse_WithBlankUserAgent_ReturnsEmptyParsedAgent(string? userAgent) {
        var parsed = Parse(userAgent);

        Assert.Null(GetProperty<string?>(parsed, "BrowserName"));
        Assert.Null(GetProperty<string?>(parsed, "BrowserVersion"));
        Assert.Null(GetProperty<string?>(parsed, "OperatingSystem"));
        Assert.Null(GetProperty<string?>(parsed, "DeviceType"));
    }

    [Theory]
    [InlineData(
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0.0.0 Safari/537.36",
        "Chrome",
        "126.0.0.0",
        "Windows",
        "Desktop")]
    [InlineData(
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 Version/17.5 Safari/605.1.15",
        "Safari",
        "17.5",
        "macOS",
        "Desktop")]
    [InlineData(
        "Mozilla/5.0 (X11; Linux x86_64; rv:126.0) Gecko/20100101 Firefox/126.0",
        "Firefox",
        "126.0",
        "Linux",
        "Desktop")]
    [InlineData(
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0.0.0 Safari/537.36 Edg/126.0.0.0",
        "Edge",
        "126.0.0.0",
        "Windows",
        "Desktop")]
    [InlineData(
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0.0.0 Safari/537.36 OPR/111.0.0.0",
        "Opera",
        "111.0.0.0",
        "Windows",
        "Desktop")]
    public void Parse_WithDesktopBrowsers_ReturnsBrowserOperatingSystemAndDevice(
        string userAgent,
        string browserName,
        string browserVersion,
        string operatingSystem,
        string deviceType) {
        var parsed = Parse(userAgent);

        Assert.Equal(browserName, GetProperty<string?>(parsed, "BrowserName"));
        Assert.Equal(browserVersion, GetProperty<string?>(parsed, "BrowserVersion"));
        Assert.Equal(operatingSystem, GetProperty<string?>(parsed, "OperatingSystem"));
        Assert.Equal(deviceType, GetProperty<string?>(parsed, "DeviceType"));
    }

    [Theory]
    [InlineData(
        "Mozilla/5.0 (iPhone; CPU iPhone OS 17_5 like Mac OS X) AppleWebKit/605.1.15 Version/17.5 Mobile/15E148 Safari/604.1",
        "Safari",
        "17.5",
        "iOS",
        "Mobile")]
    [InlineData(
        "Mozilla/5.0 (iPad; CPU OS 17_5 like Mac OS X) AppleWebKit/605.1.15 Version/17.5 Mobile/15E148 Safari/604.1",
        "Safari",
        "17.5",
        "iOS",
        "Tablet")]
    [InlineData(
        "Mozilla/5.0 (Linux; Android 14; Pixel Tablet) AppleWebKit/537.36 Chrome/126.0.0.0 Safari/537.36",
        "Chrome",
        "126.0.0.0",
        "Android",
        "Tablet")]
    [InlineData(
        "Mozilla/5.0 (Linux; Android 14; Pixel 8) AppleWebKit/537.36 Chrome/126.0.0.0 Mobile Safari/537.36",
        "Chrome",
        "126.0.0.0",
        "Android",
        "Mobile")]
    public void Parse_WithMobileAndTabletUserAgents_ReturnsExpectedDevice(
        string userAgent,
        string browserName,
        string browserVersion,
        string operatingSystem,
        string deviceType) {
        var parsed = Parse(userAgent);

        Assert.Equal(browserName, GetProperty<string?>(parsed, "BrowserName"));
        Assert.Equal(browserVersion, GetProperty<string?>(parsed, "BrowserVersion"));
        Assert.Equal(operatingSystem, GetProperty<string?>(parsed, "OperatingSystem"));
        Assert.Equal(deviceType, GetProperty<string?>(parsed, "DeviceType"));
    }

    [Fact]
    public void Parse_WithChromiumAndUnknownOperatingSystem_ReturnsOtherBrowserAndDesktop() {
        var parsed = Parse("Mozilla/5.0 CustomAgent Chromium/126.0");

        Assert.Equal("Other", GetProperty<string?>(parsed, "BrowserName"));
        Assert.Null(GetProperty<string?>(parsed, "BrowserVersion"));
        Assert.Equal("Other", GetProperty<string?>(parsed, "OperatingSystem"));
        Assert.Equal("Desktop", GetProperty<string?>(parsed, "DeviceType"));
    }

    [Fact]
    public void Parse_WithMarkerWithoutVersion_ReturnsNullVersion() {
        var parsed = Parse("Mozilla/5.0 (Windows NT 10.0) Chrome/)");

        Assert.Equal("Chrome", GetProperty<string?>(parsed, "BrowserName"));
        Assert.Null(GetProperty<string?>(parsed, "BrowserVersion"));
    }

    [Fact]
    public void ExtractVersion_WithMissingMarker_ReturnsNull() {
        var parserType = GetParserType();
        var method = parserType.GetMethod("ExtractVersion", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(method);

        var version = method!.Invoke(null, ["Mozilla/5.0 CustomAgent", "Chrome/"]);

        Assert.Null(version);
    }

    private static object Parse(string? userAgent) {
        var parserType = GetParserType();
        var method = parserType.GetMethod("Parse", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(method);
        return method!.Invoke(null, [userAgent])!;
    }

    private static Type GetParserType() {
        var parserType = Type.GetType("FoodDiary.Application.Authentication.Services.UserAgents.UserAgentParser, FoodDiary.Application");
        Assert.NotNull(parserType);
        return parserType!;
    }

    private static TValue GetProperty<TValue>(object instance, string propertyName) {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(property);
        return (TValue)property!.GetValue(instance)!;
    }
}
