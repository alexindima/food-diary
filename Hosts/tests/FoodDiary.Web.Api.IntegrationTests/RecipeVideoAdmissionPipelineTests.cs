using FoodDiary.Mediator;
using System.Net;
using FoodDiary.Modules.Ai.Contracts.Models;
using FoodDiary.Results;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class RecipeVideoAdmissionPipelineTests {
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task StalledMultipartUpload_AbortsAndReleasesAdmissionAsync(bool knownLength) {
        var clock = new UploadTimeProvider();
        var pipelineCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var original = new TransportApiWebApplicationFactory();
        await using WebApplicationFactory<Program> factory = original.WithWebHostBuilder(builder => builder.ConfigureTestServices(services => {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(clock);
            services.AddSingleton<IStartupFilter>(new UploadCompletionFilter(pipelineCompleted));
        }));
        using HttpClient client = factory.CreateClient();
        var userId = Guid.NewGuid();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.AuthenticateHeader, "true");
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, userId.ToString("D"));
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Premium");
        await using var body = new StalledMultipartBody();
        Task<HttpContext> request = factory.Server.SendAsync(context => {
            context.Request.Method = "POST";
            context.Request.Path = "/api/v1/ai/food/recipe-import/video";
            context.Request.ContentType = "multipart/form-data; boundary=boundary";
            context.Request.ContentLength = knownLength ? 4096 : null;
            context.Request.Body = body;
            context.Request.Headers[TestAuthenticationHandler.AuthenticateHeader] = "true";
            context.Request.Headers[TestAuthenticationHandler.UserIdHeader] = userId.ToString("D");
            context.Request.Headers[TestAuthenticationHandler.RoleHeader] = "Premium";
            context.Request.Headers.XRequestedWith = "XMLHttpRequest";
            context.Request.Headers[UploadCompletionFilter.Header] = "true";
        }, CancellationToken.None);
        try {
            await body.Started.Task.WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System, CancellationToken.None);
            Assert.True(clock.Created);
            clock.Fire();
            HttpContext? completed = null;
            Exception? failure = await Record.ExceptionAsync(async () =>
                completed = await request.WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System, CancellationToken.None));
            Assert.True(failure is HttpRequestException or OperationCanceledException or IOException || completed?.RequestAborted.IsCancellationRequested == true,
                $"Expected aborted upload, got {failure?.GetType().Name ?? "a response"}.");
            if (completed is not null) {
                Assert.NotEqual(StatusCodes.Status500InternalServerError, completed.Response.StatusCode);
            }
        } finally {
            body.Release();
            _ = await Record.ExceptionAsync(async () => _ = await request);
            // Client cancellation can complete before the resource filter releases admission.
            await pipelineCompleted.Task.WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System, CancellationToken.None);
        }
        // The same identity can enter again; missing idempotency is rejected after binding.
        using var retry = new MultipartFormDataContent { { new StringContent("Salad"), "text" } };
        using HttpResponseMessage retried = await client.PostAsync("/api/v1/ai/food/recipe-import/video", retry);
        Assert.Equal(HttpStatusCode.BadRequest, retried.StatusCode);
        Assert.Contains("Idempotency.Required", await retried.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task CompletedMultipartUpload_DisarmsDeadlineBeforeRecognitionAsync() {
        var clock = new UploadTimeProvider();
        var draft = new RecipeImportDraftModel("Salad", Description: null, [new("yoghurt", "180g")], ["Mix"],
            Servings: null, PrepMinutes: null, CookMinutes: null, AuthorNutrition: null, SourceUrl: null);
        ISender sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<IRequest<Result<RecipeImportDraftModel>>>(), Arg.Any<CancellationToken>())
            .Returns(call => {
                Assert.True(clock.Created);
                Assert.True(clock.Disposed);
                clock.Fire();
                call.Arg<CancellationToken>().ThrowIfCancellationRequested();
                return Task.FromResult(Result.Success(draft));
            });
        await using var original = new TransportApiWebApplicationFactory();
        await using WebApplicationFactory<Program> factory = original.WithWebHostBuilder(builder => builder.ConfigureTestServices(services => {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(clock);
            services.RemoveAll<ISender>();
            services.AddSingleton(sender);
        }));
        using HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.AuthenticateHeader, "true");
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, Guid.NewGuid().ToString("D"));
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Premium");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));
        using var content = new MultipartFormDataContent {
            { new ByteArrayContent([1, 2, 3]), "video", "recipe.mp4" },
            { new StringContent("caption"), "text" },
        };

        using HttpResponseMessage response = await client.PostAsync("/api/v1/ai/food/recipe-import/video", content);

        response.EnsureSuccessStatusCode();
        Assert.Contains("Salad", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [ExcludeFromCodeCoverage]
    private sealed class UploadCompletionFilter(TaskCompletionSource completion) : IStartupFilter {
        public const string Header = "X-Test-Stalled-Upload";

        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app => {
            app.Use(async (HttpContext context, RequestDelegate nextMiddleware) => {
                try {
                    await nextMiddleware(context);
                } finally {
                    if (context.Request.Headers.ContainsKey(Header)) {
                        completion.TrySetResult();
                    }
                }
            });
            next(app);
        };
    }

    [ExcludeFromCodeCoverage]
    private sealed class UploadTimeProvider : TimeProvider {
        private TimerCallback? _callback;
        private object? _state;
        public bool Created { get; private set; }
        public bool Disposed { get; private set; }
        public void Fire() {
            if (!Disposed) {
                _callback?.Invoke(_state);
            }
        }
        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period) {
            if (dueTime != TimeSpan.FromMinutes(2)) {
                return System.CreateTimer(callback, state, dueTime, period);
            }
            Created = true;
            Disposed = false;
            _callback = callback;
            _state = state;
            return new UploadTimer(this);
        }
        [ExcludeFromCodeCoverage]
        private sealed class UploadTimer(UploadTimeProvider owner) : ITimer {
            public bool Change(TimeSpan dueTime, TimeSpan period) => !owner.Disposed;
            public void Dispose() => owner.Disposed = true;
            public ValueTask DisposeAsync() {
                owner.Disposed = true;
                return ValueTask.CompletedTask;
            }
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class StalledMultipartBody : Stream {
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
        protected override void Dispose(bool disposing) {
            if (disposing) {
                _release.Cancel();
                _release.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
