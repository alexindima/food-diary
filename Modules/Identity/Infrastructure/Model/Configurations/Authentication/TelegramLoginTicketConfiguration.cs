using FoodDiary.Infrastructure.Persistence.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations.Authentication;

internal sealed class TelegramLoginTicketConfiguration : IEntityTypeConfiguration<TelegramLoginTicket> {
    public void Configure(EntityTypeBuilder<TelegramLoginTicket> builder) {
        builder.ToTable("TelegramLoginTickets");
        builder.HasKey(ticket => ticket.Fingerprint);
        builder.Property(ticket => ticket.Fingerprint).HasMaxLength(64);
        builder.Property(ticket => ticket.Purpose).HasMaxLength(64);
        builder.Property(ticket => ticket.BrowserBindingHash).HasMaxLength(64);
        builder.Property(ticket => ticket.ProtectedPayload).HasMaxLength(16384);
        builder.Property(ticket => ticket.ExpiresAtUtc).HasColumnType("timestamp with time zone");
        builder.HasIndex(ticket => ticket.ExpiresAtUtc);
    }
}
