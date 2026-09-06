using System.Runtime.CompilerServices;
using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.Entities.Users;

namespace FoodDiary.Application.Tests.Dietologist;

public partial class DietologistFeatureTests {
    private static readonly ConditionalWeakTable<DietologistInvitation, InvitationProfiles> Profiles = [];

    private static InvitationProfiles GetInvitationProfiles(DietologistInvitation invitation) =>
        Profiles.GetOrCreateValue(invitation);

    private static void SetInvitationProfile(DietologistInvitation invitation, User? client = null, User? dietologist = null) {
        InvitationProfiles profiles = GetInvitationProfiles(invitation);
        if (client is not null) { profiles.Client = client; }
        if (dietologist is not null) { profiles.Dietologist = dietologist; }
    }

    [ExcludeFromCodeCoverage]
    private sealed class InvitationProfiles {
        public User? Client { get; set; }
        public User? Dietologist { get; set; }
    }
}
