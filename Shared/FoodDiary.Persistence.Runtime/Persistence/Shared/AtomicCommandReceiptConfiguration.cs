using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Persistence.Runtime.Persistence.Shared;

internal sealed class AtomicCommandReceiptConfiguration : IEntityTypeConfiguration<AtomicCommandReceipt> {
    public void Configure(EntityTypeBuilder<AtomicCommandReceipt> builder) {
        builder.ToTable("AtomicCommandReceipts");
        builder.HasKey(receipt => receipt.Key);
        builder.Property(receipt => receipt.Key).HasMaxLength(64);
        builder.Property(receipt => receipt.RequestHash).HasMaxLength(64);
        builder.Property(receipt => receipt.ResponseType).HasMaxLength(512);
        builder.Property(receipt => receipt.ResponseJson).HasColumnType("jsonb");
        builder.HasIndex(receipt => receipt.ExpiresOnUtc);
        builder.HasIndex(receipt => receipt.UserId);
    }
}
