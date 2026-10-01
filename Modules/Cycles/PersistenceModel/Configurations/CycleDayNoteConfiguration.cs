using FoodDiary.Modules.Cycles.Domain.Entities;
using FoodDiary.Modules.Cycles.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Modules.Cycles.PersistenceModel.Configurations;

internal sealed class CycleDayNoteConfiguration : IEntityTypeConfiguration<CycleDayNote> {
    public void Configure(EntityTypeBuilder<CycleDayNote> builder) {
        builder.ToTable("CycleDayNotes");
        builder.Property(entry => entry.Id).HasConversion(id => id.Value, value => new CycleDayNoteId(value));
        builder.Property(entry => entry.CycleProfileId).HasConversion(id => id.Value, value => new CycleProfileId(value));
        builder.Property(entry => entry.Date).HasColumnType("date");
        builder.Property(entry => entry.Notes).HasMaxLength(CycleProfile.MaxNotesLength).IsRequired();
        builder.HasIndex(entry => new { entry.CycleProfileId, entry.Date }).IsUnique();
    }
}
