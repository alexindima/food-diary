namespace FoodDiary.Modules.Export.Application.Queries.ExportCycle;

internal sealed record CycleExportPeriod {
    public const int MaxRangeDays = 366;
    public DateOnly From { get; }
    public DateOnly To { get; }

    private CycleExportPeriod(DateOnly from, DateOnly to) {
        From = from;
        To = to;
    }

    public static CycleExportPeriod FromDates(DateOnly from, DateOnly to) {
        if (from > to || to.DayNumber - from.DayNumber > MaxRangeDays) {
            throw new ArgumentException("Invalid cycle export calendar period.", nameof(from));
        }
        return new CycleExportPeriod(from, to);
    }
}
