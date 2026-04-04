using FoodDiary.Application.Dietologist.Commands.AcceptInvitation;
using FoodDiary.Application.Dietologist.Commands.DeclineInvitation;
using FoodDiary.Application.Dietologist.Commands.DisconnectDietologist;
using FoodDiary.Application.Dietologist.Commands.InviteDietologist;
using FoodDiary.Application.Dietologist.Commands.UpdateDietologistPermissions;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Application.Dietologist.Queries.GetInvitationByToken;
using FoodDiary.Application.Dietologist.Queries.GetMyClients;
using FoodDiary.Application.Dietologist.Queries.GetMyDietologist;
using FoodDiary.Presentation.Api.Features.Dietologist.Requests;

namespace FoodDiary.Presentation.Api.Features.Dietologist.Mappings;

public static class DietologistHttpMappings {
    public static InviteDietologistCommand ToCommand(this InviteDietologistHttpRequest request, Guid userId) =>
        new(userId, request.DietologistEmail, request.Permissions.ToInput());

    public static AcceptInvitationCommand ToCommand(this AcceptInvitationHttpRequest request, Guid userId) =>
        new(request.InvitationId, request.Token, userId);

    public static DeclineInvitationCommand ToCommand(this DeclineInvitationHttpRequest request, Guid userId) =>
        new(request.InvitationId, request.Token, userId);

    public static UpdateDietologistPermissionsCommand ToCommand(this UpdateDietologistPermissionsHttpRequest request, Guid userId) =>
        new(userId, request.Permissions.ToInput());

    public static DisconnectDietologistCommand ToCommand(this DisconnectClientHttpRequest request, Guid userId) =>
        new(userId, request.ClientUserId);

    public static GetMyDietologistQuery ToMyDietologistQuery(this Guid userId) => new(userId);

    public static GetMyClientsQuery ToMyClientsQuery(this Guid userId) => new(userId);

    public static GetInvitationByTokenQuery ToInvitationQuery(this Guid invitationId) => new(invitationId);

    private static DietologistPermissionsInput ToInput(this DietologistPermissionsHttpRequest request) =>
        new(request.ShareMeals, request.ShareStatistics, request.ShareWeight,
            request.ShareWaist, request.ShareGoals, request.ShareHydration);
}
