using FoodDiary.MailInbox.Application.Common.Result;
using FoodDiary.MailInbox.Application.Messages.Models;
using FoodDiary.MailInbox.Presentation.Extensions;
using FoodDiary.MailInbox.Presentation.Features.Health.Mappings;
using FoodDiary.MailInbox.Presentation.Features.Messages.Mappings;
using FoodDiary.MailInbox.Presentation.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.MailInbox.Tests;

public sealed class MailInboxPresentationTests {
    [Fact]
    public void InboundMailHttpMappings_ToQuery_DefaultsLimitToFifty() {
        int? limit = null;

        var query = limit.ToQuery();

        Assert.Equal(50, query.Limit);
    }

    [Fact]
    public void InboundMailHttpMappings_ToDetailsResponse_MapsDmarcPreview() {
        var id = Guid.NewGuid();
        var details = new InboundMailMessageDetails(
            id,
            "message-id",
            "sender@example.com",
            ["admin@fooddiary.club"],
            "DMARC",
            "text",
            "<p>html</p>",
            "raw",
            InboundMailMessageCategories.DmarcReport,
            new DmarcReportPreview(
                "google.com",
                "report-1",
                "fooddiary.club",
                DateTimeOffset.Parse("2026-05-01T00:00:00Z"),
                DateTimeOffset.Parse("2026-05-02T00:00:00Z"),
                [
                    new DmarcReportRecordPreview(
                        "192.0.2.1",
                        2,
                        "none",
                        "pass",
                        "pass",
                        "fooddiary.club",
                        "bounce.fooddiary.club",
                        "fooddiary.club",
                        "pass",
                        "fooddiary.club",
                        "pass"),
                ]),
            "Received",
            DateTimeOffset.UtcNow);

        var response = details.ToHttpResponse();

        Assert.Equal(id, response.Id);
        Assert.NotNull(response.DmarcReport);
        Assert.Equal("google.com", response.DmarcReport.OrganizationName);
        Assert.Equal("192.0.2.1", response.DmarcReport.Records.Single().SourceIp);
        Assert.Equal("pass", response.DmarcReport.Records.Single().DkimResult);
    }

    [Fact]
    public void MailInboxHealthMappings_ReturnExpectedStatusAndQuery() {
        Assert.Equal("ok", MailInboxHealthHttpMappings.ToHealthHttpResponse().Status);
        Assert.Equal("ready", MailInboxHealthHttpMappings.ToReadyHttpResponse().Status);
        Assert.NotNull(MailInboxHealthHttpMappings.ToReadinessQuery());
    }

    [Fact]
    public void MailInboxApiErrorDetailsMapper_ConvertsDottedPathSegmentsToCamelCase() {
        Assert.Equal("request.toRecipients.0.address", MailInboxApiErrorDetailsMapper.ToCamelCasePath("Request.ToRecipients.0.Address"));
        Assert.Equal("request", MailInboxApiErrorDetailsMapper.ToCamelCasePath(""));
    }

    [Theory]
    [InlineData(ErrorKind.Validation, StatusCodes.Status400BadRequest)]
    [InlineData(ErrorKind.Unauthorized, StatusCodes.Status401Unauthorized)]
    [InlineData(ErrorKind.NotFound, StatusCodes.Status404NotFound)]
    [InlineData(ErrorKind.Conflict, StatusCodes.Status409Conflict)]
    [InlineData(ErrorKind.ExternalFailure, StatusCodes.Status502BadGateway)]
    [InlineData(ErrorKind.Internal, StatusCodes.Status500InternalServerError)]
    public void MailInboxResultExtensions_ErrorResult_MapsErrorKindToStatusCode(ErrorKind kind, int expectedStatusCode) {
        var result = MailInboxResultExtensions.ErrorResult(
            new MailInboxError("code", "message", kind, new Dictionary<string, string[]> {
                ["Request.RawMime"] = ["Required"],
            }),
            "trace-123");

        var objectResult = Assert.IsType<ObjectResult>(result);
        var response = Assert.IsType<MailInboxApiErrorHttpResponse>(objectResult.Value);
        Assert.Equal(expectedStatusCode, objectResult.StatusCode);
        Assert.Equal("code", response.Error);
        Assert.Equal("trace-123", response.TraceId);
        Assert.NotNull(response.Errors);
        Assert.Equal(["Required"], response.Errors["Request.RawMime"]);
    }
}
