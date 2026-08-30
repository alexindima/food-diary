using FoodDiary.Domain.Enums;
using FoodDiary.Domain.Primitives;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Entities.Tracking;

public sealed class MenstrualEpisode : Entity<MenstrualEpisodeId> {
    public CycleProfileId CycleProfileId { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public MenstrualEpisodeStatus Status { get; private set; }
    public bool ExcludedFromPredictions { get; private set; }

    public CycleProfile CycleProfile { get; private set; } = null!;

    private MenstrualEpisode() {
    }

    private MenstrualEpisode(MenstrualEpisodeId id) : base(id) {
    }

    internal static MenstrualEpisode Create(
        CycleProfileId cycleProfileId,
        DateOnly startDate,
        DateOnly? endDate,
        MenstrualEpisodeStatus status,
        bool excludedFromPredictions = false) {
        if (cycleProfileId == CycleProfileId.Empty) {
            throw new ArgumentException("CycleProfileId is required.", nameof(cycleProfileId));
        }

        if (!Enum.IsDefined(status)) {
            throw new ArgumentOutOfRangeException(nameof(status), "Status must be one of the supported values.");
        }

        if (endDate < startDate) {
            throw new ArgumentOutOfRangeException(nameof(endDate), "Episode end date cannot precede its start date.");
        }

        var episode = new MenstrualEpisode(MenstrualEpisodeId.New()) {
            CycleProfileId = cycleProfileId,
            StartDate = startDate,
            EndDate = endDate,
            Status = status,
            ExcludedFromPredictions = excludedFromPredictions,
        };
        episode.SetCreated();
        return episode;
    }

    internal void UpdateInferredRange(DateOnly endDate) {
        if (Status != MenstrualEpisodeStatus.Inferred) {
            return;
        }

        if (endDate < StartDate) {
            throw new ArgumentOutOfRangeException(nameof(endDate), "Episode end date cannot precede its start date.");
        }

        EndDate = endDate;
        SetModified();
    }

    internal void UpdateConfirmedRange(DateOnly startDate, DateOnly? endDate) {
        if (Status != MenstrualEpisodeStatus.Confirmed) {
            throw new InvalidOperationException("Only confirmed menstrual episodes can be edited.");
        }

        if (endDate < startDate) {
            throw new ArgumentOutOfRangeException(nameof(endDate), "Episode end date cannot precede its start date.");
        }

        StartDate = startDate;
        EndDate = endDate;
        SetModified();
    }

    internal void SetPredictionExclusion(bool excludedFromPredictions) {
        if (Status != MenstrualEpisodeStatus.Confirmed) {
            throw new InvalidOperationException("Only confirmed menstrual episodes can be excluded from predictions.");
        }

        if (ExcludedFromPredictions == excludedFromPredictions) {
            return;
        }

        ExcludedFromPredictions = excludedFromPredictions;
        SetModified();
    }
}
