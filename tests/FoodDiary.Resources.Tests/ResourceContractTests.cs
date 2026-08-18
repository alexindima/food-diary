using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Resources;
using System.Text.RegularExpressions;
using FoodDiary.Application.Abstractions.Export.Common;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Resources.Notifications;

namespace FoodDiary.Resources.Tests;

[ExcludeFromCodeCoverage]
public sealed partial class ResourceContractTests {
    private const string NotificationResourceName = "FoodDiary.Resources.Notifications.NotificationTemplates";
    private const string DiaryPdfResourceName = "FoodDiary.Resources.Reports.DiaryPdfReport";

    [Theory]
    [InlineData(NotificationResourceName)]
    [InlineData(DiaryPdfResourceName)]
    public void LocalizedResources_HaveMatchingKeysAndFormatArguments(string resourceName) {
        IReadOnlyDictionary<string, string> neutral = LoadResources(resourceName, CultureInfo.InvariantCulture);
        IReadOnlyDictionary<string, string> russian = LoadResources(resourceName, CultureInfo.GetCultureInfo("ru"));

        string[] neutralKeys = [.. neutral.Keys.Order(StringComparer.Ordinal)];
        string[] russianKeys = [.. russian.Keys.Order(StringComparer.Ordinal)];

        Assert.Equal(neutralKeys, russianKeys);
        Assert.All(neutralKeys, key => Assert.Equal(
            GetFormatArgumentIndexes(neutral[key]),
            GetFormatArgumentIndexes(russian[key])));
    }

    [Fact]
    public void NotificationResources_CoverEveryNotificationType() {
        IReadOnlyDictionary<string, string> resources = LoadResources(
            NotificationResourceName,
            CultureInfo.InvariantCulture);

        string[] notificationTypes = [.. typeof(NotificationTypes)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(static field => field.IsLiteral && field.FieldType == typeof(string))
            .Select(static field => (string)field.GetRawConstantValue()!)
            .Order(StringComparer.Ordinal)];
        string[] resourceTypes = [.. resources.Keys
            .Select(static key => key[..key.LastIndexOf('_')])
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)];

        Assert.Equal(notificationTypes, resourceTypes);
        Assert.All(notificationTypes, type => Assert.Contains(
            $"{type}_Title",
            resources.Keys,
            StringComparer.Ordinal));
    }

    [Fact]
    public void DiaryPdfResources_MatchTextContract() {
        IReadOnlyDictionary<string, string> resources = LoadResources(
            DiaryPdfResourceName,
            CultureInfo.InvariantCulture);

        string[] contractKeys = [.. typeof(DiaryPdfReportTexts)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(static property => property.Name)
            .Where(static name => !string.Equals(
                name,
                nameof(DiaryPdfReportTexts.CultureName),
                StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)];
        string[] resourceKeys = [.. resources.Keys.Order(StringComparer.Ordinal)];

        Assert.Equal(contractKeys, resourceKeys);
    }

    private static IReadOnlyDictionary<string, string> LoadResources(string resourceName, CultureInfo culture) {
        var manager = new ResourceManager(resourceName, typeof(NotificationResourceRenderer).Assembly);
        ResourceSet resourceSet = manager.GetResourceSet(culture, createIfNotExists: true, tryParents: false)
            ?? throw new InvalidOperationException($"Resource set '{resourceName}' for culture '{culture.Name}' was not found.");

        return resourceSet
            .Cast<DictionaryEntry>()
            .ToDictionary(
                static entry => (string)entry.Key,
                static entry => (string)entry.Value!,
                StringComparer.Ordinal);
    }

    private static string[] GetFormatArgumentIndexes(string value) =>
        [.. FormatArgumentRegex()
            .Matches(value)
            .Select(static match => match.Groups["index"].Value)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)];

    [GeneratedRegex(
        @"\{(?<index>\d+)(?:[^}]*)\}",
        RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
        matchTimeoutMilliseconds: 1000)]
    private static partial Regex FormatArgumentRegex();
}
