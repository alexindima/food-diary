namespace FoodDiary.Modules.Export.Application.Queries.ExportDiary;

internal sealed record DiaryDisplayOffset {
    public TimeSpan Value { get; }
    private DiaryDisplayOffset(TimeSpan value) => Value = value;

    public static DiaryDisplayOffset FromResolvedValue(TimeSpan value) {
        if (value < TimeSpan.FromMinutes(-840) || value > TimeSpan.FromMinutes(840)) {
            throw new ArgumentOutOfRangeException(nameof(value));
        }
        return new DiaryDisplayOffset(value);
    }
}
