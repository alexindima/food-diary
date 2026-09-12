using FoodDiary.Application.Admin.Mappings;
using FoodDiary.Application.ContentReports.Models;
using FoodDiary.Application.Admin.Models;

namespace FoodDiary.Application.Tests.Admin;

public partial class AdminFeatureTests {

    [Fact]
    public void AdminContentReportMappings_ToAdminModel_MapsOwnerReadModel() {
        var userId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var report = new ContentReportAdminReadModel(
            Guid.NewGuid(), userId, "Recipe", targetId, "spam", "Reviewed", "reviewed",
            DateTime.UtcNow.AddDays(-1), DateTime.UtcNow, Guid.NewGuid(), "Recipe title", "Recipe excerpt");

        AdminContentReportModel model = report.ToAdminModel();

        Assert.Equal(report.Id, model.Id);
        Assert.Equal(userId, model.ReporterId);
        Assert.Equal("Recipe", model.TargetType);
        Assert.Equal(targetId, model.TargetId);
        Assert.Equal("spam", model.Reason);
        Assert.Equal("Reviewed", model.Status);
        Assert.Equal("reviewed", model.AdminNote);
        Assert.Equal(report.CreatedOnUtc, model.CreatedAtUtc);
        Assert.Equal(report.ReviewedAtUtc, model.ReviewedAtUtc);
        Assert.Equal(report.ReviewedByUserId, model.ReviewedByUserId);
        Assert.Equal(report.TargetTitle, model.TargetTitle);
        Assert.Equal(report.TargetExcerpt, model.TargetExcerpt);
    }

}
