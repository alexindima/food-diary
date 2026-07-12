namespace FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;

[ExcludeFromCodeCoverage]
internal static class SnapshotPathResolver {
    private const string UpdateSnapshotsEnvironmentVariable = "UPDATE_CONTRACT_SNAPSHOTS";
    private static readonly string SnapshotsDirectory = ResolveSnapshotsDirectory();

    public static string GetPath(string snapshotFileName) {
        return Path.Combine(SnapshotsDirectory, snapshotFileName);
    }

    private static string ResolveSnapshotsDirectory() {
        string outputDirectory = Path.Combine(AppContext.BaseDirectory, "Snapshots");
        if (!string.Equals(Environment.GetEnvironmentVariable(UpdateSnapshotsEnvironmentVariable), "1", StringComparison.Ordinal)) {
            return outputDirectory;
        }

        DirectoryInfo? directory = new(Directory.GetCurrentDirectory());
        while (directory is not null) {
            string sourceDirectory = Path.Combine(directory.FullName, "tests", "FoodDiary.Web.Api.IntegrationTests", "Snapshots");
            if (Directory.Exists(sourceDirectory)) {
                return sourceDirectory;
            }

            directory = directory.Parent;
        }

        return outputDirectory;
    }
}
