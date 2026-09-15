namespace FoodDiary.Modules.Fasting.Presentation.Responses;

public sealed record FastingMessageHttpResponse(
    string Id,
    string TitleKey,
    string BodyKey,
    string Tone,
    IReadOnlyDictionary<string, string>? BodyParams);
