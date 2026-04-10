namespace FoodDiary.Application.Fasting.Models;

public sealed record FastingMessageModel(
    string Id,
    string TitleKey,
    string BodyKey,
    string Tone,
    IReadOnlyDictionary<string, string>? BodyParams = null);
