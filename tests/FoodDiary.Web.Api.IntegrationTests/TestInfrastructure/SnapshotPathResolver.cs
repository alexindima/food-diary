namespace FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;

[ExcludeFromCodeCoverage]
internal static class SnapshotPathResolver {
    private static readonly string SnapshotsDirectory = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Snapshots"));

    public static string GetPath(string snapshotFileName) {
        return Path.Combine(SnapshotsDirectory, snapshotFileName);
    }
}
