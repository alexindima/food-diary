namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
internal static class AsyncTestAwaiter {
    public static async Task WaitAsync(Task task, TimeSpan timeout, string failureMessage) {
        Task finished = await Task.WhenAny(task, Task.Delay(timeout)).ConfigureAwait(false);
        Assert.True(ReferenceEquals(task, finished), failureMessage);
        await task.ConfigureAwait(false);
    }
}
