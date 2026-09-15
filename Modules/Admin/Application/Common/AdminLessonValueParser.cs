using FoodDiary.Modules.Lessons.Domain.Contracts.Enums;
using FoodDiary.Results;
using FoodDiary.Modules.Admin.Application.Internal.Validation;

namespace FoodDiary.Modules.Admin.Application.Common;

internal static class AdminLessonValueParser {
    public static Result<LessonCategory> ParseCategory(string? value, string fieldName) =>
        EnumValueParser.ParseRequired<LessonCategory>(value, fieldName, "Invalid lesson category.");

    public static Result<LessonDifficulty> ParseDifficulty(string? value, string fieldName) =>
        EnumValueParser.ParseRequired<LessonDifficulty>(value, fieldName, "Invalid lesson difficulty.");
}
