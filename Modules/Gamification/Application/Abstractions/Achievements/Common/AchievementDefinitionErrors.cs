using FoodDiary.Results;

namespace FoodDiary.Application.Abstractions.Achievements.Common;

public static class AchievementDefinitionErrors {
    public static Error KeyConflict(string key) => new(
        "AchievementDefinition.KeyConflict",
        $"Achievement key '{key}' already exists.",
        ErrorKind.Conflict);

    public static Error VersionConflict() => new(
        "AchievementDefinition.VersionConflict",
        "The achievement definition was modified by another request. Reload it and try again.",
        ErrorKind.Conflict);

    public static Error NotFound(Guid id) => new(
        "AchievementDefinition.NotFound",
        $"Achievement definition '{id}' was not found.",
        ErrorKind.NotFound);
}
