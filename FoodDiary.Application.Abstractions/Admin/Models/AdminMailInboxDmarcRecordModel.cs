namespace FoodDiary.Application.Abstractions.Admin.Models;

public sealed record AdminMailInboxDmarcRecordModel(
    string? SourceIp,
    int Count,
    string? Disposition,
    string? Dkim,
    string? Spf,
    string? HeaderFrom,
    string? EnvelopeFrom,
    string? DkimDomain,
    string? DkimResult,
    string? SpfDomain,
    string? SpfResult);
