using FoodDiary.Modules.Users.Presentation.Mappings.Mappings;
using FoodDiary.Modules.Notifications.Presentation.Mappings.Mappings;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Dietologist.Presentation.Contracts.Responses;

using FoodDiary.Modules.Users.Presentation.Responses;

namespace FoodDiary.Modules.Users.Presentation.Mappings;

public static class UserHttpResponseMappings {
    extension(ProfileOverviewModel model) {
        public ProfileOverviewHttpResponse ToHttpResponse() =>
                new(
                    model.User.ToHttpResponse(),
                    model.NotificationPreferences.ToHttpResponse(),
                    model.WebPushSubscriptions.Select(static subscription => subscription.ToHttpResponse()).ToList(),
                    model.DietologistRelationship is null
                        ? null
                        : new DietologistRelationshipHttpResponse(
                            model.DietologistRelationship.InvitationId,
                            model.DietologistRelationship.Status,
                            model.DietologistRelationship.Email,
                            model.DietologistRelationship.FirstName,
                            model.DietologistRelationship.LastName,
                            model.DietologistRelationship.DietologistUserId,
                            new DietologistPermissionsHttpResponse(
                                model.DietologistRelationship.Permissions.ShareMeals,
                                model.DietologistRelationship.Permissions.ShareStatistics,
                                model.DietologistRelationship.Permissions.ShareWeight,
                                model.DietologistRelationship.Permissions.ShareWaist,
                                model.DietologistRelationship.Permissions.ShareGoals,
                                model.DietologistRelationship.Permissions.ShareHydration,
                                model.DietologistRelationship.Permissions.ShareProfile,
                                model.DietologistRelationship.Permissions.ShareFasting),
                            model.DietologistRelationship.CreatedAtUtc,
                            model.DietologistRelationship.ExpiresAtUtc,
                            model.DietologistRelationship.AcceptedAtUtc));
    }
}
