using FoodDiary.Modules.Wearables.Application.Abstractions.Models;
using FoodDiary.Modules.Wearables.Domain.Enums;

namespace FoodDiary.Modules.Wearables.Application.Common;

internal static class WearableSummaryCalculator {
    internal static WearableDailySummaryModel Calculate(DateTime date, IEnumerable<(WearableDataType DataType, double Value)> entries) {
        double? steps = null, heartRate = null, calories = null, active = null, sleep = null;

        foreach ((WearableDataType dataType, double value) in entries) {
            switch (dataType) {
                case WearableDataType.Steps: steps = (steps ?? 0) + value; break;
                case WearableDataType.HeartRate: heartRate = value; break;
                case WearableDataType.CaloriesBurned: calories = (calories ?? 0) + value; break;
                case WearableDataType.ActiveMinutes: active = (active ?? 0) + value; break;
                case WearableDataType.SleepMinutes: sleep = (sleep ?? 0) + value; break;
            }
        }

        return new WearableDailySummaryModel(date, steps, heartRate, calories, active, sleep);
    }
}
