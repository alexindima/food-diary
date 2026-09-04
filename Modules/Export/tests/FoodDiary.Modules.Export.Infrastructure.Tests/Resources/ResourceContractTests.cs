using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Resources;
using System.Text.RegularExpressions;
using FoodDiary.Application.Abstractions.Export.Common;
using FoodDiary.Modules.Export.Infrastructure.Resources;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Export.Infrastructure.Tests.Resources;

[ExcludeFromCodeCoverage]
public sealed partial class ResourceContractTests {
    private const string ResourceName = "FoodDiary.Modules.Export.Infrastructure.Resources.DiaryPdfReport";

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
    public void DiaryPdfResources_MatchTextContract() {
        IReadOnlyDictionary<string, string> resources = LoadResources(CultureInfo.InvariantCulture);
        string[] contractKeys = [.. typeof(DiaryPdfReportTexts)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(static property => property.Name)
            .Where(static name => !string.Equals(name, nameof(DiaryPdfReportTexts.CultureName), StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)];
        string[] resourceKeys = [.. resources.Keys.Order(StringComparer.Ordinal)];

        Assert.Equal(contractKeys, resourceKeys);
    }

    [Fact]
    public void ResourceAssembly_HasNeutralLanguageAndRussianSatellite() {
        Assembly assembly = typeof(DiaryPdfReportResourceTextProvider).Assembly;
        Assembly satellite = assembly.GetSatelliteAssembly(CultureInfo.GetCultureInfo("ru"));

        Assert.Multiple(
            () => Assert.Equal("FoodDiary.Modules.Export.Infrastructure", assembly.GetName().Name),
            () => Assert.Equal("en", assembly.GetCustomAttribute<NeutralResourcesLanguageAttribute>()?.CultureName),
            () => Assert.Equal("ru", satellite.GetName().CultureName),
            () => Assert.Contains(ResourceName + ".resources", assembly.GetManifestResourceNames(), StringComparer.Ordinal));
    }

    [Fact]
    public void AddExportResources_RegistersSingletonAndRendersRussianText() {
        var services = new ServiceCollection();
        Assert.Same(services, services.AddExportResources());
        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();
        DiaryPdfReportResourceTextProvider textProvider = Assert.IsType<DiaryPdfReportResourceTextProvider>(provider.GetRequiredService<IDiaryPdfReportTextProvider>());
        DiaryPdfReportTexts text = textProvider.GetTexts("ru-RU");

        Assert.Multiple(
            () => Assert.Same(textProvider, scope.ServiceProvider.GetRequiredService<IDiaryPdfReportTextProvider>()),
            () => Assert.Equal("ru", text.CultureName),
            () => Assert.Equal("Отчет дневника питания", text.ReportTitle));
    }

    private static IReadOnlyDictionary<string, string> LoadResources(CultureInfo culture) {
        var manager = new ResourceManager(ResourceName, typeof(DiaryPdfReportResourceTextProvider).Assembly);
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
