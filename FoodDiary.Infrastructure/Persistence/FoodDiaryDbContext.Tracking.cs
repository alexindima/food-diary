using FoodDiary.Domain.Entities.Content;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Entities.WeeklyGoals;
using FoodDiary.Domain.Entities.Tracking.Fasting;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence;

public sealed partial class FoodDiaryDbContext {
    public DbSet<WeightEntry> WeightEntries => Set<WeightEntry>();
    public DbSet<WeightGoal> WeightGoals => Set<WeightGoal>();
    public DbSet<WaistEntry> WaistEntries => Set<WaistEntry>();
    public DbSet<WaistGoal> WaistGoals => Set<WaistGoal>();
    public DbSet<WeeklyGoal> WeeklyGoals => Set<WeeklyGoal>();
    public DbSet<CycleProfile> CycleProfiles => Set<CycleProfile>();
    public DbSet<BleedingEntry> CycleBleedingEntries => Set<BleedingEntry>();
    public DbSet<CycleSymptomEntry> CycleSymptomEntries => Set<CycleSymptomEntry>();
    public DbSet<CycleFactor> CycleFactors => Set<CycleFactor>();
    public DbSet<FertilitySignal> FertilitySignals => Set<FertilitySignal>();
    public DbSet<MenstrualEpisode> CycleMenstrualEpisodes => Set<MenstrualEpisode>();
    public DbSet<HydrationEntry> HydrationEntries => Set<HydrationEntry>();
    public DbSet<DailyAdvice> DailyAdvices => Set<DailyAdvice>();
    public DbSet<FastingPlan> FastingPlans => Set<FastingPlan>();
    public DbSet<FastingOccurrence> FastingOccurrences => Set<FastingOccurrence>();
    public DbSet<FastingCheckIn> FastingCheckIns => Set<FastingCheckIn>();
    public DbSet<FastingSession> FastingSessions => Set<FastingSession>();
    public DbSet<FastingTelemetryEvent> FastingTelemetryEvents => Set<FastingTelemetryEvent>();
    public DbSet<MarketingAttributionEvent> MarketingAttributionEvents => Set<MarketingAttributionEvent>();
    public DbSet<ExerciseEntry> ExerciseEntries => Set<ExerciseEntry>();
}
