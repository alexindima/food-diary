using System.Text.Json;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;

namespace FoodDiary.Modules.Fasting.Application.Commands.RecordFastingTelemetry;

public sealed record RecordFastingTelemetryCommand(
    string Category,
    string Name,
    string? Timestamp,
    JsonElement? Details) : ICommand<Result>;
