using FoodDiary.Modules.Fasting.Contracts.Read.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;

namespace FoodDiary.Modules.Fasting.Contracts.Queries.ReadCurrentFasting;

// Trusted owner operation: the caller authorizes the supplied scope.
public sealed record ReadCurrentFastingQuery(UserId UserId) : IQuery<FastingSessionModel?>;
