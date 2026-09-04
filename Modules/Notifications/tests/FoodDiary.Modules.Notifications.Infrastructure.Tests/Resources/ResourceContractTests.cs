using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Resources;
using System.Text.RegularExpressions;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Modules.Notifications.Infrastructure;
using FoodDiary.Modules.Notifications.Infrastructure.Resources;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure.Tests.Resources;

[ExcludeFromCodeCoverage]
public sealed partial class ResourceContractTests {
    private const string ResourceName = "FoodDiary.Modules.Notifications.Infrastructure.Resources.NotificationTemplates";

    [Fact]
    public void LocalizedResources_HaveMatchingKeysAndFormatArguments() {
        IReadOnlyDictionary<string, string> neutral = LoadResources(CultureInfo.InvariantCulture);
        IReadOnlyDictionary<string, string> russian = LoadResources(CultureInfo.GetCultureInfo("ru"));
        string[] neutralKeys = [.. neutral.Keys.Order(StringComparer.Ordinal)];
        string[] russianKeys = [.. russian.Keys.Order(StringComparer.Ordinal)];

        Assert.Equal(neutralKeys, russianKeys);
        Assert.All(neutralKeys, key => Assert.Equal(
            GetFormatArgumentIndexes(neutral[key]),
            GetFormatArgumentIndexes(russian[key])));
    }

    [Fact]
    public void NotificationResources_CoverEveryNotificationType() {
        IReadOnlyDictionary<string, string> resources = LoadResources(CultureInfo.InvariantCulture);
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
        Assert.All(notificationTypes, type => Assert.Contains($"{type}_Title", resources.Keys, StringComparer.Ordinal));
    }

    [Fact]
    public void ResourceAssembly_HasNeutralLanguageAndRussianSatellite() {
        Assembly assembly = typeof(NotificationResourceRenderer).Assembly;
        Assembly satellite = assembly.GetSatelliteAssembly(CultureInfo.GetCultureInfo("ru"));

        Assert.Multiple(
            () => Assert.Equal("FoodDiary.Modules.Notifications.Infrastructure", assembly.GetName().Name),
            () => Assert.Equal("en", assembly.GetCustomAttribute<NeutralResourcesLanguageAttribute>()?.CultureName),
            () => Assert.Equal("ru", satellite.GetName().CultureName),
            () => Assert.Contains(ResourceName + ".resources", assembly.GetManifestResourceNames(), StringComparer.Ordinal));
    }

    [Fact]
    public void AddNotificationResources_RegistersSingletonAndRendersRussianText() {
        var services = new ServiceCollection();
        Assert.Same(services, services.AddNotificationResources());
        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();
        NotificationResourceRenderer renderer = Assert.IsType<NotificationResourceRenderer>(provider.GetRequiredService<INotificationTextRenderer>());
        NotificationText text = renderer.Render(NotificationTypes.NewRecommendation, "ru-RU", string.Empty);

        Assert.Multiple(
            () => Assert.Same(renderer, scope.ServiceProvider.GetRequiredService<INotificationTextRenderer>()),
            () => Assert.Equal("Новая рекомендация от вашего диетолога", text.Title),
            () => Assert.Null(text.Body));
    }

    private static IReadOnlyDictionary<string, string> LoadResources(CultureInfo culture) {
        var manager = new ResourceManager(ResourceName, typeof(NotificationResourceRenderer).Assembly);
        ResourceSet resourceSet = manager.GetResourceSet(culture, createIfNotExists: true, tryParents: false)
            ?? throw new InvalidOperationException($"Resource set '{ResourceName}' for culture '{culture.Name}' was not found.");
        return resourceSet.Cast<DictionaryEntry>().ToDictionary(
            static entry => (string)entry.Key,
            static entry => (string)entry.Value!,
            StringComparer.Ordinal);
    }

    private static string[] GetFormatArgumentIndexes(string value) =>
        [.. FormatArgumentRegex().Matches(value)
            .Select(static match => match.Groups["index"].Value)
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)];

    [GeneratedRegex(@"\{(?<index>\d+)(?:[^}]*)\}", RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex FormatArgumentRegex();
}
