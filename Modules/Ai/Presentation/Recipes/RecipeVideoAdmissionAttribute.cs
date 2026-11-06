using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Presentation.Api.Extensions;
using FoodDiary.Presentation.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Ai.Presentation.Recipes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RecipeVideoAdmissionAttribute : Attribute, IAsyncResourceFilter, IAsyncActionFilter {
    private static readonly Lock AdmissionLock = new();
    private static readonly HashSet<Guid> ActiveUsers = [];
    private static readonly TimeSpan UploadTimeout = TimeSpan.FromMinutes(2);

    public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next) {
        Guid userId = context.HttpContext.User.GetUserGuid() ?? throw new CurrentUserUnavailableException();
        context.HttpContext.RequestAborted.ThrowIfCancellationRequested();
        bool admitted;
        // The set is bounded by the global limit and the identity is normalized across claim aliases.
        lock (AdmissionLock) {
            admitted = ActiveUsers.Count < 2 && ActiveUsers.Add(userId);
        }
        if (!admitted) {
            context.Result = new ObjectResult(new ApiErrorHttpResponse(
                "Ai.VideoBusy", "Video recognition is busy. Try again shortly.", context.HttpContext.TraceIdentifier)) {
                StatusCode = StatusCodes.Status429TooManyRequests,
            };
            return;
        }
        try {
            using var upload = new UploadDeadline(context.HttpContext,
                context.HttpContext.RequestServices.GetRequiredService<TimeProvider>());
            // FormFeature aborts the request when this token expires. Let MVC retain
            // its normal cancellation/exception handling, including wrapped contexts.
            await next();
        } finally {
            lock (AdmissionLock) {
                ActiveUsers.Remove(userId);
            }
        }
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next) {
        // Model binding is complete. Upload time must not consume the recognition deadline.
        if (context.HttpContext.Items.TryGetValue(typeof(UploadDeadline), out object? value) && value is UploadDeadline upload) {
            upload.FinishBinding();
        }
        await next();
    }

    private sealed class UploadDeadline : IFormFeature, IDisposable {
        private readonly HttpContext _context;
        private readonly IFormFeature _inner;
        private readonly CancellationTokenSource _timeout;
        private readonly CancellationTokenSource _cancellation;
        private readonly CancellationTokenRegistration _abortUpload;

        public UploadDeadline(HttpContext context, TimeProvider timeProvider) {
            _context = context;
            _inner = context.Features.Get<IFormFeature>() ?? new FormFeature(context.Request);
            _timeout = new CancellationTokenSource(UploadTimeout, timeProvider);
            _cancellation = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted, _timeout.Token);
            _abortUpload = _timeout.Token.Register(static state => ((HttpContext)state!).Abort(), context);
            context.Features.Set<IFormFeature>(this);
            context.Items[typeof(UploadDeadline)] = this;
        }

        public bool HasFormContentType => _inner.HasFormContentType;
        public IFormCollection? Form { get => _inner.Form; set => _inner.Form = value; }

        public IFormCollection ReadForm() => ReadFormAsync(CancellationToken.None).GetAwaiter().GetResult();
        public Task<IFormCollection> ReadFormAsync() => ReadFormAsync(CancellationToken.None);

        public async Task<IFormCollection> ReadFormAsync(CancellationToken cancellationToken) {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancellation.Token);
            return await _inner.ReadFormAsync(linked.Token);
        }

        public void FinishBinding() {
            _timeout.Dispose();
            _abortUpload.Dispose();
        }

        public void Dispose() {
            _context.Features.Set(_inner);
            _context.Items.Remove(typeof(UploadDeadline));
            FinishBinding();
            _cancellation.Dispose();
        }
    }
}
