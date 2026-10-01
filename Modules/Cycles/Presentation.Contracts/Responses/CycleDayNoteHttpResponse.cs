namespace FoodDiary.Modules.Cycles.Presentation.Contracts.Responses;

public sealed record CycleDayNoteHttpResponse(Guid Id, Guid CycleProfileId, DateTime Date, string Notes);
