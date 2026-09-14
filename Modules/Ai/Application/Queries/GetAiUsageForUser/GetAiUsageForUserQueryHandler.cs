using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Ai.Contracts.Models;
using FoodDiary.Modules.Ai.Application.Abstractions.Common;
using FoodDiary.Modules.Ai.Contracts.Queries.GetAiUsageForUser;
using FoodDiary.Mediator;

namespace FoodDiary.Modules.Ai.Application.Queries.GetAiUsageForUser;

public sealed class GetAiUsageForUserQueryHandler(IAiUsageQuery usageRepository) : IRequestHandler<GetAiUsageForUserQuery, AiUsageSummary> {
    public Task<AiUsageSummary> Handle(GetAiUsageForUserQuery request, CancellationToken cancellationToken) {
        DateTime fromUtc = request.FromUtc;
        DateTime toUtc = request.ToUtc;
        Guid userId = request.UserId;
        return usageRepository.GetSummaryForUserAsync(fromUtc, toUtc, new UserId(userId), cancellationToken);
    }

}
