using System.Reflection;
using FoodDiary.Modules.Ai.Infrastructure.Diagnostics;
using FoodDiary.Modules.Ai.Infrastructure.Providers.Services.Recipes;

namespace FoodDiary.Modules.Ai.Infrastructure.Tests.Services;

[ExcludeFromCodeCoverage]
public sealed class WindowsCoverageMarkerTests {
    [Fact]
    public void Marker_OnlySelectsPlatformHelpersAndKeepsThemAvailableToLinuxCollectors() {
        Type[] owners = [typeof(RecipeSourceReader), typeof(RecipeVideoProcessor)];
        MethodInfo[] marked = [.. owners.SelectMany(owner => owner.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
            .Where(method => method.GetCustomAttribute<ExcludeFromWindowsCoverageAttribute>() is not null)
            .OrderBy(method => method.Name, StringComparer.Ordinal)];
        AttributeUsageAttribute? usage = typeof(ExcludeFromWindowsCoverageAttribute).GetCustomAttribute<AttributeUsageAttribute>();

        Assert.NotNull(usage);
        Assert.Multiple(() => {
            Assert.Equal(["ConnectPublicAsync", "RestrictUnixDirectoryPermissions"], marked.Select(method => method.Name), StringComparer.Ordinal);
            Assert.Equal(AttributeTargets.Method, usage.ValidOn);
            Assert.False(usage.Inherited);
            Assert.False(usage.AllowMultiple);
        });
        Assert.All(marked, method => Assert.False(method.IsDefined(typeof(ExcludeFromCodeCoverageAttribute), inherit: false)));
    }
}
