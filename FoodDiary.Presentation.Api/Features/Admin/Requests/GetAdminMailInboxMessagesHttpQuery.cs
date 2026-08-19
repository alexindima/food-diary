using FoodDiary.Presentation.Api.Policies;

namespace FoodDiary.Presentation.Api.Features.Admin.Requests;

public sealed record GetAdminMailInboxMessagesHttpQuery(
    [OpenApiNumericRange(PresentationQueryLimits.MinimumPageSize, PresentationQueryLimits.MaximumAdminMailInboxMessages)] int Limit = 50);
