namespace FoodDiary.Modules.Images.Service.Contracts.Common;

public union DeleteImageAssetResult(DeleteImageAssetResult.Removed, DeleteImageAssetResult.Refused) {
    public static DeleteImageAssetResult Success { get; } = new Removed();

    public bool Deleted => this is Removed;
    public string? ErrorCode => this is Refused refused ? refused.Code : null;

    public sealed record Removed;
    public sealed record Refused(string? Code = null);
}
