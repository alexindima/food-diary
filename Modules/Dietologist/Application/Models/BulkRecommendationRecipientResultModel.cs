namespace FoodDiary.Modules.Dietologist.Application.Models;

public union BulkRecommendationRecipientResultModel(
    BulkRecommendationRecipientResultModel.Created,
    BulkRecommendationRecipientResultModel.AlreadyProcessed,
    BulkRecommendationRecipientResultModel.Rejected) {
    public Guid ClientUserId => this switch {
        Created created => created.ClientId,
        AlreadyProcessed processed => processed.ClientId,
        Rejected rejected => rejected.ClientId,
        null => throw new InvalidOperationException("A recommendation recipient outcome is required."),
    };

    public bool Succeeded => this is Created or AlreadyProcessed;
    public Guid? RecommendationId => this switch {
        Created created => created.Id,
        AlreadyProcessed processed => processed.Id,
        Rejected or null => null,
    };
    public bool WasAlreadyProcessed => this is AlreadyProcessed;
    public string? ErrorCode => this is Rejected rejected ? rejected.Code : null;

    public sealed record Created(Guid ClientId, Guid Id);
    public sealed record AlreadyProcessed(Guid ClientId, Guid Id);
    public sealed record Rejected(Guid ClientId, string Code);
}
