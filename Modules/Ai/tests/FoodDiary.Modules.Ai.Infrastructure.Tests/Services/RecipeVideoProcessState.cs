namespace FoodDiary.Modules.Ai.Infrastructure.Tests.Services;

[ExcludeFromCodeCoverage]
public sealed record RecipeVideoProcessState(int Pid, int? ChildPid, string InputPath, string OutputPath, string[] Arguments, int? DirectoryMode);
