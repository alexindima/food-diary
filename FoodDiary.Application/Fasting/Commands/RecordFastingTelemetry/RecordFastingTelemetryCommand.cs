using System.Text.Json;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Abstractions.Messaging;

namespace FoodDiary.Application.Fasting.Commands.RecordFastingTelemetry;

public sealed record RecordFastingTelemetryCommand(
    string Category,
    string Name,
    string? Timestamp,
    JsonElement? Details) : ICommand<Result>;
