namespace FoodDiary.Contracts.WaistEntries;

public record UpdateWaistEntryRequest(
    DateTime Date,
    double Circumference);
