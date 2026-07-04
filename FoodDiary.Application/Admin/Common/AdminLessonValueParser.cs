using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Admin.Common;

internal static class AdminLessonValueParser {
    public static Result<LessonCategory> ParseCategory(string? value, string fieldName) =>
        EnumValueParser.ParseRequired<LessonCategory>(value, fieldName, "Invalid lesson category.");

    public static Result<LessonDifficulty> ParseDifficulty(string? value, string fieldName) =>
        EnumValueParser.ParseRequired<LessonDifficulty>(value, fieldName, "Invalid lesson difficulty.");
}
