using FoodDiary.Domain.Entities.Ai;
using FoodDiary.Domain.Entities.Content;
using FoodDiary.Domain.Entities.FavoriteMeals;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations;

internal sealed class WeightEntryConfiguration : IEntityTypeConfiguration<WeightEntry> {
    public void Configure(EntityTypeBuilder<WeightEntry> entity) {
        entity.Property(e => e.Id).HasConversion(
            id => id.Value,
            value => new WeightEntryId(value));

        entity.Property(e => e.UserId).HasConversion(
            id => id.Value,
            value => new UserId(value));

        entity.Property(e => e.Date)
            .HasColumnType("date");

        entity.HasIndex(e => new { e.UserId, e.Date }).IsUnique();

        entity.HasOne(e => e.User)
            .WithMany(u => u.WeightEntries)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class WaistEntryConfiguration : IEntityTypeConfiguration<WaistEntry> {
    public void Configure(EntityTypeBuilder<WaistEntry> entity) {
        entity.Property(e => e.Id).HasConversion(
            id => id.Value,
            value => new WaistEntryId(value));

        entity.Property(e => e.UserId).HasConversion(
            id => id.Value,
            value => new UserId(value));

        entity.Property(e => e.Date)
            .HasColumnType("date");

        entity.HasIndex(e => new { e.UserId, e.Date }).IsUnique();

        entity.HasOne(e => e.User)
            .WithMany(u => u.WaistEntries)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class CycleConfiguration : IEntityTypeConfiguration<Cycle> {
    public void Configure(EntityTypeBuilder<Cycle> entity) {
        entity.Property(e => e.Id).HasConversion(
            id => id.Value,
            value => new CycleId(value));

        entity.Property(e => e.UserId).HasConversion(
            id => id.Value,
            value => new UserId(value));

        entity.Property(e => e.StartDate)
            .HasColumnType("date");

        entity.Property(e => e.AverageLength)
            .HasDefaultValue(28);

        entity.Property(e => e.LutealLength)
            .HasDefaultValue(14);

        entity.Property(e => e.Notes)
            .HasMaxLength(1024);

        entity.HasOne<global::FoodDiary.Domain.Entities.Users.User>()
            .WithMany(u => u.Cycles)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasMany(e => e.Days)
            .WithOne(d => d.Cycle)
            .HasForeignKey(d => d.CycleId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.Navigation(e => e.Days)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        entity.HasIndex(e => new { e.UserId, e.StartDate })
            .HasDatabaseName("IX_Cycles_User_StartDate");
    }
}

internal sealed class CycleDayConfiguration : IEntityTypeConfiguration<CycleDay> {
    public void Configure(EntityTypeBuilder<CycleDay> entity) {
        entity.Property(e => e.Id).HasConversion(
            id => id.Value,
            value => new CycleDayId(value));

        entity.Property(e => e.CycleId).HasConversion(
            id => id.Value,
            value => new CycleId(value));

        entity.Property(e => e.Date)
            .HasColumnType("date");

        entity.Property(e => e.Notes)
            .HasMaxLength(1024);

        entity.HasIndex(e => new { e.CycleId, e.Date }).IsUnique();

        entity.OwnsOne(e => e.Symptoms, builder => {
            builder.Property(s => s.Pain).HasColumnName("Pain").IsRequired();
            builder.Property(s => s.Mood).HasColumnName("Mood").IsRequired();
            builder.Property(s => s.Edema).HasColumnName("Edema").IsRequired();
            builder.Property(s => s.Headache).HasColumnName("Headache").IsRequired();
            builder.Property(s => s.Energy).HasColumnName("Energy").IsRequired();
            builder.Property(s => s.SleepQuality).HasColumnName("SleepQuality").IsRequired();
            builder.Property(s => s.Libido).HasColumnName("Libido").IsRequired();
        });
    }
}

internal sealed class HydrationEntryConfiguration : IEntityTypeConfiguration<HydrationEntry> {
    public void Configure(EntityTypeBuilder<HydrationEntry> entity) {
        entity.Property(e => e.Id).HasConversion(
            id => id.Value,
            value => new HydrationEntryId(value));

        entity.Property(e => e.UserId).HasConversion(
            id => id.Value,
            value => new UserId(value));

        entity.Property(e => e.Timestamp)
            .HasColumnType("timestamp with time zone");

        entity.Property(e => e.AmountMl)
            .IsRequired();

        entity.HasIndex(e => new { e.UserId, e.Timestamp })
            .HasDatabaseName("IX_HydrationEntries_User_Timestamp");

        entity.HasOne(e => e.User)
            .WithMany(u => u.HydrationEntries)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class DailyAdviceConfiguration : IEntityTypeConfiguration<DailyAdvice> {
    public void Configure(EntityTypeBuilder<DailyAdvice> entity) {
        entity.Property(e => e.Id).HasConversion(
            id => id.Value,
            value => new DailyAdviceId(value));

        entity.Property(e => e.Locale)
            .IsRequired()
            .HasMaxLength(10);

        entity.Property(e => e.Value)
            .IsRequired()
            .HasMaxLength(512);

        entity.Property(e => e.Tag)
            .HasMaxLength(64);

        entity.Property(e => e.Weight)
            .HasDefaultValue(1);

        entity.HasIndex(e => new { e.Locale, e.Tag });
    }
}

internal sealed class AiUsageConfiguration : IEntityTypeConfiguration<AiUsage> {
    public void Configure(EntityTypeBuilder<AiUsage> entity) {
        entity.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new AiUsageId(value))
            .ValueGeneratedNever();

        entity.Property(e => e.UserId).HasConversion(
            id => id.Value,
            value => new UserId(value));

        entity.Property(e => e.Operation)
            .IsRequired()
            .HasMaxLength(32);

        entity.Property(e => e.Model)
            .IsRequired()
            .HasMaxLength(64);

        entity.HasOne<global::FoodDiary.Domain.Entities.Users.User>()
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasIndex(e => e.UserId);
        entity.HasIndex(e => new { e.UserId, e.CreatedOnUtc });
    }
}

internal sealed class FastingSessionConfiguration : IEntityTypeConfiguration<FastingSession> {
    public void Configure(EntityTypeBuilder<FastingSession> entity) {
        entity.Property<uint>("xmin").IsRowVersion();

        entity.Property(e => e.Id)
            .HasConversion(
                id => id.Value,
                value => new FastingSessionId(value))
            .ValueGeneratedNever();

        entity.Property(e => e.UserId).HasConversion(
            id => id.Value,
            value => new UserId(value));

        entity.Property(e => e.Protocol)
            .HasConversion<string>()
            .HasMaxLength(16);

        entity.Property(e => e.Notes)
            .HasMaxLength(500);

        entity.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasIndex(e => e.UserId);
        entity.HasIndex(e => new { e.UserId, e.IsCompleted });
    }
}

internal sealed class FavoriteMealConfiguration : IEntityTypeConfiguration<FavoriteMeal> {
    public void Configure(EntityTypeBuilder<FavoriteMeal> entity) {
        entity.Property(e => e.Id)
            .HasConversion(
                id => id.Value,
                value => new FavoriteMealId(value))
            .ValueGeneratedNever();

        entity.Property(e => e.UserId).HasConversion(
            id => id.Value,
            value => new UserId(value));

        entity.Property(e => e.MealId).HasConversion(
            id => id.Value,
            value => new MealId(value));

        entity.Property(e => e.Name)
            .HasMaxLength(500);

        entity.Property(e => e.CreatedAtUtc)
            .HasColumnType("timestamp with time zone");

        entity.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(e => e.Meal)
            .WithMany()
            .HasForeignKey(e => e.MealId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasIndex(e => new { e.UserId, e.MealId })
            .IsUnique();

        entity.HasIndex(e => e.UserId);
    }
}
