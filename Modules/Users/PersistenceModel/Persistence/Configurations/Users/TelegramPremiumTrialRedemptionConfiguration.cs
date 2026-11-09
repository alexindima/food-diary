using FoodDiary.Modules.Users.PersistenceModel.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Modules.Users.PersistenceModel.Persistence.Configurations.Users;

internal sealed class TelegramPremiumTrialRedemptionConfiguration : IEntityTypeConfiguration<TelegramPremiumTrialRedemption> {
    public void Configure(EntityTypeBuilder<TelegramPremiumTrialRedemption> builder) {
        builder.ToTable("TelegramPremiumTrialRedemptions");
        builder.HasKey(redemption => redemption.TelegramUserId);
        builder.Property(redemption => redemption.TelegramUserId).ValueGeneratedNever();
    }
}
