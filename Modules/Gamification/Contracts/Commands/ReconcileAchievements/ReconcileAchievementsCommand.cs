using FoodDiary.Mediator;
using FoodDiary.Domain.ValueObjects.Ids;
namespace FoodDiary.Modules.Gamification.Contracts.Commands.ReconcileAchievements;
public sealed record ReconcileAchievementsCommand(UserId UserId, DateTime OccurredAtUtc) : IRequest;
