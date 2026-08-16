namespace FoodDiary.Development.Mcp.Tests;

[ExcludeFromCodeCoverage]
public sealed class ChangeSetSnapshotServiceTests {
    [Theory]
    [InlineData("R  new/path.cs\0old/path.cs\0", "new/path.cs")]
    [InlineData("C  copied/path.cs\0source/path.cs\0", "copied/path.cs")]
    public void ParseChangedPaths_UsesDestinationForRenameAndCopy(
        string porcelain,
        string expectedPath) {
        string[] result = ChangeSetSnapshotService.ParseChangedPaths(porcelain);

        Assert.Equal([expectedPath], result);
    }

    [Fact]
    public void ParseChangedPaths_PreservesOrdinaryDeletesAndUnicodePaths() {
        const string porcelain = " D deleted.cs\0?? каталог/новый файл.cs\0";

        string[] result = ChangeSetSnapshotService.ParseChangedPaths(porcelain);

        Assert.Equal(["deleted.cs", "каталог/новый файл.cs"], result);
    }
}
