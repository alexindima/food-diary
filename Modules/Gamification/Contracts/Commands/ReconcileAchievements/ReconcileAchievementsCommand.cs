using FoodDiary.Mediator;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
namespace FoodDiary.Modules.Gamification.Contracts.Commands.ReconcileAchievements;
public sealed record ReconcileAchievementsCommand(UserId UserId, DateTime OccurredAtUtc) : IRequest;
