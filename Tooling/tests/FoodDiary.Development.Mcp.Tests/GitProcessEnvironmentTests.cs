using System.Diagnostics;
using FoodDiary.Development.Mcp.Infrastructure;

namespace FoodDiary.Development.Mcp.Tests;

[ExcludeFromCodeCoverage]
public sealed class GitProcessEnvironmentTests {
    [Fact]
    public void ClearLocalRepositoryVariables_PreservesUnrelatedEnvironment() {
        ProcessStartInfo startInfo = new();
        startInfo.Environment["GIT_DIR"] = "outer-git-directory";
        startInfo.Environment["GIT_WORK_TREE"] = "outer-worktree";
        startInfo.Environment["FOODDIARY_TEST_SENTINEL"] = "preserved";

        GitProcessEnvironment.ClearLocalRepositoryVariables(startInfo);

        Assert.False(startInfo.Environment.ContainsKey("GIT_DIR"));
        Assert.False(startInfo.Environment.ContainsKey("GIT_WORK_TREE"));
        Assert.Equal("preserved", startInfo.Environment["FOODDIARY_TEST_SENTINEL"]);
    }
}
