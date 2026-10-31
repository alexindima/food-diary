using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using FoodDiary.Modules.Admin.Application.Abstractions.Common;
using FoodDiary.Modules.Admin.Application.Abstractions.Models;
using Microsoft.Extensions.Options;

namespace FoodDiary.Modules.Admin.Infrastructure.Integrations.BugTriage;

internal sealed class AdminBugReportReader(HttpClient http, IOptions<AdminBugTriageOptions> options) : IAdminBugReportReader {
    public async Task<AdminBugReportPage> GetPageAsync(AdminBugReportFilter filter, CancellationToken cancellationToken) {
        AdminBugTriageOptions settings = options.Value;
        if (settings.BaseUrl.Length == 0) { return new AdminBugReportPage([], 0) { IsConfigured = false }; }
        string query = $"?page={filter.Page.ToString(CultureInfo.InvariantCulture)}&limit={filter.Limit.ToString(CultureInfo.InvariantCulture)}" +
            $"&fromUtc={Uri.EscapeDataString(filter.FromUtc?.ToString("O", CultureInfo.InvariantCulture) ?? "")}&toUtc={Uri.EscapeDataString(filter.ToUtc?.ToString("O", CultureInfo.InvariantCulture) ?? "")}" +
            $"&status={Uri.EscapeDataString(filter.Status ?? "")}&search={Uri.EscapeDataString(filter.Search ?? "")}&id={filter.Id}&source={Uri.EscapeDataString(filter.Source ?? "")}";
        using HttpResponseMessage response = await SendPageRequestAsync("api/report-journal/all" + query, cancellationToken);
        if (response.StatusCode is HttpStatusCode.NotFound) {
            using HttpResponseMessage legacy = await SendPageRequestAsync("api/report-journal" + query, cancellationToken);
            AdminBugReportPage page = await ReadPageAsync(legacy, cancellationToken);
            // Older providers contain only email reports and ignore the new source parameter.
            return filter.Source is "logs" ? new AdminBugReportPage([], 0) : page;
        }
        return await ReadPageAsync(response, cancellationToken);
    }

    private async Task<HttpResponseMessage> SendPageRequestAsync(string path, CancellationToken cancellationToken) {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add("X-BugTriage-Read-Key", options.Value.ReadApiKey);
        return await http.SendAsync(request, cancellationToken);
    }

    private static async Task<AdminBugReportPage> ReadPageAsync(HttpResponseMessage response, CancellationToken cancellationToken) {
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AdminBugReportPage>(cancellationToken)
            ?? throw new InvalidOperationException("Bug report journal returned an empty response.");
    }
}
