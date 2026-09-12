using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations.Meals;

internal sealed class MealRecognitionReceiptConfiguration : IEntityTypeConfiguration<MealRecognitionReceipt> {
    public void Configure(EntityTypeBuilder<MealRecognitionReceipt> builder) {
        builder.ToTable("MealRecognitionReceipts");
        builder.HasKey(receipt => receipt.OperationId);
        builder.Property(receipt => receipt.OperationId).ValueGeneratedNever();
        builder.Property<uint>("xmin").IsRowVersion();
        builder.Property(receipt => receipt.UserId).HasConversion(id => id.Value, value => new UserId(value));
        builder.Property(receipt => receipt.MealId).HasConversion(id => id.Value, value => new MealId(value));
        builder.HasIndex(receipt => new { receipt.UserId, receipt.RecognitionId }).IsUnique();
        // No Meal foreign key: this receipt must survive ordinary meal deletion and undo.
    }
}
