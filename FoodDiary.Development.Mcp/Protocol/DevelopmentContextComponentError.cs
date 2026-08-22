using System.Diagnostics.CodeAnalysis;

namespace FoodDiary.Development.Mcp.Protocol;

[ExcludeFromCodeCoverage]
public sealed record DevelopmentContextComponentError(
    string Component,
    string ErrorCode,
    string Message);
