namespace FoodDiary.Modules.Users.Domain.Contracts.Privacy;

public static class PrivacyNoticeVersions {
    public const string Current = "2026-11-02";
    public const string LegacyUnversioned = "legacy-unversioned";
    // The AI purpose/text is unchanged in this release. A later notice version
    // does not inherit compatibility with unversioned historical acceptance.
    public const string LegacyCompatibleAiNotice = "2026-11-02";
}
