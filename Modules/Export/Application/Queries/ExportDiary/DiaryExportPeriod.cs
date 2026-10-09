namespace FoodDiary.Modules.Export.Application.Queries.ExportDiary;

internal sealed record DiaryExportPeriod {
    public const int MaxRangeDays = 366;
    public DateTime From { get; }
    public DateTime To { get; }
    public DiaryDisplayOffset DisplayOffset { get; }

    private DiaryExportPeriod(DateTime from, DateTime to, DiaryDisplayOffset displayOffset) {
        From = from;
        To = to;
        DisplayOffset = displayOffset;
    }

    public static DiaryExportPeriod FromResolvedRange(DateTime from, DateTime to, DiaryDisplayOffset displayOffset) {
        ArgumentNullException.ThrowIfNull(displayOffset);
        if (from > to || (to - from).TotalDays > MaxRangeDays) {
            throw new ArgumentException("Invalid diary export period.", nameof(from));
        }
        _ = from.Add(displayOffset.Value);
        _ = to.Add(displayOffset.Value);
        return new DiaryExportPeriod(from, to, displayOffset);
    }
}
