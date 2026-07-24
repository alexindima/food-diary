using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations.Dietologist;

internal sealed class ClientTaskConfiguration : IEntityTypeConfiguration<ClientTask> {
    public void Configure(EntityTypeBuilder<ClientTask> builder) {
        builder.Property(task => task.Id)
            .HasConversion(id => id.Value, value => new ClientTaskId(value))
            .ValueGeneratedNever();

        builder.Property(task => task.DietologistUserId)
            .HasConversion(id => id.Value, value => new UserId(value));
        builder.Property(task => task.ClientUserId)
            .HasConversion(id => id.Value, value => new UserId(value));
        builder.Property(task => task.Title).IsRequired().HasMaxLength(200);
        builder.Property(task => task.Details).HasMaxLength(2000);
        builder.Property(task => task.Status).HasConversion<string>().HasMaxLength(32);

        builder.HasIndex(task => new { task.ClientUserId, task.CreatedOnUtc });
        builder.HasIndex(task => new { task.DietologistUserId, task.ClientUserId });
        builder.HasIndex(task => new { task.Status, task.DueAtUtc, task.DueReminderSentAtUtc });

        builder.HasOne<FoodDiary.Domain.Entities.Users.User>()
            .WithMany()
            .HasForeignKey(task => task.DietologistUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<FoodDiary.Domain.Entities.Users.User>()
            .WithMany()
            .HasForeignKey(task => task.ClientUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
