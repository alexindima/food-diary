using System.Buffers;
using System.Security.Cryptography;
using System.Text.Json;
using FoodDiary.Modules.MealPlanning.Application.ShoppingLists.Models;

namespace FoodDiary.Modules.MealPlanning.Application.ShoppingLists.Common;

/// <summary>Opaque content precondition for the exact persisted shopping-list snapshot.</summary>
public static class ShoppingListRevision {
    private const long PostgresEpochTicks = 630822816000000000L;

    public static bool IsValid(string? revision) =>
        revision is { Length: 64 } && revision.All(static character => character is >= '0' and <= '9' or >= 'a' and <= 'f');

    // Npgsql truncates ticks relative to 2000 towards zero, including dates before
    // that epoch. Min/Max have special infinity representations and stay exact.
    public static DateTime NormalizeTimestamp(DateTime timestamp) {
        long ticks = timestamp.Ticks;
        if (ticks != DateTime.MinValue.Ticks && ticks != DateTime.MaxValue.Ticks) {
            ticks = PostgresEpochTicks + ((ticks - PostgresEpochTicks) / 10 * 10);
        }
        return new DateTime(ticks, DateTimeKind.Utc);
    }

    public static DateTime? NormalizeTimestamp(DateTime? timestamp) =>
        timestamp.HasValue ? NormalizeTimestamp(timestamp.Value) : null;

    public static string Compute(ShoppingListModel list) {
        ArgumentNullException.ThrowIfNull(list);
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer)) {
            writer.WriteStartObject();
            writer.WriteNumber("format", 1);
            writer.WriteString("id", list.Id);
            writer.WriteString("name", list.Name);
            writer.WriteString("createdAt", NormalizeTimestamp(list.CreatedAt));
            writer.WriteStartArray("items");
            foreach (ShoppingListItemModel item in list.Items.OrderBy(static item => item.SortOrder).ThenBy(static item => item.Id)) {
                WriteItem(writer, item);
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return Convert.ToHexStringLower(SHA256.HashData(buffer.WrittenSpan));
    }

    private static void WriteItem(Utf8JsonWriter writer, ShoppingListItemModel item) {
        writer.WriteStartObject();
        writer.WriteString("id", item.Id);
        writer.WriteString("shoppingListId", item.ShoppingListId);
        WriteGuid(writer, "productId", item.ProductId);
        writer.WriteString("name", item.Name);
        WriteNumber(writer, "amount", item.Amount);
        writer.WriteString("unit", item.Unit);
        writer.WriteString("category", item.Category);
        writer.WriteString("aisle", item.Aisle);
        writer.WriteString("note", item.Note);
        writer.WriteBoolean("isChecked", item.IsChecked);
        if (item.CheckedOnUtc.HasValue) {
            writer.WriteString("checkedOnUtc", NormalizeTimestamp(item.CheckedOnUtc.Value));
        } else {
            writer.WriteNull("checkedOnUtc");
        }
        writer.WriteNumber("sortOrder", item.SortOrder);
        writer.WriteStartArray("sources");
        foreach (ShoppingListItemSourceModel source in item.Sources
                     .OrderBy(static source => source.DayNumber ?? int.MaxValue)
                     .ThenBy(static source => source.Label, StringComparer.Ordinal)
                     .ThenBy(static source => source.Id)) {
            WriteSource(writer, source);
        }
        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    private static void WriteSource(Utf8JsonWriter writer, ShoppingListItemSourceModel source) {
        writer.WriteStartObject();
        writer.WriteString("id", source.Id);
        writer.WriteString("sourceType", source.SourceType);
        WriteGuid(writer, "mealPlanId", source.MealPlanId);
        WriteGuid(writer, "mealPlanMealId", source.MealPlanMealId);
        WriteGuid(writer, "recipeId", source.RecipeId);
        writer.WriteString("label", source.Label);
        if (source.DayNumber.HasValue) {
            writer.WriteNumber("dayNumber", source.DayNumber.Value);
        } else {
            writer.WriteNull("dayNumber");
        }
        writer.WriteString("mealType", source.MealType);
        writer.WriteNumber("amount", source.Amount);
        writer.WriteString("unit", source.Unit);
        writer.WriteEndObject();
    }

    private static void WriteGuid(Utf8JsonWriter writer, string name, Guid? value) {
        if (value.HasValue) {
            writer.WriteString(name, value.Value);
        } else {
            writer.WriteNull(name);
        }
    }

    private static void WriteNumber(Utf8JsonWriter writer, string name, double? value) {
        if (value.HasValue) {
            writer.WriteNumber(name, value.Value);
        } else {
            writer.WriteNull(name);
        }
    }
}
