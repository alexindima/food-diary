using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;
using FoodDiary.Modules.Identity.Contracts.Authentication.Models;
using FoodDiary.Modules.Identity.Contracts.Authentication.Queries.GetLoginDeviceSummary;
using FoodDiary.Mediator;

namespace FoodDiary.Modules.Identity.Application.Authentication.Queries.GetLoginDeviceSummary;

public sealed class GetLoginDeviceSummaryQueryHandler(IUserLoginEventQuery repository)
    : IRequestHandler<GetLoginDeviceSummaryQuery, IReadOnlyList<UserLoginDeviceSummaryModel>> {
    public Task<IReadOnlyList<UserLoginDeviceSummaryModel>> Handle(GetLoginDeviceSummaryQuery request, CancellationToken cancellationToken) =>
        repository.GetDeviceSummaryAsync(request.FromUtc, request.ToUtc, cancellationToken);
}
