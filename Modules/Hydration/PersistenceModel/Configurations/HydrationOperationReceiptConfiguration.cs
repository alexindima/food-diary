using FoodDiary.Modules.Hydration.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Hydration.Domain.Entities.Tracking;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Modules.Hydration.PersistenceModel.Configurations;

internal sealed class HydrationOperationReceiptConfiguration : IEntityTypeConfiguration<HydrationOperationReceipt> {
    public void Configure(EntityTypeBuilder<HydrationOperationReceipt> builder) {
        builder.ToTable("HydrationOperationReceipts");
        builder.HasKey(receipt => new { receipt.UserId, receipt.OperationId });
        builder.Property(receipt => receipt.OperationId).ValueGeneratedNever();
        builder.Property(receipt => receipt.UserId).HasConversion(id => id.Value, value => new UserId(value));
        builder.Property(receipt => receipt.EntryId).HasConversion(id => id.Value, value => new HydrationEntryId(value));
        builder.HasIndex(receipt => receipt.EntryId).IsUnique();
        // No entry FK: deletion must not erase the permanent replay receipt.
    }
}
