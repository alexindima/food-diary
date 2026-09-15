using FoodDiary.Mediator;
namespace FoodDiary.Modules.Images.Service.Contracts.Commands.CleanupOrphanImages;
/// <summary>Scans all eligible candidates in bounded pages and returns the total number deleted.</summary>
public sealed record CleanupOrphanImagesCommand(DateTime OlderThanUtc, int BatchSize) : IRequest<int>;
