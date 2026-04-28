namespace FoodDiary.Presentation.Api.Security;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class BlockImpersonatedAccessAttribute : Attribute;
