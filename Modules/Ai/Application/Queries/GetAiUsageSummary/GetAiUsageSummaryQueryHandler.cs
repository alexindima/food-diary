using FoodDiary.Modules.Ai.Contracts.Models;
using FoodDiary.Modules.Ai.Application.Abstractions.Common;
using FoodDiary.Modules.Ai.Contracts.Queries.GetAiUsageSummary;
using FoodDiary.Mediator;

namespace FoodDiary.Modules.Ai.Application.Queries.GetAiUsageSummary;

public sealed class GetAiUsageSummaryQueryHandler(IAiUsageQuery usageRepository) : IRequestHandler<GetAiUsageSummaryQuery, AiUsageSummary> {
    public Task<AiUsageSummary> Handle(GetAiUsageSummaryQuery request, CancellationToken cancellationToken) {
        DateTime fromUtc = request.FromUtc;
        DateTime toUtc = request.ToUtc;
        return usageRepository.GetSummaryAsync(fromUtc, toUtc, cancellationToken);
    }

}
