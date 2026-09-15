using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Authentication.Models;
using FoodDiary.Application.Abstractions.Authentication.Queries.GetLoginDeviceSummary;
using FoodDiary.Mediator;

namespace FoodDiary.Application.Identity.Authentication.Queries.GetLoginDeviceSummary;

public sealed class GetLoginDeviceSummaryQueryHandler(IUserLoginEventQuery repository)
    : IRequestHandler<GetLoginDeviceSummaryQuery, IReadOnlyList<UserLoginDeviceSummaryModel>> {
    public Task<IReadOnlyList<UserLoginDeviceSummaryModel>> Handle(GetLoginDeviceSummaryQuery request, CancellationToken cancellationToken) =>
        repository.GetDeviceSummaryAsync(request.FromUtc, request.ToUtc, cancellationToken);
}
