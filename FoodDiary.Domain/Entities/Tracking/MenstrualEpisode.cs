using FoodDiary.Domain.Enums;
using FoodDiary.Domain.Primitives;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Entities.Tracking;

public sealed class MenstrualEpisode : Entity<MenstrualEpisodeId> {
    public CycleProfileId CycleProfileId { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime? EndDate { get; private set; }
    public MenstrualEpisodeStatus Status { get; private set; }
    public bool ExcludedFromPredictions { get; private set; }

    public CycleProfile CycleProfile { get; private set; } = null!;

    private MenstrualEpisode() {
    }

    private MenstrualEpisode(MenstrualEpisodeId id) : base(id) {
    }

    internal static MenstrualEpisode Create(
        CycleProfileId cycleProfileId,
        DateTime startDate,
        DateTime? endDate,
        MenstrualEpisodeStatus status,
        bool excludedFromPredictions = false) {
        if (cycleProfileId == CycleProfileId.Empty) {
            throw new ArgumentException("CycleProfileId is required.", nameof(cycleProfileId));
        }

        if (!Enum.IsDefined(status)) {
            throw new ArgumentOutOfRangeException(nameof(status), "Status must be one of the supported values.");
        }

        DateTime normalizedStart = CycleProfile.NormalizeDate(startDate);
        DateTime? normalizedEnd = endDate.HasValue ? CycleProfile.NormalizeDate(endDate.Value) : null;
        if (normalizedEnd < normalizedStart) {
            throw new ArgumentOutOfRangeException(nameof(endDate), "Episode end date cannot precede its start date.");
        }

        var episode = new MenstrualEpisode(MenstrualEpisodeId.New()) {
            CycleProfileId = cycleProfileId,
            StartDate = normalizedStart,
            EndDate = normalizedEnd,
            Status = status,
            ExcludedFromPredictions = excludedFromPredictions,
        };
        episode.SetCreated();
        return episode;
    }

    internal void UpdateInferredRange(DateTime endDate) {
        if (Status != MenstrualEpisodeStatus.Inferred) {
            return;
        }

        DateTime normalizedEnd = CycleProfile.NormalizeDate(endDate);
        if (normalizedEnd < StartDate) {
            throw new ArgumentOutOfRangeException(nameof(endDate), "Episode end date cannot precede its start date.");
        }

        EndDate = normalizedEnd;
        SetModified();
    }
}
