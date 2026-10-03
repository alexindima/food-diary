using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Modules.Users.PersistenceModel.Persistence.Configurations.Users;

internal sealed class UserPreferencesConfiguration : IEntityTypeConfiguration<UserPreferences> {
    public void Configure(EntityTypeBuilder<UserPreferences> builder) {
        builder.ToTable("UserPreferences");
        builder.HasKey(state => state.Id);
        builder.Property(state => state.Id).HasConversion(id => id.Value, value => new UserId(value)).ValueGeneratedNever();
        builder.Property<uint>("xmin").IsRowVersion();
        builder.Property(state => state.TimeZoneId).HasMaxLength(100);
        builder.Property(state => state.Language).HasDefaultValue("en");
        builder.Property(state => state.Theme).HasDefaultValue("ocean");
        builder.Property(state => state.UiStyle).HasDefaultValue("classic");
        builder.Property(state => state.SurfaceStyle).HasMaxLength(16).HasDefaultValue("normal").IsRequired();
        builder.Property(state => state.PushNotificationsEnabled).HasDefaultValue(value: false);
        builder.Property(state => state.FastingPushNotificationsEnabled).HasDefaultValue(value: true);
        builder.Property(state => state.SocialPushNotificationsEnabled).HasDefaultValue(value: true);
        builder.Property(state => state.FastingCheckInReminderHours).HasDefaultValue(12);
        builder.Property(state => state.FastingCheckInFollowUpReminderHours).HasDefaultValue(20);
        builder.Property(state => state.DashboardLayoutJson).HasColumnType("jsonb").HasColumnName("DashboardLayout");
    }
}
