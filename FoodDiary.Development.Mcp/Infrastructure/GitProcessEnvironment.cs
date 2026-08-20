using System.Diagnostics;

namespace FoodDiary.Development.Mcp.Infrastructure;

internal static class GitProcessEnvironment {
    private static readonly string[] LocalRepositoryVariables = [
        "GIT_ALTERNATE_OBJECT_DIRECTORIES",
        "GIT_COMMON_DIR",
        "GIT_CONFIG",
        "GIT_CONFIG_COUNT",
        "GIT_CONFIG_PARAMETERS",
        "GIT_DIR",
        "GIT_GRAFT_FILE",
        "GIT_IMPLICIT_WORK_TREE",
        "GIT_INDEX_FILE",
        "GIT_INTERNAL_SUPER_PREFIX",
        "GIT_NO_REPLACE_OBJECTS",
        "GIT_OBJECT_DIRECTORY",
        "GIT_PREFIX",
        "GIT_REPLACE_REF_BASE",
        "GIT_SHALLOW_FILE",
        "GIT_WORK_TREE",
    ];

    public static void ClearLocalRepositoryVariables(ProcessStartInfo startInfo) {
        foreach (string variable in LocalRepositoryVariables) {
            startInfo.Environment.Remove(variable);
        }
    }
}
