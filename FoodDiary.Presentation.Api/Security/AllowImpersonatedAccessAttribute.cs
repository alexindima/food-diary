namespace FoodDiary.Presentation.Api.Security;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class AllowImpersonatedAccessAttribute : Attribute;
