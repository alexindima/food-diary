using FoodDiary.Application.Dietologist.Mappings;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Dietologist;

public partial class DietologistFeatureTests {

    [Fact]
    public void DietologistMappings_ToDietologistInfoModel_MapsAcceptedInvitation() {
        var dietologistId = UserId.New();
        User dietologist = CreateUser(dietologistId, "diet@example.com");
        typeof(User).GetProperty(nameof(User.FirstName))!.SetValue(dietologist, "Dana");
        typeof(User).GetProperty(nameof(User.LastName))!.SetValue(dietologist, "Smith");

        DietologistInvitation invitation = CreateAcceptedInvitation(UserId.New(), dietologistId);
        typeof(DietologistInvitation).GetProperty(nameof(DietologistInvitation.DietologistUser))!
            .SetValue(invitation, dietologist);

        var model = invitation.ToDietologistInfoModel();

        Assert.Equal(invitation.Id.Value, model.InvitationId);
        Assert.Equal(dietologistId.Value, model.DietologistUserId);
        Assert.Equal("diet@example.com", model.Email);
        Assert.Equal("Dana", model.FirstName);
        Assert.Equal("Smith", model.LastName);
        Assert.True(model.Permissions.ShareMeals);
    }


    [Fact]
    public void DietologistMappings_ToRelationshipModel_MapsDomainInvitation() {
        var dietologistId = UserId.New();
        User dietologist = CreateUser(dietologistId, "diet@example.com");
        typeof(User).GetProperty(nameof(User.FirstName))!.SetValue(dietologist, "Dana");
        typeof(User).GetProperty(nameof(User.LastName))!.SetValue(dietologist, "Smith");

        DietologistInvitation invitation = CreateAcceptedInvitation(UserId.New(), dietologistId);
        typeof(DietologistInvitation).GetProperty(nameof(DietologistInvitation.DietologistUser))!
            .SetValue(invitation, dietologist);

        DietologistRelationshipModel model = invitation.ToRelationshipModel();

        Assert.Equal(invitation.Id.Value, model.InvitationId);
        Assert.Equal(DietologistInvitationStatus.Accepted.ToString(), model.Status);
        Assert.Equal("diet@example.com", model.Email);
        Assert.Equal("Dana", model.FirstName);
        Assert.Equal("Smith", model.LastName);
        Assert.Equal(dietologistId.Value, model.DietologistUserId);
        Assert.Equal(invitation.AcceptedAtUtc, model.AcceptedAtUtc);
    }


    [Fact]
    public void DietologistMappings_ToClientSummaryModel_MapsProfileWhenShared() {
        var clientId = UserId.New();
        var dietologistId = UserId.New();
        User client = CreateUser(clientId, "client@example.com");
        typeof(User).GetProperty(nameof(User.FirstName))!.SetValue(client, "Casey");
        typeof(User).GetProperty(nameof(User.LastName))!.SetValue(client, "Jones");
        typeof(User).GetProperty(nameof(User.ProfileImage))!.SetValue(client, "https://cdn.example.com/avatar.jpg");
        typeof(User).GetProperty(nameof(User.Gender))!.SetValue(client, "F");
        typeof(User).GetProperty(nameof(User.Height))!.SetValue(client, 170d);
        typeof(User).GetProperty(nameof(User.ActivityLevel))!.SetValue(client, ActivityLevel.High);
        DateTime birthDate = new(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        typeof(User).GetProperty(nameof(User.BirthDate))!.SetValue(client, birthDate);

        DietologistInvitation invitation = CreateAcceptedInvitation(clientId, dietologistId);
        typeof(DietologistInvitation).GetProperty(nameof(DietologistInvitation.ClientUser))!
            .SetValue(invitation, client);

        var model = invitation.ToClientSummaryModel();

        Assert.Equal(clientId.Value, model.UserId);
        Assert.Equal("client@example.com", model.Email);
        Assert.Equal("Casey", model.FirstName);
        Assert.Equal("Jones", model.LastName);
        Assert.Equal("https://cdn.example.com/avatar.jpg", model.ProfileImage);
        Assert.Equal(birthDate, model.BirthDate);
        Assert.Equal("F", model.Gender);
        Assert.Equal(170d, model.Height);
        Assert.Equal(ActivityLevel.High.ToString(), model.ActivityLevel);
        Assert.Equal(invitation.AcceptedAtUtc, model.AcceptedAtUtc);
    }


    [Fact]
    public void DietologistMappings_ToCurrentUserInvitationModel_MapsExpiredDomainInvitation() {
        var clientId = UserId.New();
        var invitation = DietologistInvitation.Create(
            clientId,
            "diet@example.com",
            "hash",
            DateTime.UtcNow.AddDays(-1),
            AllDomainPermissions);
        typeof(DietologistInvitation).GetProperty(nameof(DietologistInvitation.ClientUser))!
            .SetValue(invitation, CreateUser(clientId, "client@example.com"));

        DietologistInvitationForCurrentUserModel model = invitation.ToCurrentUserInvitationModel();

        Assert.Equal(invitation.Id.Value, model.InvitationId);
        Assert.Equal(clientId.Value, model.ClientUserId);
        Assert.Equal("client@example.com", model.ClientEmail);
        Assert.Equal("Expired", model.Status);
        Assert.Equal(invitation.ExpiresAtUtc, model.ExpiresAtUtc);
    }


    [Fact]
    public void DietologistMappings_ToInvitationModel_MapsClientDetails() {
        var clientId = UserId.New();
        User client = CreateUser(clientId, "client@example.com");
        typeof(User).GetProperty(nameof(User.FirstName))!.SetValue(client, "Casey");
        typeof(User).GetProperty(nameof(User.LastName))!.SetValue(client, "Jones");

        DietologistInvitation invitation = CreatePendingInvitation(clientId, "diet@example.com");
        typeof(DietologistInvitation).GetProperty(nameof(DietologistInvitation.ClientUser))!
            .SetValue(invitation, client);

        var model = invitation.ToInvitationModel();

        Assert.Equal(invitation.Id.Value, model.InvitationId);
        Assert.Equal("client@example.com", model.ClientEmail);
        Assert.Equal("Casey", model.ClientFirstName);
        Assert.Equal("Jones", model.ClientLastName);
        Assert.Equal(DietologistInvitationStatus.Pending.ToString(), model.Status);
    }


    [Fact]
    public void DietologistModels_CanBeConstructed() {
        var permissions = new DietologistPermissionsModel(ShareMeals: true, ShareStatistics: false, ShareWeight: true, ShareWaist: false, ShareGoals: true, ShareHydration: false, ShareProfile: true, ShareFasting: false);
        DateTime acceptedAt = DateTime.UtcNow;
        DateTime expiresAt = acceptedAt.AddDays(7);

        var info = new DietologistInfoModel(
            Guid.NewGuid(), Guid.NewGuid(), "diet@example.com", "Dana", "Smith", permissions, acceptedAt);
        var invitation = new InvitationModel(
            Guid.NewGuid(), "client@example.com", "Casey", "Jones", "Pending", acceptedAt, expiresAt);

        Assert.Equal("diet@example.com", info.Email);
        Assert.Equal("client@example.com", invitation.ClientEmail);
        Assert.Equal(expiresAt, invitation.ExpiresAtUtc);
    }

}
