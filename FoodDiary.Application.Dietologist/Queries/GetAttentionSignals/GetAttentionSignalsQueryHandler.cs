using System.Globalization;
using FoodDiary.Application.Abstractions.Audit.Common;
using FoodDiary.Application.Abstractions.Audit.Models;
using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Application.Abstractions.Dietologist.Models;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Dietologist.Common;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Dietologist.Queries.GetAttentionSignals;

public sealed class GetAttentionSignalsQueryHandler(
    IDietologistInvitationReadService invitationReadService,
    IAttentionSignalMetricsReadService metricsReadService,
    IAuditEntryReadService auditReadService,
    ICurrentUserAccessService currentUserAccessService,
    TimeProvider timeProvider)
    : IQueryHandler<GetAttentionSignalsQuery, Result<IReadOnlyList<AttentionSignalModel>>> {
    public async Task<Result<IReadOnlyList<AttentionSignalModel>>> Handle(
        GetAttentionSignalsQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> dietologistIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (dietologistIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<IReadOnlyList<AttentionSignalModel>>(dietologistIdResult);
        }

        UserId dietologistId = dietologistIdResult.Value;
        Result<IReadOnlyList<ClientSummaryModel>> clientsResult = await invitationReadService.GetMyClientsAsync(
            dietologistId,
            cancellationToken).ConfigureAwait(false);
        if (clientsResult.IsFailure) {
            return Result.Failure<IReadOnlyList<AttentionSignalModel>>(clientsResult.Error);
        }

        DateTime nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        int lookbackDays = Math.Clamp(query.LookbackDays, 7, 90);
        IReadOnlyList<AttentionSignalMetricsReadModel> metrics = await metricsReadService.GetAsync(
            [.. clientsResult.Value.Select(client => new UserId(client.UserId))],
            nowUtc.Date.AddDays(-(lookbackDays - 1)),
            nowUtc.Date,
            cancellationToken).ConfigureAwait(false);
        IReadOnlyDictionary<Guid, AttentionSignalMetricsReadModel> metricsByClient =
            metrics.ToDictionary(item => item.ClientUserId);
        var signals = new List<AttentionSignalModel>();
        foreach (ClientSummaryModel client in clientsResult.Value) {
            if (metricsByClient.TryGetValue(client.UserId, out AttentionSignalMetricsReadModel? clientMetrics)) {
                AddClientSignals(signals, client, clientMetrics, query, nowUtc);
            }
        }

        IReadOnlyList<AuditEntryReadModel> states = await auditReadService.GetRecentAsync(
            subjectClientUserId: null,
            limit: 500,
            cancellationToken).ConfigureAwait(false);
        IReadOnlyList<AttentionSignalModel> visible = [
            .. signals
                .Where(signal => IsVisible(signal, dietologistId, states, nowUtc))
                .OrderByDescending(signal => SeverityOrder(signal.Severity))
                .ThenByDescending(signal => signal.DetectedAtUtc),
        ];
        return Result.Success(visible);
    }

    private static void AddClientSignals(
        ICollection<AttentionSignalModel> signals,
        ClientSummaryModel client,
        AttentionSignalMetricsReadModel metrics,
        GetAttentionSignalsQuery query,
        DateTime nowUtc) {
        if (client.Permissions.ShareMeals) {
            AddInactivitySignal(signals, client, metrics, Math.Clamp(query.InactivityDays, 1, 30), nowUtc);
        }

        if (client.Permissions.ShareStatistics) {
            AddCalorieSignal(
                signals,
                client,
                metrics,
                Math.Clamp(query.CalorieDeviationPercent, 5, 100),
                Math.Clamp(query.SustainedDays, 2, 14));
        }

        if (client.Permissions.ShareWeight) {
            AddWeightSignal(signals, client, metrics, Math.Clamp(query.WeightChangePercent, 0.5, 20));
        }
    }

    private static void AddInactivitySignal(
        ICollection<AttentionSignalModel> signals,
        ClientSummaryModel client,
        AttentionSignalMetricsReadModel metrics,
        int inactivityDays,
        DateTime nowUtc) {
        DateTime? lastMealUtc = metrics.LastMealAtUtc;
        double inactiveDays = lastMealUtc.HasValue
            ? (nowUtc - lastMealUtc.Value).TotalDays
            : (nowUtc - client.AcceptedAtUtc).TotalDays;
        if (inactiveDays < inactivityDays) {
            return;
        }

        DateTime detectedAt = lastMealUtc ?? client.AcceptedAtUtc;
        signals.Add(CreateSignal(
            client,
            "DiaryInactivity",
            inactiveDays >= inactivityDays * 2 ? "High" : "Medium",
            lastMealUtc.HasValue ? "NoRecentDiaryEntries" : "InsufficientDiaryData",
            detectedAt));
    }

    private static void AddCalorieSignal(
        ICollection<AttentionSignalModel> signals,
        ClientSummaryModel client,
        AttentionSignalMetricsReadModel metrics,
        double deviationPercent,
        int sustainedDays) {
        if (metrics.DailyCalorieTarget <= 0) {
            return;
        }

        AttentionSignalDailyCaloriesReadModel[] loggedDays = [
            .. metrics.DailyCalories
                .Where(day => day.Calories > 0)
                .OrderByDescending(day => day.Date)
                .Take(sustainedDays),
        ];
        if (loggedDays.Length < sustainedDays) {
            return;
        }

        bool sustained = loggedDays.All(day =>
            Math.Abs(day.Calories - metrics.DailyCalorieTarget) / metrics.DailyCalorieTarget * 100 >= deviationPercent);
        if (!sustained) {
            return;
        }

        double averageDeviation = loggedDays.Average(day =>
            Math.Abs(day.Calories - metrics.DailyCalorieTarget) / metrics.DailyCalorieTarget * 100);
        signals.Add(CreateSignal(
            client,
            "CalorieTargetDeviation",
            averageDeviation >= deviationPercent * 1.5 ? "High" : "Medium",
            "SustainedCalorieTargetDeviation",
            loggedDays.Max(day => day.Date)));
    }

    private static void AddWeightSignal(
        ICollection<AttentionSignalModel> signals,
        ClientSummaryModel client,
        AttentionSignalMetricsReadModel metrics,
        double changePercent) {
        AttentionSignalWeightPointReadModel[] points = [
            .. metrics.WeightPoints
                .Where(point => point.WeightKg > 0)
                .OrderBy(point => point.Date),
        ];
        if (points.Length < 2) {
            return;
        }

        double percent = Math.Abs(points[^1].WeightKg - points[0].WeightKg) /
                         points[0].WeightKg * 100;
        if (percent < changePercent) {
            return;
        }

        signals.Add(CreateSignal(
            client,
            "MaterialWeightChange",
            percent >= changePercent * 1.5 ? "High" : "Medium",
            "MaterialWeightChange",
            points[^1].Date));
    }

    private static AttentionSignalModel CreateSignal(
        ClientSummaryModel client,
        string type,
        string severity,
        string reason,
        DateTime detectedAtUtc) {
        string displayName = string.Join(
            ' ',
            new[] { client.FirstName, client.LastName }.Where(value => !string.IsNullOrWhiteSpace(value)));
        if (string.IsNullOrWhiteSpace(displayName)) {
            displayName = client.Email;
        }

        string id = string.Create(
            CultureInfo.InvariantCulture,
            $"{type}:{client.UserId:N}:{detectedAtUtc:yyyyMMdd}");
        return new AttentionSignalModel(
            id,
            client.UserId,
            displayName,
            type,
            severity,
            reason,
            detectedAtUtc,
            SnoozedUntilUtc: null);
    }

    private static bool IsVisible(
        AttentionSignalModel signal,
        UserId dietologistId,
        IReadOnlyList<AuditEntryReadModel> states,
        DateTime nowUtc) {
        AuditEntryReadModel? state = states.FirstOrDefault(entry =>
            entry.ActorUserId == dietologistId.Value &&
            string.Equals(entry.TargetType, "AttentionSignal", StringComparison.Ordinal) &&
            string.Equals(entry.TargetId, signal.Id, StringComparison.Ordinal) &&
            (string.Equals(entry.Action, "dietologist.attention.acknowledged", StringComparison.Ordinal) ||
             string.Equals(entry.Action, "dietologist.attention.snoozed", StringComparison.Ordinal)));
        if (string.Equals(state?.Action, "dietologist.attention.acknowledged", StringComparison.Ordinal)) {
            return false;
        }

        return !string.Equals(state?.Action, "dietologist.attention.snoozed", StringComparison.Ordinal) ||
               !DateTime.TryParse(
                   state?.Metadata,
                   CultureInfo.InvariantCulture,
                   DateTimeStyles.RoundtripKind,
                   out DateTime snoozedUntilUtc) ||
               snoozedUntilUtc <= nowUtc;
    }

    private static int SeverityOrder(string severity) => severity switch {
        "High" => 2,
        "Medium" => 1,
        _ => 0,
    };
}
