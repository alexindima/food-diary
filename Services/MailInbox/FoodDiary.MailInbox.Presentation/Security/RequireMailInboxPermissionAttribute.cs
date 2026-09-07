namespace FoodDiary.MailInbox.Presentation.Security;

[AttributeUsage(AttributeTargets.Method)]
public sealed class RequireMailInboxPermissionAttribute(MailInboxPermission permission) : Attribute {
    public MailInboxPermission Permission { get; } = permission;
}
