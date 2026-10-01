namespace FoodDiary.Modules.Cycles.Application.Abstractions.Models;

public sealed record CycleDayNoteReadModel(Guid Id, Guid CycleProfileId, DateOnly Date, string Notes);
