# Content Reports Module Guidelines

ContentReports owns report creation, moderation capabilities, its aggregate, persistence ports/model, and adapter. Preserve legacy CLR namespaces, `FoodDiary.Application.ContentReports` assembly identity, central migrations, HTTP contracts, and Admin authorization boundaries.

ReportStatus and ReportTargetType live in `Domain/Enums` with the stable
`FoodDiary.Domain.Enums` namespace and unchanged member values. Consumers using
these types reference ContentReports Domain explicitly. Preserve string EF
conversions and HTTP strings; moving the assembly owner requires coordinated
consumer rebuilds. Central FoodDiary.Domain must not reference this module.
