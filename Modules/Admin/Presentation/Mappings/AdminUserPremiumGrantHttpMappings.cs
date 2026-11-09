using FoodDiary.Modules.Admin.Application.Commands.SetAdminUserPremiumGrant;
using FoodDiary.Modules.Admin.Application.Queries.GetAdminUserPremiumGrant;
using FoodDiary.Modules.Admin.Presentation.Requests;
using FoodDiary.Modules.Admin.Presentation.Responses;
using FoodDiary.Modules.Users.Contracts.Models;

namespace FoodDiary.Modules.Admin.Presentation.Mappings;

public static class AdminUserPremiumGrantHttpMappings {
    public static GetAdminUserPremiumGrantQuery ToPremiumGrantQuery(this Guid userId) => new(userId);

    public static SetAdminUserPremiumGrantCommand ToSetPremiumGrantCommand(this AdminUserPremiumGrantHttpRequest request, Guid userId, Guid actorUserId) =>
        new(userId, actorUserId, request.EndsAtUtc, request.Reason);

    public static SetAdminUserPremiumGrantCommand ToRevokePremiumGrantCommand(this Guid userId, Guid actorUserId) =>
        new(userId, actorUserId, EndsAtUtc: null, Reason: null);

    public static AdminUserPremiumGrantHttpResponse ToHttpResponse(this UserManualPremiumGrantModel grant) =>
        new(grant.EndsAtUtc, grant.Reason);
}
