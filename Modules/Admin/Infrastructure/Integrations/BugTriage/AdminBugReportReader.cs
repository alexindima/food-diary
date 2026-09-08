using System.Globalization;
using System.Net.Http.Json;
using FoodDiary.Application.Abstractions.Admin.Common;
using FoodDiary.Application.Abstractions.Admin.Models;
using Microsoft.Extensions.Options;

namespace FoodDiary.Infrastructure.Integrations.BugTriage;

internal sealed class AdminBugReportReader(HttpClient http, IOptions<AdminBugTriageOptions> options) : IAdminBugReportReader {
    public async Task<AdminBugReportPage> GetPageAsync(AdminBugReportFilter filter, CancellationToken cancellationToken) {
        AdminBugTriageOptions settings = options.Value;
        if (settings.BaseUrl.Length == 0) { return new AdminBugReportPage([], 0) { IsConfigured = false }; }
        string query = $"api/report-journal?page={filter.Page.ToString(CultureInfo.InvariantCulture)}&limit={filter.Limit.ToString(CultureInfo.InvariantCulture)}" +
            $"&fromUtc={Uri.EscapeDataString(filter.FromUtc?.ToString("O", CultureInfo.InvariantCulture) ?? "")}&toUtc={Uri.EscapeDataString(filter.ToUtc?.ToString("O", CultureInfo.InvariantCulture) ?? "")}" +
            $"&status={Uri.EscapeDataString(filter.Status ?? "")}&search={Uri.EscapeDataString(filter.Search ?? "")}&id={filter.Id}";
        using var request = new HttpRequestMessage(HttpMethod.Get, query);
        request.Headers.Add("X-BugTriage-Read-Key", settings.ReadApiKey);
        using HttpResponseMessage response = await http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AdminBugReportPage>(cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Bug report journal returned an empty response.");
    }
}
