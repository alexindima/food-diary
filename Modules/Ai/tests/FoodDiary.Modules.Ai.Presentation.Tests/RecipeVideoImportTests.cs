using System.Reflection;
using System.Security.Claims;
using System.Text;
using FoodDiary.Mediator;
using FoodDiary.Modules.Ai.Application.Commands.ImportRecipeVideo;
using FoodDiary.Modules.Ai.Contracts.Models;
using FoodDiary.Modules.Ai.Presentation.Recipes;
using FoodDiary.Modules.Ai.Presentation.Recipes.Controllers;
using FoodDiary.Modules.Ai.Presentation.Responses;
using FoodDiary.Presentation.Api.Filters;
using FoodDiary.Presentation.Api.Tests;
using FoodDiary.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Ai.Presentation.Tests;

[ExcludeFromCodeCoverage]
public sealed class RecipeVideoImportTests {
    [Fact]
    public async Task Upload_SendsOpenStreamToApplication_AndReturnsRecipeDraft() {
        var draft = new RecipeImportDraftModel("Salad", Description: null, [new("yoghurt", "180g")], ["Mix"], Servings: null, PrepMinutes: null, CookMinutes: null, AuthorNutrition: null, SourceUrl: null);
        ImportRecipeVideoCommand? command = null;
        byte[]? received = null;
        ISender sender = SubstituteSender.Create(Result.Success(draft), request => {
            command = Assert.IsType<ImportRecipeVideoCommand>(request);
            Assert.NotNull(command.Video);
            using var copied = new MemoryStream();
            command.Video.CopyTo(copied);
            received = copied.ToArray();
        });
        var http = new DefaultHttpContext();
        typeof(IdempotencyRequestContext).GetMethod("SetRequestId", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, [http, new string('A', 64)]);
        var controller = new RecipeVideoImportController(sender) { ControllerContext = new ControllerContext { HttpContext = http } };
        await using var original = new MemoryStream([1, 2, 3]);
        var file = new FormFile(original, 0, 3, "video", "user-filename.mp4");
        IActionResult result = await controller.ImportRecipeVideo(Guid.NewGuid(), file, sourceUrl: null, "caption");
        RecipeImportHttpResponse response = Assert.IsType<RecipeImportHttpResponse>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.Multiple(() => Assert.Equal("Salad", response.Name), () => Assert.Equal(new byte[] { 1, 2, 3 }, received), () => Assert.Equal("caption", command!.Text));
    }

    [Fact]
    public async Task Admission_RejectsThirdRequestBeforeBinding_AndReleasesSlots() {
        var filter = new RecipeVideoAdmissionAttribute();
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int admitted = 0;
        ResourceExecutingContext first = CreateResourceContext();
        ResourceExecutingContext second = CreateResourceContext();
        async Task<ResourceExecutedContext> HoldAsync(ResourceExecutingContext context) {
            if (Interlocked.Increment(ref admitted) == 2) {
                entered.TrySetResult();
            }
            await release.Task;
            return new ResourceExecutedContext(context, []);
        }
        Task firstRequest = filter.OnResourceExecutionAsync(first, () => HoldAsync(first));
        Task secondRequest = filter.OnResourceExecutionAsync(second, () => HoldAsync(second));
        try {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None);
            ResourceExecutingContext third = CreateResourceContext();
            bool bound = false;
            await filter.OnResourceExecutionAsync(third, () => {
                bound = true;
                return Task.FromResult(new ResourceExecutedContext(third, []));
            });
            Assert.Multiple(() => Assert.False(bound), () => Assert.Equal(StatusCodes.Status429TooManyRequests, Assert.IsType<ObjectResult>(third.Result).StatusCode));
        } finally {
            release.TrySetResult();
            await Task.WhenAll(firstRequest, secondRequest);
        }
        ResourceExecutingContext retry = CreateResourceContext();
        await filter.OnResourceExecutionAsync(retry, () => Task.FromResult(new ResourceExecutedContext(retry, [])));
        Assert.Null(retry.Result);
    }

    [Theory]
    [InlineData("sub")]
    [InlineData("nameid")]
    public async Task Admission_OneUserCannotOccupyBothSlots(string secondClaimType) {
        var filter = new RecipeVideoAdmissionAttribute();
        var userId = Guid.NewGuid();
        ResourceExecutingContext first = CreateResourceContext(userId);
        ResourceExecutingContext duplicate = CreateResourceContext(userId, claimType: secondClaimType);
        ResourceExecutingContext other = CreateResourceContext();
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task firstRequest = filter.OnResourceExecutionAsync(first, async () => {
            await release.Task;
            return new ResourceExecutedContext(first, []);
        });
        try {
            bool duplicateBound = false;
            await filter.OnResourceExecutionAsync(duplicate, () => {
                duplicateBound = true;
                return Task.FromResult(new ResourceExecutedContext(duplicate, []));
            });
            Assert.False(duplicateBound, "The same user must not occupy both video admission slots.");
            Assert.Equal(StatusCodes.Status429TooManyRequests, Assert.IsType<ObjectResult>(duplicate.Result).StatusCode);
            bool otherBound = false;
            await filter.OnResourceExecutionAsync(other, () => {
                otherBound = true;
                return Task.FromResult(new ResourceExecutedContext(other, []));
            });
            Assert.True(otherBound);
        } finally {
            release.TrySetResult();
            await firstRequest;
        }
        ResourceExecutingContext retry = CreateResourceContext(userId, TimeProvider.System);
        await filter.OnResourceExecutionAsync(retry, () => Task.FromResult(new ResourceExecutedContext(retry, [])));
        Assert.Null(retry.Result);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Admission_StalledMultipartReadHasDeadlineAndReleasesCapacity(bool knownLength) {
        var clock = new DeadlineTimeProvider();
        var filter = new RecipeVideoAdmissionAttribute();
        var userId = Guid.NewGuid();
        ResourceExecutingContext context = CreateResourceContext(userId, clock);
        await using var body = new StalledBody();
        context.HttpContext.Request.ContentType = "multipart/form-data; boundary=boundary";
        context.HttpContext.Request.ContentLength = knownLength ? 100 : null;
        context.HttpContext.Request.Body = body;
        bool actionReached = false;
        Task request = filter.OnResourceExecutionAsync(context, async () => {
            // MVC's form value provider intentionally calls the parameterless overload.
            await context.HttpContext.Request.ReadFormAsync(CancellationToken.None);
            actionReached = true;
            return new ResourceExecutedContext(context, []);
        });
        try {
            await body.Started.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System, CancellationToken.None);
            Assert.NotNull(clock.Timer);
            clock.Timer.Fire();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                request.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System, CancellationToken.None));
            Assert.False(actionReached);
        } finally {
            body.Release();
            try {
                await request;
            } catch (OperationCanceledException) {
            }
        }
        ResourceExecutingContext retry = CreateResourceContext(userId, TimeProvider.System);
        await filter.OnResourceExecutionAsync(retry, () => Task.FromResult(new ResourceExecutedContext(retry, [])));
        Assert.Null(retry.Result);
    }

    [Fact]
    public async Task Admission_CompletedFormDoesNotTimeOutRecognition() {
        var clock = new DeadlineTimeProvider();
        var filter = new RecipeVideoAdmissionAttribute();
        ResourceExecutingContext context = CreateResourceContext(clock: clock);
        context.HttpContext.Request.ContentType = "multipart/form-data; boundary=boundary";
        context.HttpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(
            "--boundary\r\nContent-Disposition: form-data; name=\"text\"\r\n\r\nSalad\r\n--boundary--\r\n"));

        await filter.OnResourceExecutionAsync(context, async () => {
            IFormCollection form = await context.HttpContext.Request.ReadFormAsync(CancellationToken.None);
            Assert.Equal("Salad", form["text"]);
            async Task<ActionExecutedContext> RecognizeAsync() {
                clock.Timer?.Fire();
                Assert.False(context.HttpContext.RequestAborted.IsCancellationRequested);
                return await Task.FromResult(new ActionExecutedContext(context, [], new object()));
            }
            if ((object)filter is IAsyncActionFilter actionFilter) {
                await actionFilter.OnActionExecutionAsync(new ActionExecutingContext(context, [], new Dictionary<string, object?>(StringComparer.Ordinal), new object()), RecognizeAsync);
            } else {
                await RecognizeAsync();
            }
            return new ResourceExecutedContext(context, []);
        });
        Assert.Null(context.Result);
    }

    private static ResourceExecutingContext CreateResourceContext(Guid? userId = null, TimeProvider? clock = null,
        string claimType = ClaimTypes.NameIdentifier) {
        var http = new DefaultHttpContext {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(claimType, (userId ?? Guid.NewGuid()).ToString("D"))], "Test")),
            RequestServices = new ServiceCollection().AddSingleton(clock ?? TimeProvider.System).BuildServiceProvider(),
        };
        return new ResourceExecutingContext(new ActionContext(http, new RouteData(), new ActionDescriptor()), [], []);
    }

    [ExcludeFromCodeCoverage]
    private sealed class DeadlineTimeProvider : TimeProvider {
        public DeadlineTimer? Timer { get; private set; }
        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period) =>
            Timer = new DeadlineTimer(callback, state);
    }

    [ExcludeFromCodeCoverage]
    private sealed class DeadlineTimer(TimerCallback callback, object? state) : ITimer {
        private bool _disposed;
        public void Fire() {
            if (!_disposed) {
                callback(state);
            }
        }
        public bool Change(TimeSpan dueTime, TimeSpan period) => !_disposed;
        public void Dispose() => _disposed = true;
        public ValueTask DisposeAsync() {
            _disposed = true;
            return ValueTask.CompletedTask;
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class StalledBody : Stream {
        private readonly CancellationTokenSource _release = new();
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public void Release() => _release.Cancel();
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) {
            Started.TrySetResult();
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _release.Token);
            await Task.Delay(Timeout.InfiniteTimeSpan, linked.Token);
            return 0;
        }
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
