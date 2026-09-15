namespace FoodDiary.Modules.Users.Application.Abstractions.Models;

public sealed record UserCleanupBatch(int RemovedCount, UserCleanupCursor? LastExamined);
