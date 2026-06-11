using System.Globalization;
using System.Text;
using FoodDiary.Domain.Entities.Tracking;

namespace FoodDiary.Application.Export.Services;

public static class CycleCsvGenerator {
    public static byte[] Generate(CycleProfile profile, DateTime dateFrom, DateTime dateTo) {
        var sb = new StringBuilder();
        sb.AppendLine("RecordType,Date,EndDate,Category,Value,Flow,Intensity,TemperatureCelsius,OvulationTest,CervicalFluid,HadSex,Notes");
        AppendProfile(sb, profile);
        AppendBleedingEntries(sb, profile, dateFrom, dateTo);
        AppendSymptoms(sb, profile, dateFrom, dateTo);
        AppendFertilitySignals(sb, profile, dateFrom, dateTo);
        AppendFactors(sb, profile, dateFrom, dateTo);

        byte[] preamble = Encoding.UTF8.GetPreamble();
        byte[] content = Encoding.UTF8.GetBytes(sb.ToString());
        byte[] result = new byte[preamble.Length + content.Length];
        preamble.CopyTo(result, 0);
        content.CopyTo(result, preamble.Length);
        return result;
    }

    private static void AppendProfile(StringBuilder sb, CycleProfile profile) {
        AppendRow(
            sb,
            "Profile",
            profile.TrackingStartDate,
            endDate: null,
            category: "Mode",
            value: profile.Mode.ToString(),
            flow: null,
            intensity: null,
            temperatureCelsius: null,
            ovulationTest: null,
            cervicalFluid: null,
            hadSex: null,
            notes: profile.Notes);
    }

    private static void AppendBleedingEntries(StringBuilder sb, CycleProfile profile, DateTime dateFrom, DateTime dateTo) {
        foreach (BleedingEntry entry in profile.BleedingEntries.Where(entry => IsInRange(entry.Date, dateFrom, dateTo)).OrderBy(entry => entry.Date).ThenBy(entry => entry.Type)) {
            AppendRow(
                sb,
                "Bleeding",
                entry.Date,
                endDate: null,
                category: entry.Type.ToString(),
                value: null,
                flow: entry.Flow.ToString(),
                intensity: entry.PainImpact,
                temperatureCelsius: null,
                ovulationTest: null,
                cervicalFluid: null,
                hadSex: null,
                notes: entry.Notes);
        }
    }

    private static void AppendSymptoms(StringBuilder sb, CycleProfile profile, DateTime dateFrom, DateTime dateTo) {
        foreach (CycleSymptomEntry entry in profile.SymptomEntries.Where(entry => IsInRange(entry.Date, dateFrom, dateTo)).OrderBy(entry => entry.Date).ThenBy(entry => entry.Category)) {
            AppendRow(
                sb,
                "Symptom",
                entry.Date,
                endDate: null,
                category: entry.Category.ToString(),
                value: string.Join('|', entry.Tags),
                flow: null,
                intensity: entry.Intensity,
                temperatureCelsius: null,
                ovulationTest: null,
                cervicalFluid: null,
                hadSex: null,
                notes: entry.Note);
        }
    }

    private static void AppendFertilitySignals(StringBuilder sb, CycleProfile profile, DateTime dateFrom, DateTime dateTo) {
        foreach (FertilitySignal signal in profile.FertilitySignals.Where(signal => IsInRange(signal.Date, dateFrom, dateTo)).OrderBy(signal => signal.Date)) {
            AppendRow(
                sb,
                "FertilitySignal",
                signal.Date,
                endDate: null,
                category: null,
                value: null,
                flow: null,
                intensity: null,
                temperatureCelsius: signal.BasalBodyTemperatureCelsius,
                ovulationTest: signal.OvulationTestResult?.ToString(),
                cervicalFluid: signal.CervicalFluid,
                hadSex: signal.HadSex,
                notes: signal.Notes);
        }
    }

    private static void AppendFactors(StringBuilder sb, CycleProfile profile, DateTime dateFrom, DateTime dateTo) {
        foreach (CycleFactor factor in profile.Factors.Where(factor => OverlapsRange(factor.StartDate, factor.EndDate, dateFrom, dateTo)).OrderBy(factor => factor.StartDate).ThenBy(factor => factor.Type)) {
            AppendRow(
                sb,
                "Factor",
                factor.StartDate,
                factor.EndDate,
                category: factor.Type.ToString(),
                value: null,
                flow: null,
                intensity: null,
                temperatureCelsius: null,
                ovulationTest: null,
                cervicalFluid: null,
                hadSex: null,
                notes: factor.Notes);
        }
    }

    private static void AppendRow(
        StringBuilder sb,
        string recordType,
        DateTime date,
        DateTime? endDate,
        string? category,
        string? value,
        string? flow,
        int? intensity,
        double? temperatureCelsius,
        string? ovulationTest,
        string? cervicalFluid,
        bool? hadSex,
        string? notes) {
        string[] fields = [
            recordType,
            FormatDate(date),
            endDate.HasValue ? FormatDate(endDate.Value) : "",
            category ?? "",
            value ?? "",
            flow ?? "",
            intensity?.ToString(CultureInfo.InvariantCulture) ?? "",
            temperatureCelsius?.ToString("0.##", CultureInfo.InvariantCulture) ?? "",
            ovulationTest ?? "",
            cervicalFluid ?? "",
            hadSex?.ToString(CultureInfo.InvariantCulture) ?? "",
            notes ?? "",
        ];

        sb.AppendLine(string.Join(',', fields.Select(EscapeCsv)));
    }

    private static bool IsInRange(DateTime date, DateTime dateFrom, DateTime dateTo) =>
        date >= dateFrom.Date && date <= dateTo.Date;

    private static bool OverlapsRange(DateTime startDate, DateTime? endDate, DateTime dateFrom, DateTime dateTo) =>
        startDate <= dateTo.Date && (endDate is null || endDate.Value >= dateFrom.Date);

    private static string FormatDate(DateTime value) =>
        value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string EscapeCsv(string value) {
        if (string.IsNullOrEmpty(value)) {
            return "";
        }

        ReadOnlySpan<char> valueSpan = value.AsSpan();
        if (valueSpan.Contains('"') ||
            valueSpan.Contains(',') ||
            valueSpan.Contains('\n') ||
            valueSpan.Contains('\r')) {
            return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
        }

        return value;
    }
}
