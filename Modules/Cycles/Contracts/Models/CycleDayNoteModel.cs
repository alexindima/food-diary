namespace FoodDiary.Modules.Cycles.Contracts.Models;

public sealed record CycleDayNoteModel(Guid Id, Guid CycleProfileId, DateOnly Date, string Notes);
