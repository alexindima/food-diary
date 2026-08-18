namespace FoodDiary.Presentation.Api.Policies;

public static class PresentationRequestLimits {
    public const long AiPayloadBytes = 256 * 1024;
    public const long BulkRecommendationsPayloadBytes = 64 * 1024;
    public const long RichWritePayloadBytes = 1024 * 1024;
    public const long AdminImportPayloadBytes = 5 * 1024 * 1024;
}
