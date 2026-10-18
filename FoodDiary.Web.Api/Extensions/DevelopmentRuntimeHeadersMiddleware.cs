using System.Net;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text;

namespace FoodDiary.Web.Api.Extensions;

/// <summary>Local diagnostics identify running binaries, independently from the source checkout.</summary>
public sealed class DevelopmentRuntimeHeadersMiddleware(RequestDelegate next, IWebHostEnvironment environment) {
    public const string BuildHeader = "X-FoodDiary-Api-Build";
    public const string InstanceHeader = "X-FoodDiary-Api-Instance";
    private readonly string _instanceId = Guid.NewGuid().ToString("D");
    private readonly string? _binaryFingerprint = environment.IsDevelopment() ? CaptureBinaryFingerprint() : null;

    public Task InvokeAsync(HttpContext context) {
        bool publish = environment.IsDevelopment()
            && context.Connection.RemoteIpAddress is { } address
            && IPAddress.IsLoopback(address)
            && context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase);
        void ApplyHeaders() {
            context.Response.Headers.Remove(BuildHeader);
            context.Response.Headers.Remove(InstanceHeader);
            if (publish && _binaryFingerprint is { } fingerprint) {
                context.Response.Headers[BuildHeader] = fingerprint;
                context.Response.Headers[InstanceHeader] = _instanceId;
            }
        }
        ApplyHeaders();
        context.Response.OnStarting(() => {
            // Run after downstream/cache callbacks, so cached headers cannot identify another process or leak outside the local gate.
            ApplyHeaders();
            return Task.CompletedTask;
        });
        return next(context);
    }

    private static string? CaptureBinaryFingerprint() {
        try {
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            bool foundAssembly = false;
            foreach (string path in Directory.EnumerateFiles(AppContext.BaseDirectory, "FoodDiary*.dll").Order(StringComparer.Ordinal)) {
                foundAssembly = true;
                using FileStream stream = File.OpenRead(path);
                using var reader = new PEReader(stream);
                MetadataReader metadata = reader.GetMetadataReader();
                Guid moduleId = metadata.GetGuid(metadata.GetModuleDefinition().Mvid);
                hash.AppendData(Encoding.UTF8.GetBytes(Path.GetFileName(path)));
                hash.AppendData(moduleId.ToByteArray());
            }
            return foundAssembly ? Convert.ToHexStringLower(hash.GetHashAndReset()) : null;
        } catch (Exception error) when (error is IOException or UnauthorizedAccessException or BadImageFormatException) {
            // Optional diagnostics must never break application requests.
            return null;
        }
    }
}
