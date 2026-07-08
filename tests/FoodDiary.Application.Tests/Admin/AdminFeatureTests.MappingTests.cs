using FoodDiary.Application.Admin.Mappings;
using FoodDiary.Domain.Entities.Social;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Admin.Models;

namespace FoodDiary.Application.Tests.Admin;

public partial class AdminFeatureTests {

    [Fact]
    public void AdminContentReportMappings_ToAdminModel_MapsDomainReport() {
        var userId = UserId.New();
        var targetId = Guid.NewGuid();
        var report = ContentReport.Create(userId, ReportTargetType.Recipe, targetId, " spam ");
        report.MarkReviewed(" reviewed ");

        AdminContentReportModel model = report.ToAdminModel();

        Assert.Equal(report.Id.Value, model.Id);
        Assert.Equal(userId.Value, model.ReporterId);
        Assert.Equal("Recipe", model.TargetType);
        Assert.Equal(targetId, model.TargetId);
        Assert.Equal("spam", model.Reason);
        Assert.Equal("Reviewed", model.Status);
        Assert.Equal("reviewed", model.AdminNote);
        Assert.Equal(report.CreatedOnUtc, model.CreatedAtUtc);
        Assert.Equal(report.ReviewedAtUtc, model.ReviewedAtUtc);
    }

}
