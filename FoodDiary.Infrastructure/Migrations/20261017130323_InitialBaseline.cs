using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations {
    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public partial class InitialBaseline : Migration {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.Sql("""
                DO $guard$
                BEGIN
                    IF EXISTS (SELECT 1 FROM public."__EFMigrationsHistory") THEN
                        RAISE EXCEPTION 'Legacy migration history detected. Apply scripts/database/Adopt-InitialBaseline.sql before updating this database.';
                    END IF;
                END $guard$;
                """);

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,");

            migrationBuilder.CreateTable(
                name: "AchievementDefinitions",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Metric = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Threshold = table.Column<int>(type: "integer", nullable: false),
                    TitleRu = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    TitleEn = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    DescriptionRu = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DescriptionEn = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Icon = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => table.PrimaryKey("PK_AchievementDefinitions", x => x.Id));

            migrationBuilder.CreateTable(
                name: "AchievementEvaluationOutbox",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NextAttemptOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    ProcessedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeadLetteredOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LockedUntilUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LockedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    LastError = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    Revision = table.Column<long>(type: "bigint", nullable: false),
                },
                constraints: table => table.PrimaryKey("PK_AchievementEvaluationOutbox", x => x.Id));

            migrationBuilder.CreateTable(
                name: "AiPromptTemplates",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Locale = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    PromptText = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => table.PrimaryKey("PK_AiPromptTemplates", x => x.Id));

            migrationBuilder.CreateTable(
                name: "AtomicCommandReceipts",
                columns: table => new {
                    Key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ResponseType = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    ResponseJson = table.Column<string>(type: "jsonb", nullable: false),
                    ExpiresOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table => table.PrimaryKey("PK_AtomicCommandReceipts", x => x.Key));

            migrationBuilder.CreateTable(
                name: "AuditEntries",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectClientUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Action = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    TargetType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    TargetId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Metadata = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table => table.PrimaryKey("PK_AuditEntries", x => x.Id));

            migrationBuilder.CreateTable(
                name: "BillingWebhookEvents",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    EventId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    EventType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ExternalObjectId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ReceivedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    NextAttemptAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PayloadJson = table.Column<string>(type: "jsonb", nullable: true),
                    ParsedEventJson = table.Column<string>(type: "jsonb", nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => table.PrimaryKey("PK_BillingWebhookEvents", x => x.Id));

            migrationBuilder.CreateTable(
                name: "BugAcknowledgementReceipts",
                columns: table => new {
                    InboxId = table.Column<Guid>(type: "uuid", nullable: false),
                },
                constraints: table => table.PrimaryKey("PK_BugAcknowledgementReceipts", x => x.InboxId));

            migrationBuilder.CreateTable(
                name: "ConsumedTelegramAssertions",
                columns: table => new {
                    Fingerprint = table.Column<string>(type: "character varying(67)", maxLength: 67, nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table => {
                    table.PrimaryKey("PK_ConsumedTelegramAssertions", x => x.Fingerprint);
                    table.CheckConstraint("CK_ConsumedTelegramAssertions_V2", "\"Fingerprint\" LIKE 'v2:%'");
                });

            migrationBuilder.CreateTable(
                name: "DailyAdvices",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Value = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Locale = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Weight = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    Tag = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => table.PrimaryKey("PK_DailyAdvices", x => x.Id));

            migrationBuilder.CreateTable(
                name: "EmailOutbox",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Purpose = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, defaultValue: "other"),
                    ReplyTo = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    InReplyTo = table.Column<string>(type: "character varying(998)", maxLength: 998, nullable: true),
                    AutoSubmitted = table.Column<bool>(type: "boolean", nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    FromAddress = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    FromName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ToAddressesJson = table.Column<string>(type: "text", nullable: false),
                    Subject = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    HtmlBody = table.Column<string>(type: "text", nullable: false),
                    TextBody = table.Column<string>(type: "text", nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NextAttemptOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    ProcessedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeadLetteredOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LockedUntilUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LockedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    LastError = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                },
                constraints: table => table.PrimaryKey("PK_EmailOutbox", x => x.Id));

            migrationBuilder.CreateTable(
                name: "EmailTemplates",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Locale = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    Subject = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    HtmlBody = table.Column<string>(type: "text", nullable: false),
                    TextBody = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => table.PrimaryKey("PK_EmailTemplates", x => x.Id));

            migrationBuilder.CreateTable(
                name: "FastingTelemetryEvents",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SessionId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Protocol = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    PlanType = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    OccurrenceKind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    ReminderPresetId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    ReminderSource = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    FirstReminderHours = table.Column<int>(type: "integer", nullable: true),
                    FollowUpReminderHours = table.Column<int>(type: "integer", nullable: true),
                    PlannedDurationHours = table.Column<int>(type: "integer", nullable: true),
                    ActualDurationHours = table.Column<double>(type: "double precision", nullable: true),
                    HungerLevel = table.Column<int>(type: "integer", nullable: true),
                    EnergyLevel = table.Column<int>(type: "integer", nullable: true),
                    MoodLevel = table.Column<int>(type: "integer", nullable: true),
                    SymptomsCount = table.Column<int>(type: "integer", nullable: true),
                    HadNotes = table.Column<bool>(type: "boolean", nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => table.PrimaryKey("PK_FastingTelemetryEvents", x => x.Id));

            migrationBuilder.CreateTable(
                name: "ImageObjectDeletionOutbox",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ObjectKey = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    IsConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NextAttemptOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    ProcessedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeadLetteredOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LockedUntilUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LockedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    LastError = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                },
                constraints: table => table.PrimaryKey("PK_ImageObjectDeletionOutbox", x => x.Id));

            migrationBuilder.CreateTable(
                name: "MarketingAttributionEvents",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    EventType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    AnonymousId = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
                    SessionId = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
                    LandingPath = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    ReferrerHost = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    UtmSource = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    UtmMedium = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    UtmCampaign = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    UtmContent = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    UtmTerm = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    BuildVersion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => table.PrimaryKey("PK_MarketingAttributionEvents", x => x.Id));

            migrationBuilder.CreateTable(
                name: "NutritionLessons",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Content = table.Column<string>(type: "character varying(65536)", maxLength: 65536, nullable: false),
                    Summary = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Locale = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Category = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Difficulty = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    EstimatedReadMinutes = table.Column<int>(type: "integer", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => table.PrimaryKey("PK_NutritionLessons", x => x.Id));

            migrationBuilder.CreateTable(
                name: "OpenFoodFactsProducts",
                columns: table => new {
                    Barcode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Brand = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Category = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    ImageUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    CaloriesPer100G = table.Column<double>(type: "double precision", nullable: true),
                    ProteinsPer100G = table.Column<double>(type: "double precision", nullable: true),
                    FatsPer100G = table.Column<double>(type: "double precision", nullable: true),
                    CarbsPer100G = table.Column<double>(type: "double precision", nullable: true),
                    FiberPer100G = table.Column<double>(type: "double precision", nullable: true),
                    LastSyncedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastSeenAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SearchHitCount = table.Column<int>(type: "integer", nullable: false),
                },
                constraints: table => table.PrimaryKey("PK_OpenFoodFactsProducts", x => x.Barcode));

            migrationBuilder.CreateTable(
                name: "OutboxReplayAudits",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OutboxName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Reason = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    RequestedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PreviousAttemptCount = table.Column<int>(type: "integer", nullable: false),
                    PreviousError = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                },
                constraints: table => table.PrimaryKey("PK_OutboxReplayAudits", x => x.Id));

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => table.PrimaryKey("PK_Roles", x => x.Id));

            migrationBuilder.CreateTable(
                name: "TelegramAssertionPolicies",
                columns: table => new {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    NotBeforeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table => {
                    table.PrimaryKey("PK_TelegramAssertionPolicies", x => x.Id);
                    table.CheckConstraint("CK_TelegramAssertionPolicies_Singleton", "\"Id\" = 1");
                });

            migrationBuilder.CreateTable(
                name: "TelegramLoginTickets",
                columns: table => new {
                    Fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Purpose = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    BrowserBindingHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ProtectedPayload = table.Column<string>(type: "character varying(16384)", maxLength: 16384, nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table => table.PrimaryKey("PK_TelegramLoginTickets", x => x.Fingerprint));

            migrationBuilder.CreateTable(
                name: "TelegramOperations",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BotId = table.Column<long>(type: "bigint", nullable: false),
                    UpdateId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SecurityVersion = table.Column<long>(type: "bigint", nullable: false),
                    PayloadHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ProtectedPayload = table.Column<string>(type: "character varying(65536)", maxLength: 65536, nullable: false),
                    ProtectedCheckpoint = table.Column<string>(type: "character varying(65536)", maxLength: 65536, nullable: true),
                    LeaseId = table.Column<Guid>(type: "uuid", nullable: true),
                    LeaseExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NextAttemptAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Completed = table.Column<bool>(type: "boolean", nullable: false),
                },
                constraints: table => table.PrimaryKey("PK_TelegramOperations", x => x.Id));

            migrationBuilder.CreateTable(
                name: "UsdaFoods",
                columns: table => new {
                    FdcId = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    FoodCategoryId = table.Column<int>(type: "integer", nullable: true),
                    FoodCategory = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                },
                constraints: table => table.PrimaryKey("PK_UsdaFoods", x => x.FdcId));

            migrationBuilder.CreateTable(
                name: "UsdaNutrients",
                columns: table => new {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    UnitName = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                },
                constraints: table => table.PrimaryKey("PK_UsdaNutrients", x => x.Id));

            migrationBuilder.CreateTable(
                name: "AiPromptRevisions",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PromptText = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SavedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ArchivedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                },
                constraints: table => {
                    table.PrimaryKey("PK_AiPromptRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiPromptRevisions_AiPromptTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "AiPromptTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmailTemplateRevisions",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Subject = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    HtmlBody = table.Column<string>(type: "text", nullable: false),
                    TextBody = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SavedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ArchivedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                },
                constraints: table => {
                    table.PrimaryKey("PK_EmailTemplateRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailTemplateRevisions_EmailTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "EmailTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UsdaFoodPortions",
                columns: table => new {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    FdcId = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<double>(type: "double precision", nullable: false),
                    MeasureUnitName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    GramWeight = table.Column<double>(type: "double precision", nullable: false),
                    PortionDescription = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Modifier = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                },
                constraints: table => {
                    table.PrimaryKey("PK_UsdaFoodPortions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UsdaFoodPortions_UsdaFoods_FdcId",
                        column: x => x.FdcId,
                        principalTable: "UsdaFoods",
                        principalColumn: "FdcId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DailyReferenceValues",
                columns: table => new {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NutrientId = table.Column<int>(type: "integer", nullable: false),
                    Value = table.Column<double>(type: "double precision", nullable: false),
                    Unit = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    AgeGroup = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Gender = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                },
                constraints: table => {
                    table.PrimaryKey("PK_DailyReferenceValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DailyReferenceValues_UsdaNutrients_NutrientId",
                        column: x => x.NutrientId,
                        principalTable: "UsdaNutrients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UsdaFoodNutrients",
                columns: table => new {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    FdcId = table.Column<int>(type: "integer", nullable: false),
                    NutrientId = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<double>(type: "double precision", nullable: false),
                },
                constraints: table => {
                    table.PrimaryKey("PK_UsdaFoodNutrients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UsdaFoodNutrients_UsdaFoods_FdcId",
                        column: x => x.FdcId,
                        principalTable: "UsdaFoods",
                        principalColumn: "FdcId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UsdaFoodNutrients_UsdaNutrients_NutrientId",
                        column: x => x.NutrientId,
                        principalTable: "UsdaNutrients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AdminImpersonationSessions",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ActorIpAddress = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ActorUserAgent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => table.PrimaryKey("PK_AdminImpersonationSessions", x => x.Id));

            migrationBuilder.CreateTable(
                name: "AiQuotaPeriods",
                columns: table => new {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodStartUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConsumedInputTokens = table.Column<long>(type: "bigint", nullable: false),
                    ConsumedOutputTokens = table.Column<long>(type: "bigint", nullable: false),
                    ReservedInputTokens = table.Column<long>(type: "bigint", nullable: false),
                    ReservedOutputTokens = table.Column<long>(type: "bigint", nullable: false),
                    UpdatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table => {
                    table.PrimaryKey("PK_AiQuotaPeriods", x => new { x.UserId, x.PeriodStartUtc });
                    table.CheckConstraint("CK_AiQuotaPeriods_ConsumedInputTokens", "\"ConsumedInputTokens\" >= 0");
                    table.CheckConstraint("CK_AiQuotaPeriods_ConsumedOutputTokens", "\"ConsumedOutputTokens\" >= 0");
                    table.CheckConstraint("CK_AiQuotaPeriods_ReservedInputTokens", "\"ReservedInputTokens\" >= 0");
                    table.CheckConstraint("CK_AiQuotaPeriods_ReservedOutputTokens", "\"ReservedOutputTokens\" >= 0");
                });

            migrationBuilder.CreateTable(
                name: "AiQuotaReservations",
                columns: table => new {
                    RequestId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodStartUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Operation = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ReservedInputTokens = table.Column<long>(type: "bigint", nullable: false),
                    ReservedOutputTokens = table.Column<long>(type: "bigint", nullable: false),
                    ActualInputTokens = table.Column<long>(type: "bigint", nullable: true),
                    ActualOutputTokens = table.Column<long>(type: "bigint", nullable: true),
                    State = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    ExpiresOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table => {
                    table.PrimaryKey("PK_AiQuotaReservations", x => x.RequestId);
                    table.CheckConstraint("CK_AiQuotaReservations_ReservedInputTokens", "\"ReservedInputTokens\" >= 0");
                    table.CheckConstraint("CK_AiQuotaReservations_ReservedOutputTokens", "\"ReservedOutputTokens\" >= 0");
                    table.ForeignKey(
                        name: "FK_AiQuotaReservations_AiQuotaPeriods_UserId_PeriodStartUtc",
                        columns: x => new { x.UserId, x.PeriodStartUtc },
                        principalTable: "AiQuotaPeriods",
                        principalColumns: ["UserId", "PeriodStartUtc"],
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AiUsages",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Operation = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Model = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    InputTokens = table.Column<int>(type: "integer", nullable: false),
                    OutputTokens = table.Column<int>(type: "integer", nullable: false),
                    TotalTokens = table.Column<int>(type: "integer", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => table.PrimaryKey("PK_AiUsages", x => x.Id));

            migrationBuilder.CreateTable(
                name: "BillingPayments",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    BillingSubscriptionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Provider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ExternalPaymentId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ExternalCustomerId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ExternalSubscriptionId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ExternalPaymentMethodId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ExternalPriceId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Plan = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(19,3)", nullable: true, precision: 19, scale: 3),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    Tax = table.Column<decimal>(type: "numeric(19,3)", nullable: true, precision: 19, scale: 3),
                    Fee = table.Column<decimal>(type: "numeric(19,3)", nullable: true, precision: 19, scale: 3),
                    Earnings = table.Column<decimal>(type: "numeric(19,3)", nullable: true, precision: 19, scale: 3),
                    PayoutCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    PayoutEarnings = table.Column<decimal>(type: "numeric(19,3)", nullable: true, precision: 19, scale: 3),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CurrentPeriodStartUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CurrentPeriodEndUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    WebhookEventId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ProviderMetadataJson = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => table.PrimaryKey("PK_BillingPayments", x => x.Id));

            migrationBuilder.CreateTable(
                name: "BillingSubscriptions",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ExternalCustomerId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ExternalSubscriptionId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ExternalPaymentMethodId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ExternalPriceId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Plan = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CurrentPeriodStartUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CurrentPeriodEndUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelAtPeriodEnd = table.Column<bool>(type: "boolean", nullable: false),
                    CanceledAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TrialStartUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TrialEndUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NextBillingAttemptUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ProviderMetadataJson = table.Column<string>(type: "jsonb", nullable: true),
                    LastWebhookEventId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    LastWebhookOccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastSyncedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PremiumRoleManagedByBilling = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => table.PrimaryKey("PK_BillingSubscriptions", x => x.Id));

            migrationBuilder.CreateTable(
                name: "ClientTasks",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DietologistUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Details = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    DueAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    StatusChangedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DueReminderSentAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => table.PrimaryKey("PK_ClientTasks", x => x.Id));

            migrationBuilder.CreateTable(
                name: "ContentReports",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TargetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AdminNote = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ReviewedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => table.PrimaryKey("PK_ContentReports", x => x.Id));

            migrationBuilder.CreateTable(
                name: "CycleBleedingEntries",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CycleProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Flow = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PainImpact = table.Column<int>(type: "integer", nullable: true),
                    Notes = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => table.PrimaryKey("PK_CycleBleedingEntries", x => x.Id));

            migrationBuilder.CreateTable(
                name: "CycleConsents",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CycleProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Purpose = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    GrantedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevokedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => table.PrimaryKey("PK_CycleConsents", x => x.Id));

            migrationBuilder.CreateTable(
                name: "CycleDayNotes",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CycleProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => table.PrimaryKey("PK_CycleDayNotes", x => x.Id));

            migrationBuilder.CreateTable(
                name: "CycleFactors",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CycleProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Notes = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => table.PrimaryKey("PK_CycleFactors", x => x.Id));

            migrationBuilder.CreateTable(
                name: "CycleMenstrualEpisodes",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CycleProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ExcludedFromPredictions = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => table.PrimaryKey("PK_CycleMenstrualEpisodes", x => x.Id));

            migrationBuilder.CreateTable(
                name: "CyclePredictionRevisions",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CycleProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    GeneratedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NextPeriodStartFrom = table.Column<DateOnly>(type: "date", nullable: true),
                    NextPeriodStartTo = table.Column<DateOnly>(type: "date", nullable: true),
                    Confidence = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DataSufficiency = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PatternConsistency = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CompletedCycleCount = table.Column<int>(type: "integer", nullable: false),
                    CalibrationSampleCount = table.Column<int>(type: "integer", nullable: false),
                    HistoricalCoveragePercent = table.Column<double>(type: "double precision", nullable: true),
                    MeanAbsoluteErrorDays = table.Column<double>(type: "double precision", nullable: true),
                    ReasonCodes = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    AlgorithmVersion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => table.PrimaryKey("PK_CyclePredictionRevisions", x => x.Id));

            migrationBuilder.CreateTable(
                name: "CycleProfiles",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Mode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Goal = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ReproductiveState = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Confidence = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    TrackingStartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    AverageCycleLength = table.Column<int>(type: "integer", nullable: false, defaultValue: 28),
                    AveragePeriodLength = table.Column<int>(type: "integer", nullable: false, defaultValue: 5),
                    LutealLength = table.Column<int>(type: "integer", nullable: false, defaultValue: 14),
                    IsRegular = table.Column<bool>(type: "boolean", nullable: false),
                    IsOnboardingComplete = table.Column<bool>(type: "boolean", nullable: false),
                    ShowFertilityEstimates = table.Column<bool>(type: "boolean", nullable: false),
                    DiscreetNotifications = table.Column<bool>(type: "boolean", nullable: false),
                    HideFromDashboard = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => table.PrimaryKey("PK_CycleProfiles", x => x.Id));

            migrationBuilder.CreateTable(
                name: "CycleSymptomEntries",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CycleProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Category = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Intensity = table.Column<int>(type: "integer", nullable: false),
                    TagsJson = table.Column<string>(type: "jsonb", nullable: false),
                    Note = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => {
                    table.PrimaryKey("PK_CycleSymptomEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CycleSymptomEntries_CycleProfiles_CycleProfileId",
                        column: x => x.CycleProfileId,
                        principalTable: "CycleProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FertilitySignals",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CycleProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    BasalBodyTemperatureCelsius = table.Column<double>(type: "double precision", nullable: true),
                    OvulationTestResult = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    CervicalFluid = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    HadSex = table.Column<bool>(type: "boolean", nullable: true),
                    Notes = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => {
                    table.PrimaryKey("PK_FertilitySignals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FertilitySignals_CycleProfiles_CycleProfileId",
                        column: x => x.CycleProfileId,
                        principalTable: "CycleProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DietologistInvitations",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DietologistUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    DietologistEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ShareProfile = table.Column<bool>(type: "boolean", nullable: false),
                    ShareMeals = table.Column<bool>(type: "boolean", nullable: false),
                    ShareStatistics = table.Column<bool>(type: "boolean", nullable: false),
                    ShareWeight = table.Column<bool>(type: "boolean", nullable: false),
                    ShareWaist = table.Column<bool>(type: "boolean", nullable: false),
                    ShareGoals = table.Column<bool>(type: "boolean", nullable: false),
                    ShareHydration = table.Column<bool>(type: "boolean", nullable: false),
                    ShareFasting = table.Column<bool>(type: "boolean", nullable: false),
                    AcceptedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => table.PrimaryKey("PK_DietologistInvitations", x => x.Id));

            migrationBuilder.CreateTable(
                name: "ExerciseEntries",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateTime>(type: "date", nullable: false),
                    ExerciseType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    DurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    CaloriesBurned = table.Column<double>(type: "double precision", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => table.PrimaryKey("PK_ExerciseEntries", x => x.Id));

            migrationBuilder.CreateTable(
                name: "FastingCheckIns",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OccurrenceId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CheckedInAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    HungerLevel = table.Column<int>(type: "integer", nullable: false),
                    EnergyLevel = table.Column<int>(type: "integer", nullable: false),
                    MoodLevel = table.Column<int>(type: "integer", nullable: false),
                    Symptoms = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => table.PrimaryKey("PK_FastingCheckIns", x => x.Id));

            migrationBuilder.CreateTable(
                name: "FastingOccurrences",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    SequenceNumber = table.Column<int>(type: "integer", nullable: false),
                    ScheduledForUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    InitialTargetHours = table.Column<int>(type: "integer", nullable: true),
                    AddedTargetHours = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CheckInAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    HungerLevel = table.Column<int>(type: "integer", nullable: true),
                    EnergyLevel = table.Column<int>(type: "integer", nullable: true),
                    MoodLevel = table.Column<int>(type: "integer", nullable: true),
                    Symptoms = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CheckInNotes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => table.PrimaryKey("PK_FastingOccurrences", x => x.Id));

            migrationBuilder.CreateTable(
                name: "FastingPlans",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Protocol = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Title = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StoppedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IntermittentFastHours = table.Column<int>(type: "integer", nullable: true),
                    IntermittentEatingWindowHours = table.Column<int>(type: "integer", nullable: true),
                    ExtendedTargetHours = table.Column<int>(type: "integer", nullable: true),
                    CyclicFastDays = table.Column<int>(type: "integer", nullable: true),
                    CyclicEatDays = table.Column<int>(type: "integer", nullable: true),
                    CyclicEatDayFastHours = table.Column<int>(type: "integer", nullable: true),
                    CyclicEatDayEatingWindowHours = table.Column<int>(type: "integer", nullable: true),
                    CyclicAnchorDateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CyclicNextPhaseDateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => table.PrimaryKey("PK_FastingPlans", x => x.Id));

            migrationBuilder.CreateTable(
                name: "FastingSessions",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    InitialPlannedDurationHours = table.Column<int>(type: "integer", nullable: false),
                    AddedDurationHours = table.Column<int>(type: "integer", nullable: false),
                    Protocol = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => table.PrimaryKey("PK_FastingSessions", x => x.Id));

            migrationBuilder.CreateTable(
                name: "FavoriteMeals",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    MealId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RemovedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => table.PrimaryKey("PK_FavoriteMeals", x => x.Id));

            migrationBuilder.CreateTable(
                name: "FavoriteProducts",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PreferredPortionAmount = table.Column<double>(type: "double precision", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => table.PrimaryKey("PK_FavoriteProducts", x => x.Id));

            migrationBuilder.CreateTable(
                name: "FavoriteRecipes",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => table.PrimaryKey("PK_FavoriteRecipes", x => x.Id));

            migrationBuilder.CreateTable(
                name: "FoodRecognitionJobImages",
                columns: table => new {
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    ImageAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    ImageUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                },
                constraints: table => table.PrimaryKey("PK_FoodRecognitionJobImages", x => new { x.JobId, x.ImageAssetId }));

            migrationBuilder.CreateTable(
                name: "FoodRecognitionJobs",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IsProductLabel = table.Column<bool>(type: "boolean", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ImageAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    ImageUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    Description = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    VisionJson = table.Column<string>(type: "text", nullable: true),
                    NutritionJson = table.Column<string>(type: "text", nullable: true),
                    ErrorCode = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    NutritionErrorCode = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                },
                constraints: table => table.PrimaryKey("PK_FoodRecognitionJobs", x => x.Id));

            migrationBuilder.CreateTable(
                name: "HydrationEntries",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AmountMl = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => {
                    table.PrimaryKey("PK_HydrationEntries", x => x.Id);
                    table.CheckConstraint("CK_HydrationEntries_AmountMl", "\"AmountMl\" > 0 AND \"AmountMl\" <= 10000");
                });

            migrationBuilder.CreateTable(
                name: "HydrationOperationReceipts",
                columns: table => new {
                    OperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntryId = table.Column<Guid>(type: "uuid", nullable: false),
                    TimestampUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AmountMl = table.Column<int>(type: "integer", nullable: false),
                },
                constraints: table => table.PrimaryKey("PK_HydrationOperationReceipts", x => new { x.UserId, x.OperationId }));

            migrationBuilder.CreateTable(
                name: "ImageAssets",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ObjectKey = table.Column<string>(type: "text", nullable: false),
                    Url = table.Column<string>(type: "text", nullable: false),
                    IsConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => table.PrimaryKey("PK_ImageAssets", x => x.Id));

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: true),
                    Password = table.Column<string>(type: "text", nullable: false),
                    HasPassword = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    MustChangePassword = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    GoogleIssuer = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    GoogleSubject = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    IsEmailConfirmed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    EmailConfirmationTokenHash = table.Column<string>(type: "text", nullable: true),
                    EmailConfirmationTokenExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EmailConfirmationSentAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PasswordResetTokenHash = table.Column<string>(type: "text", nullable: true),
                    PasswordResetTokenExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PasswordResetSentAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastLoginAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Username = table.Column<string>(type: "text", nullable: true),
                    FirstName = table.Column<string>(type: "text", nullable: true),
                    LastName = table.Column<string>(type: "text", nullable: true),
                    ProfileImage = table.Column<string>(type: "text", nullable: true),
                    ProfileImageAssetId = table.Column<Guid>(type: "uuid", nullable: true),
                    TelegramUserId = table.Column<long>(type: "bigint", nullable: true),
                    TelegramLinkVerifiedForRecovery = table.Column<bool>(type: "boolean", nullable: false),
                    TelegramOidcIssuer = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    TelegramOidcSubject = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    AiInputTokenLimit = table.Column<long>(type: "bigint", nullable: false, defaultValue: 5000000L),
                    AiOutputTokenLimit = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1000000L),
                    AiConsentAcceptedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PremiumTrialStartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PremiumTrialEndsAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SecurityVersion = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_ImageAssets_ProfileImageAssetId",
                        column: x => x.ProfileImageAssetId,
                        principalTable: "ImageAssets",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MealPlans",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    DietType = table.Column<string>(type: "text", nullable: false),
                    DurationDays = table.Column<int>(type: "integer", nullable: false),
                    TargetCaloriesPerDay = table.Column<double>(type: "double precision", nullable: true),
                    IsCurated = table.Column<bool>(type: "boolean", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => {
                    table.PrimaryKey("PK_MealPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MealPlans_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MealRecognitionReceipts",
                columns: table => new {
                    OperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecognitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    MealId = table.Column<Guid>(type: "uuid", nullable: false),
                    MealVersion = table.Column<long>(type: "bigint", nullable: false),
                    MealOccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SavedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UndoUntilUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UndoneAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                },
                constraints: table => {
                    table.PrimaryKey("PK_MealRecognitionReceipts", x => x.OperationId);
                    table.ForeignKey(
                        name: "FK_MealRecognitionReceipts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Meals",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MealType = table.Column<int>(type: "integer", nullable: true),
                    Comment = table.Column<string>(type: "text", nullable: true),
                    ImageUrl = table.Column<string>(type: "text", nullable: true),
                    ImageAssetId = table.Column<Guid>(type: "uuid", nullable: true),
                    TotalCalories = table.Column<double>(type: "double precision", nullable: false),
                    TotalProteins = table.Column<double>(type: "double precision", nullable: false),
                    TotalFats = table.Column<double>(type: "double precision", nullable: false),
                    TotalCarbs = table.Column<double>(type: "double precision", nullable: false),
                    TotalFiber = table.Column<double>(type: "double precision", nullable: false),
                    TotalAlcohol = table.Column<double>(type: "double precision", nullable: false),
                    IsNutritionAutoCalculated = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    ManualCalories = table.Column<double>(type: "double precision", nullable: true),
                    ManualProteins = table.Column<double>(type: "double precision", nullable: true),
                    ManualFats = table.Column<double>(type: "double precision", nullable: true),
                    ManualCarbs = table.Column<double>(type: "double precision", nullable: true),
                    ManualFiber = table.Column<double>(type: "double precision", nullable: true),
                    ManualAlcohol = table.Column<double>(type: "double precision", nullable: true),
                    PreMealSatietyLevel = table.Column<int>(type: "integer", nullable: false),
                    PostMealSatietyLevel = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => {
                    table.PrimaryKey("PK_Meals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Meals_ImageAssets_ImageAssetId",
                        column: x => x.ImageAssetId,
                        principalTable: "ImageAssets",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Meals_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PayloadJson = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    ReferenceId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    ReadAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Barcode = table.Column<string>(type: "text", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Brand = table.Column<string>(type: "text", nullable: true),
                    Category = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Comment = table.Column<string>(type: "text", nullable: true),
                    ImageUrl = table.Column<string>(type: "text", nullable: true),
                    ImageAssetId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProductType = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    BaseUnit = table.Column<int>(type: "integer", nullable: false),
                    BaseAmount = table.Column<double>(type: "double precision", nullable: false),
                    DefaultPortionAmount = table.Column<double>(type: "double precision", nullable: false),
                    CaloriesPerBase = table.Column<double>(type: "double precision", nullable: false),
                    ProteinsPerBase = table.Column<double>(type: "double precision", nullable: false),
                    FatsPerBase = table.Column<double>(type: "double precision", nullable: false),
                    CarbsPerBase = table.Column<double>(type: "double precision", nullable: false),
                    FiberPerBase = table.Column<double>(type: "double precision", nullable: false),
                    AlcoholPerBase = table.Column<double>(type: "double precision", nullable: false),
                    Visibility = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    UsdaFdcId = table.Column<int>(type: "integer", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => {
                    table.PrimaryKey("PK_Products", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Products_ImageAssets_ImageAssetId",
                        column: x => x.ImageAssetId,
                        principalTable: "ImageAssets",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Products_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RecentItems",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    LastUsedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UsageCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => {
                    table.PrimaryKey("PK_RecentItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecentItems_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RecipeLikes",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipeId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => {
                    table.PrimaryKey("PK_RecipeLikes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecipeLikes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Recipes",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LanguageConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    Language = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false, defaultValue: "en"),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Comment = table.Column<string>(type: "text", nullable: true),
                    Category = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false, defaultValue: "other"),
                    ImageUrl = table.Column<string>(type: "text", nullable: true),
                    ImageAssetId = table.Column<Guid>(type: "uuid", nullable: true),
                    PrepTime = table.Column<int>(type: "integer", nullable: true),
                    CookTime = table.Column<int>(type: "integer", nullable: true),
                    Servings = table.Column<int>(type: "integer", nullable: false),
                    MissingIngredientCount = table.Column<int>(type: "integer", nullable: false),
                    TotalCalories = table.Column<double>(type: "double precision", nullable: true),
                    TotalProteins = table.Column<double>(type: "double precision", nullable: true),
                    TotalFats = table.Column<double>(type: "double precision", nullable: true),
                    TotalCarbs = table.Column<double>(type: "double precision", nullable: true),
                    TotalFiber = table.Column<double>(type: "double precision", nullable: true),
                    TotalAlcohol = table.Column<double>(type: "double precision", nullable: true),
                    IsNutritionAutoCalculated = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    ManualCalories = table.Column<double>(type: "double precision", nullable: true),
                    ManualProteins = table.Column<double>(type: "double precision", nullable: true),
                    ManualFats = table.Column<double>(type: "double precision", nullable: true),
                    ManualCarbs = table.Column<double>(type: "double precision", nullable: true),
                    ManualFiber = table.Column<double>(type: "double precision", nullable: true),
                    ManualAlcohol = table.Column<double>(type: "double precision", nullable: true),
                    Visibility = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => {
                    table.PrimaryKey("PK_Recipes", x => x.Id);
                    table.CheckConstraint("CK_Recipes_Category", "\"Category\" IN ('other', 'breakfast', 'soups', 'salads', 'main_courses', 'side_dishes', 'appetizers', 'sandwiches', 'pasta', 'baking', 'desserts', 'drinks', 'sauces', 'snacks', 'preserves')");
                    table.ForeignKey(
                        name: "FK_Recipes_ImageAssets_ImageAssetId",
                        column: x => x.ImageAssetId,
                        principalTable: "ImageAssets",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Recipes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Recommendations",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DietologistUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Text = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    ReadAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => {
                    table.PrimaryKey("PK_Recommendations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Recommendations_Users_ClientUserId",
                        column: x => x.ClientUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Recommendations_Users_DietologistUserId",
                        column: x => x.DietologistUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RecommendationTemplates",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DietologistUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Text = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    IsArchived = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => {
                    table.PrimaryKey("PK_RecommendationTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecommendationTemplates_Users_DietologistUserId",
                        column: x => x.DietologistUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShoppingLists",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => {
                    table.PrimaryKey("PK_ShoppingLists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShoppingLists_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserAchievements",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AchievementKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EarnedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EarnedValue = table.Column<int>(type: "integer", nullable: false),
                    DefinitionVersion = table.Column<int>(type: "integer", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => {
                    table.PrimaryKey("PK_UserAchievements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserAchievements_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserLessonProgress",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LessonId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReadAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => {
                    table.PrimaryKey("PK_UserLessonProgress", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserLessonProgress_NutritionLessons_LessonId",
                        column: x => x.LessonId,
                        principalTable: "NutritionLessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserLessonProgress_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserLoginEvents",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthProvider = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    BrowserName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    BrowserVersion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    OperatingSystem = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    DeviceType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    LoggedInAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => {
                    table.PrimaryKey("PK_UserLoginEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserLoginEvents_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserNutritionProfiles",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BirthDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Gender = table.Column<string>(type: "text", nullable: true),
                    WeightKg = table.Column<double>(type: "double precision", nullable: true),
                    DesiredWeightKg = table.Column<double>(type: "double precision", nullable: true),
                    DesiredWaistCm = table.Column<double>(type: "double precision", nullable: true),
                    HeightCm = table.Column<double>(type: "double precision", nullable: true),
                    ActivityLevel = table.Column<string>(type: "text", nullable: false),
                    DailyCalorieTarget = table.Column<double>(type: "double precision", nullable: true),
                    ProteinTarget = table.Column<double>(type: "double precision", nullable: true),
                    FatTarget = table.Column<double>(type: "double precision", nullable: true),
                    CarbTarget = table.Column<double>(type: "double precision", nullable: true),
                    FiberTarget = table.Column<double>(type: "double precision", nullable: true),
                    StepGoal = table.Column<int>(type: "integer", nullable: true),
                    WaterGoal = table.Column<double>(type: "double precision", nullable: true),
                    HydrationGoal = table.Column<double>(type: "double precision", nullable: true),
                    CalorieCyclingEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    MondayCalories = table.Column<double>(type: "double precision", nullable: true),
                    TuesdayCalories = table.Column<double>(type: "double precision", nullable: true),
                    WednesdayCalories = table.Column<double>(type: "double precision", nullable: true),
                    ThursdayCalories = table.Column<double>(type: "double precision", nullable: true),
                    FridayCalories = table.Column<double>(type: "double precision", nullable: true),
                    SaturdayCalories = table.Column<double>(type: "double precision", nullable: true),
                    SundayCalories = table.Column<double>(type: "double precision", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => {
                    table.PrimaryKey("PK_UserNutritionProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserNutritionProfiles_Users_Id",
                        column: x => x.Id,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserPreferences",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DashboardLayout = table.Column<string>(type: "jsonb", nullable: true),
                    Language = table.Column<string>(type: "text", nullable: true, defaultValue: "en"),
                    Theme = table.Column<string>(type: "text", nullable: true, defaultValue: "ocean"),
                    UiStyle = table.Column<string>(type: "text", nullable: true, defaultValue: "classic"),
                    SurfaceStyle = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false, defaultValue: "normal"),
                    PushNotificationsEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    FastingPushNotificationsEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    SocialPushNotificationsEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    FastingCheckInReminderHours = table.Column<int>(type: "integer", nullable: false, defaultValue: 12),
                    FastingCheckInFollowUpReminderHours = table.Column<int>(type: "integer", nullable: false, defaultValue: 20),
                    TimeZoneId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => {
                    table.PrimaryKey("PK_UserPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserPreferences_Users_Id",
                        column: x => x.Id,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRefreshTokenSessions",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RefreshTokenHash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    RememberMe = table.Column<bool>(type: "boolean", nullable: false),
                    AuthProvider = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastRotatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PreviousRefreshTokenHash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    PreviousRefreshTokenValidUntilUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => {
                    table.PrimaryKey("PK_UserRefreshTokenSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserRefreshTokenSessions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRoleAuditEvents",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Action = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Source = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => {
                    table.PrimaryKey("PK_UserRoleAuditEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserRoleAuditEvents_Users_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_UserRoleAuditEvents_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                },
                constraints: table => {
                    table.PrimaryKey("PK_UserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WaistEntries",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateTime>(type: "date", nullable: false),
                    CircumferenceCm = table.Column<double>(type: "double precision", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => {
                    table.PrimaryKey("PK_WaistEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WaistEntries_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WaistGoals",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetWaistCm = table.Column<double>(type: "double precision", nullable: false),
                    StartWaistCm = table.Column<double>(type: "double precision", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    EndedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndWaistCm = table.Column<double>(type: "double precision", nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => {
                    table.PrimaryKey("PK_WaistGoals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WaistGoals_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WearableConnections",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ExternalUserId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    AccessToken = table.Column<string>(type: "character varying(8192)", maxLength: 8192, nullable: false),
                    RefreshToken = table.Column<string>(type: "character varying(8192)", maxLength: 8192, nullable: true),
                    TokenExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastSyncedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LastConnectRequestId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    LastConnectRequestHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => {
                    table.PrimaryKey("PK_WearableConnections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WearableConnections_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WearableSyncEntries",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DataType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Date = table.Column<DateTime>(type: "date", nullable: false),
                    Value = table.Column<double>(type: "double precision", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => {
                    table.PrimaryKey("PK_WearableSyncEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WearableSyncEntries_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WebPushSubscriptions",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Endpoint = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    P256Dh = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Auth = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    ExpirationTimeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Locale = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => {
                    table.PrimaryKey("PK_WebPushSubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WebPushSubscriptions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WeeklyGoals",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    WeekStartUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    TargetDays = table.Column<int>(type: "integer", nullable: false),
                    ReminderEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    ReminderTimeMinutes = table.Column<int>(type: "integer", nullable: true),
                    TimeZoneOffsetMinutes = table.Column<int>(type: "integer", nullable: true),
                    LastReminderLocalDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => {
                    table.PrimaryKey("PK_WeeklyGoals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WeeklyGoals_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WeightEntries",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateTime>(type: "date", nullable: false),
                    WeightKg = table.Column<double>(type: "double precision", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => {
                    table.PrimaryKey("PK_WeightEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WeightEntries_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WeightGoals",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetWeightKg = table.Column<double>(type: "double precision", nullable: false),
                    StartWeightKg = table.Column<double>(type: "double precision", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    EndedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndWeightKg = table.Column<double>(type: "double precision", nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => {
                    table.PrimaryKey("PK_WeightGoals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WeightGoals_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MealPlanDays",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MealPlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    DayNumber = table.Column<int>(type: "integer", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => {
                    table.PrimaryKey("PK_MealPlanDays", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MealPlanDays_MealPlans_MealPlanId",
                        column: x => x.MealPlanId,
                        principalTable: "MealPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MealAiSessions",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MealId = table.Column<Guid>(type: "uuid", nullable: false),
                    ImageAssetId = table.Column<Guid>(type: "uuid", nullable: true),
                    Source = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false, defaultValue: "Text"),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false, defaultValue: "Reviewed"),
                    RecognizedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Notes = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => {
                    table.PrimaryKey("PK_MealAiSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MealAiSessions_ImageAssets_ImageAssetId",
                        column: x => x.ImageAssetId,
                        principalTable: "ImageAssets",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MealAiSessions_Meals_MealId",
                        column: x => x.MealId,
                        principalTable: "Meals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NotificationWebPushOutbox",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NotificationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NextAttemptOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    ProcessedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeadLetteredOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LockedUntilUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LockedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    LastError = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    CompletedSubscriptionIdsJson = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                },
                constraints: table => {
                    table.PrimaryKey("PK_NotificationWebPushOutbox", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationWebPushOutbox_Notifications_NotificationId",
                        column: x => x.NotificationId,
                        principalTable: "Notifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductImages",
                columns: table => new {
                    ImageAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    ImageUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                },
                constraints: table => {
                    table.PrimaryKey("PK_ProductImages", x => new { x.ProductId, x.ImageAssetId });
                    table.ForeignKey(
                        name: "FK_ProductImages_ImageAssets_ImageAssetId",
                        column: x => x.ImageAssetId,
                        principalTable: "ImageAssets",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductImages_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MealItems",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MealId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: true),
                    RecipeId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceAiItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    Origin = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false, defaultValue: "Manual"),
                    Amount = table.Column<double>(type: "double precision", nullable: false),
                    SnapshotName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    SnapshotImageUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    SnapshotUnit = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    SnapshotBaseAmount = table.Column<double>(type: "double precision", nullable: true),
                    SnapshotCaloriesPerBase = table.Column<double>(type: "double precision", nullable: true),
                    SnapshotProteinsPerBase = table.Column<double>(type: "double precision", nullable: true),
                    SnapshotFatsPerBase = table.Column<double>(type: "double precision", nullable: true),
                    SnapshotCarbsPerBase = table.Column<double>(type: "double precision", nullable: true),
                    SnapshotFiberPerBase = table.Column<double>(type: "double precision", nullable: true),
                    SnapshotAlcoholPerBase = table.Column<double>(type: "double precision", nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => {
                    table.PrimaryKey("PK_MealItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MealItems_Meals_MealId",
                        column: x => x.MealId,
                        principalTable: "Meals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MealItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MealItems_Recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "Recipes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RecipeComments",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Text = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => {
                    table.PrimaryKey("PK_RecipeComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecipeComments_Recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "Recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RecipeComments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RecipeImages",
                columns: table => new {
                    ImageAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ImageUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                },
                constraints: table => {
                    table.PrimaryKey("PK_RecipeImages", x => new { x.RecipeId, x.ImageAssetId });
                    table.ForeignKey(
                        name: "FK_RecipeImages_ImageAssets_ImageAssetId",
                        column: x => x.ImageAssetId,
                        principalTable: "ImageAssets",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RecipeImages_Recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "Recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RecipeSteps",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipeId = table.Column<Guid>(type: "uuid", nullable: false),
                    StepNumber = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: true),
                    Instruction = table.Column<string>(type: "text", nullable: false),
                    ImageUrl = table.Column<string>(type: "text", nullable: true),
                    ImageAssetId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => {
                    table.PrimaryKey("PK_RecipeSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecipeSteps_ImageAssets_ImageAssetId",
                        column: x => x.ImageAssetId,
                        principalTable: "ImageAssets",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RecipeSteps_Recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "Recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RecommendationBulkDispatches",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DietologistUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecommendationId = table.Column<Guid>(type: "uuid", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => {
                    table.PrimaryKey("PK_RecommendationBulkDispatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecommendationBulkDispatches_Recommendations_Recommendation~",
                        column: x => x.RecommendationId,
                        principalTable: "Recommendations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RecommendationBulkDispatches_Users_ClientUserId",
                        column: x => x.ClientUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RecommendationBulkDispatches_Users_DietologistUserId",
                        column: x => x.DietologistUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RecommendationComments",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RecommendationId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Text = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => {
                    table.PrimaryKey("PK_RecommendationComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecommendationComments_Recommendations_RecommendationId",
                        column: x => x.RecommendationId,
                        principalTable: "Recommendations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RecommendationComments_Users_AuthorUserId",
                        column: x => x.AuthorUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShoppingListItems",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ShoppingListId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Amount = table.Column<double>(type: "double precision", nullable: true),
                    Unit = table.Column<int>(type: "integer", nullable: true),
                    Category = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Aisle = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Note = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    IsChecked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CheckedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => {
                    table.PrimaryKey("PK_ShoppingListItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShoppingListItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ShoppingListItems_ShoppingLists_ShoppingListId",
                        column: x => x.ShoppingListId,
                        principalTable: "ShoppingLists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MealPlanMeals",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MealPlanDayId = table.Column<Guid>(type: "uuid", nullable: false),
                    MealType = table.Column<string>(type: "text", nullable: false),
                    RecipeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Servings = table.Column<int>(type: "integer", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => {
                    table.PrimaryKey("PK_MealPlanMeals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MealPlanMeals_MealPlanDays_MealPlanDayId",
                        column: x => x.MealPlanDayId,
                        principalTable: "MealPlanDays",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MealPlanMeals_Recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "Recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MealAiItems",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MealAiSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    NameEn = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    NameLocal = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Amount = table.Column<double>(type: "double precision", nullable: false),
                    Unit = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Calories = table.Column<double>(type: "double precision", nullable: false),
                    Proteins = table.Column<double>(type: "double precision", nullable: false),
                    Fats = table.Column<double>(type: "double precision", nullable: false),
                    Carbs = table.Column<double>(type: "double precision", nullable: false),
                    Fiber = table.Column<double>(type: "double precision", nullable: false),
                    Alcohol = table.Column<double>(type: "double precision", nullable: false),
                    Confidence = table.Column<double>(type: "double precision", nullable: false, defaultValue: 1.0),
                    Resolution = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false, defaultValue: "Accepted"),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => {
                    table.PrimaryKey("PK_MealAiItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MealAiItems_MealAiSessions_MealAiSessionId",
                        column: x => x.MealAiSessionId,
                        principalTable: "MealAiSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RecipeIngredients",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PublicName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    PublicUnit = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    TextName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    AmountText = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    RecipeStepId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: true),
                    NestedRecipeId = table.Column<Guid>(type: "uuid", nullable: true),
                    Amount = table.Column<double>(type: "double precision", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => {
                    table.PrimaryKey("PK_RecipeIngredients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecipeIngredients_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RecipeIngredients_RecipeSteps_RecipeStepId",
                        column: x => x.RecipeStepId,
                        principalTable: "RecipeSteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RecipeIngredients_Recipes_NestedRecipeId",
                        column: x => x.NestedRecipeId,
                        principalTable: "Recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RecipeStepImages",
                columns: table => new {
                    ImageAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipeStepId = table.Column<Guid>(type: "uuid", nullable: false),
                    ImageUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                },
                constraints: table => {
                    table.PrimaryKey("PK_RecipeStepImages", x => new { x.RecipeStepId, x.ImageAssetId });
                    table.ForeignKey(
                        name: "FK_RecipeStepImages_ImageAssets_ImageAssetId",
                        column: x => x.ImageAssetId,
                        principalTable: "ImageAssets",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RecipeStepImages_RecipeSteps_RecipeStepId",
                        column: x => x.RecipeStepId,
                        principalTable: "RecipeSteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShoppingListItemSources",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ShoppingListItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    MealPlanId = table.Column<Guid>(type: "uuid", nullable: true),
                    MealPlanMealId = table.Column<Guid>(type: "uuid", nullable: true),
                    RecipeId = table.Column<Guid>(type: "uuid", nullable: true),
                    Label = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    DayNumber = table.Column<int>(type: "integer", nullable: true),
                    MealType = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Amount = table.Column<double>(type: "double precision", nullable: false),
                    Unit = table.Column<int>(type: "integer", nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => {
                    table.PrimaryKey("PK_ShoppingListItemSources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShoppingListItemSources_ShoppingListItems_ShoppingListItemId",
                        column: x => x.ShoppingListItemId,
                        principalTable: "ShoppingListItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AchievementDefinitions_IsActive_SortOrder",
                table: "AchievementDefinitions",
                columns: ["IsActive", "SortOrder"]);

            migrationBuilder.CreateIndex(
                name: "IX_AchievementDefinitions_Key",
                table: "AchievementDefinitions",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AchievementEvaluationOutbox_DueLease",
                table: "AchievementEvaluationOutbox",
                columns: ["ProcessedOnUtc", "DeadLetteredOnUtc", "NextAttemptOnUtc", "LockedUntilUtc"]);

            migrationBuilder.CreateIndex(
                name: "IX_AchievementEvaluationOutbox_UserId",
                table: "AchievementEvaluationOutbox",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdminImpersonationSessions_ActorUserId",
                table: "AdminImpersonationSessions",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AdminImpersonationSessions_StartedAtUtc",
                table: "AdminImpersonationSessions",
                column: "StartedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AdminImpersonationSessions_TargetUserId",
                table: "AdminImpersonationSessions",
                column: "TargetUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AiPromptRevisions_TemplateId_ArchivedOnUtc",
                table: "AiPromptRevisions",
                columns: ["TemplateId", "ArchivedOnUtc"]);

            migrationBuilder.CreateIndex(
                name: "IX_AiPromptTemplates_Key_IsActive",
                table: "AiPromptTemplates",
                columns: ["Key", "IsActive"]);

            migrationBuilder.CreateIndex(
                name: "IX_AiPromptTemplates_Key_Locale",
                table: "AiPromptTemplates",
                columns: ["Key", "Locale"],
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AiQuotaReservations_UserId_PeriodStartUtc_State_ExpiresOnUtc",
                table: "AiQuotaReservations",
                columns: ["UserId", "PeriodStartUtc", "State", "ExpiresOnUtc"]);

            migrationBuilder.CreateIndex(
                name: "IX_AiUsages_CreatedOnUtc",
                table: "AiUsages",
                column: "CreatedOnUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AiUsages_UserId",
                table: "AiUsages",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AiUsages_UserId_CreatedOnUtc",
                table: "AiUsages",
                columns: ["UserId", "CreatedOnUtc"]);

            migrationBuilder.CreateIndex(
                name: "IX_AtomicCommandReceipts_ExpiresOnUtc",
                table: "AtomicCommandReceipts",
                column: "ExpiresOnUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AtomicCommandReceipts_UserId",
                table: "AtomicCommandReceipts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntries_CreatedAtUtc",
                table: "AuditEntries",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntries_SubjectClientUserId_CreatedAtUtc",
                table: "AuditEntries",
                columns: ["SubjectClientUserId", "CreatedAtUtc"]);

            migrationBuilder.CreateIndex(
                name: "IX_BillingPayments_BillingSubscriptionId",
                table: "BillingPayments",
                column: "BillingSubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_BillingPayments_Provider_ExternalCustomerId",
                table: "BillingPayments",
                columns: ["Provider", "ExternalCustomerId"]);

            migrationBuilder.CreateIndex(
                name: "IX_BillingPayments_Provider_ExternalPaymentId",
                table: "BillingPayments",
                columns: ["Provider", "ExternalPaymentId"],
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BillingPayments_Provider_ExternalPaymentMethodId",
                table: "BillingPayments",
                columns: ["Provider", "ExternalPaymentMethodId"]);

            migrationBuilder.CreateIndex(
                name: "IX_BillingPayments_Provider_ExternalSubscriptionId",
                table: "BillingPayments",
                columns: ["Provider", "ExternalSubscriptionId"]);

            migrationBuilder.CreateIndex(
                name: "IX_BillingPayments_UserId",
                table: "BillingPayments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_BillingSubscriptions_Provider_ExternalCustomerId",
                table: "BillingSubscriptions",
                columns: ["Provider", "ExternalCustomerId"],
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BillingSubscriptions_Provider_ExternalPaymentMethodId",
                table: "BillingSubscriptions",
                columns: ["Provider", "ExternalPaymentMethodId"]);

            migrationBuilder.CreateIndex(
                name: "IX_BillingSubscriptions_Provider_ExternalSubscriptionId",
                table: "BillingSubscriptions",
                columns: ["Provider", "ExternalSubscriptionId"],
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BillingSubscriptions_UserId",
                table: "BillingSubscriptions",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BillingWebhookEvents_ProcessedAtUtc",
                table: "BillingWebhookEvents",
                column: "ProcessedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_BillingWebhookEvents_Provider_EventId",
                table: "BillingWebhookEvents",
                columns: ["Provider", "EventId"],
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BillingWebhookEvents_Status_NextAttemptAtUtc_ReceivedAtUtc",
                table: "BillingWebhookEvents",
                columns: ["Status", "NextAttemptAtUtc", "ReceivedAtUtc"]);

            migrationBuilder.CreateIndex(
                name: "IX_ClientTasks_ClientUserId_CreatedOnUtc",
                table: "ClientTasks",
                columns: ["ClientUserId", "CreatedOnUtc"]);

            migrationBuilder.CreateIndex(
                name: "IX_ClientTasks_DietologistUserId_ClientUserId",
                table: "ClientTasks",
                columns: ["DietologistUserId", "ClientUserId"]);

            migrationBuilder.CreateIndex(
                name: "IX_ClientTasks_Status_DueAtUtc_DueReminderSentAtUtc",
                table: "ClientTasks",
                columns: ["Status", "DueAtUtc", "DueReminderSentAtUtc"]);

            migrationBuilder.CreateIndex(
                name: "IX_ConsumedTelegramAssertions_ExpiresAtUtc",
                table: "ConsumedTelegramAssertions",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ContentReports_Status_CreatedOnUtc",
                table: "ContentReports",
                columns: ["Status", "CreatedOnUtc"]);

            migrationBuilder.CreateIndex(
                name: "IX_ContentReports_UserId_TargetType_TargetId",
                table: "ContentReports",
                columns: ["UserId", "TargetType", "TargetId"],
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CycleBleedingEntries_CycleProfileId_Date_Type",
                table: "CycleBleedingEntries",
                columns: ["CycleProfileId", "Date", "Type"],
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CycleConsents_CycleProfileId_Purpose",
                table: "CycleConsents",
                columns: ["CycleProfileId", "Purpose"],
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CycleDayNotes_CycleProfileId_Date",
                table: "CycleDayNotes",
                columns: ["CycleProfileId", "Date"],
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CycleFactors_CycleProfileId_Type_StartDate",
                table: "CycleFactors",
                columns: ["CycleProfileId", "Type", "StartDate"],
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CycleMenstrualEpisodes_CycleProfileId_StartDate",
                table: "CycleMenstrualEpisodes",
                columns: ["CycleProfileId", "StartDate"],
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CyclePredictionRevisions_CycleProfileId_GeneratedAtUtc",
                table: "CyclePredictionRevisions",
                columns: ["CycleProfileId", "GeneratedAtUtc"]);

            migrationBuilder.CreateIndex(
                name: "IX_CycleProfiles_UserId",
                table: "CycleProfiles",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CycleSymptomEntries_CycleProfileId_Date_Category",
                table: "CycleSymptomEntries",
                columns: ["CycleProfileId", "Date", "Category"],
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DailyAdvices_GroupId_Locale",
                table: "DailyAdvices",
                columns: ["GroupId", "Locale"],
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DailyAdvices_Locale_Tag",
                table: "DailyAdvices",
                columns: ["Locale", "Tag"]);

            migrationBuilder.CreateIndex(
                name: "IX_DailyReferenceValues_NutrientId_AgeGroup_Gender",
                table: "DailyReferenceValues",
                columns: ["NutrientId", "AgeGroup", "Gender"],
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DietologistInvitations_ClientUserId",
                table: "DietologistInvitations",
                column: "ClientUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DietologistInvitations_ClientUserId_Status",
                table: "DietologistInvitations",
                columns: ["ClientUserId", "Status"]);

            migrationBuilder.CreateIndex(
                name: "IX_DietologistInvitations_DietologistEmail_Status",
                table: "DietologistInvitations",
                columns: ["DietologistEmail", "Status"]);

            migrationBuilder.CreateIndex(
                name: "IX_DietologistInvitations_DietologistUserId",
                table: "DietologistInvitations",
                column: "DietologistUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailOutbox_DueLease",
                table: "EmailOutbox",
                columns: ["ProcessedOnUtc", "DeadLetteredOnUtc", "NextAttemptOnUtc", "LockedUntilUtc"]);

            migrationBuilder.CreateIndex(
                name: "IX_EmailTemplateRevisions_TemplateId_ArchivedOnUtc",
                table: "EmailTemplateRevisions",
                columns: ["TemplateId", "ArchivedOnUtc"]);

            migrationBuilder.CreateIndex(
                name: "IX_EmailTemplates_Key_Locale",
                table: "EmailTemplates",
                columns: ["Key", "Locale"],
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseEntries_UserId_Date",
                table: "ExerciseEntries",
                columns: ["UserId", "Date"]);

            migrationBuilder.CreateIndex(
                name: "IX_FastingCheckIns_OccurrenceId_CheckedInAtUtc",
                table: "FastingCheckIns",
                columns: ["OccurrenceId", "CheckedInAtUtc"]);

            migrationBuilder.CreateIndex(
                name: "IX_FastingCheckIns_UserId_CheckedInAtUtc",
                table: "FastingCheckIns",
                columns: ["UserId", "CheckedInAtUtc"]);

            migrationBuilder.CreateIndex(
                name: "IX_FastingOccurrences_PlanId",
                table: "FastingOccurrences",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_FastingOccurrences_PlanId_SequenceNumber",
                table: "FastingOccurrences",
                columns: ["PlanId", "SequenceNumber"],
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FastingOccurrences_StartedAtUtc_Active",
                table: "FastingOccurrences",
                column: "StartedAtUtc",
                filter: "\"Status\" = 'Active'");

            migrationBuilder.CreateIndex(
                name: "IX_FastingOccurrences_UserId",
                table: "FastingOccurrences",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_FastingOccurrences_UserId_StartedAtUtc",
                table: "FastingOccurrences",
                columns: ["UserId", "StartedAtUtc"]);

            migrationBuilder.CreateIndex(
                name: "IX_FastingOccurrences_UserId_Status",
                table: "FastingOccurrences",
                columns: ["UserId", "Status"]);

            migrationBuilder.CreateIndex(
                name: "IX_FastingPlans_UserId",
                table: "FastingPlans",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_FastingPlans_UserId_Status",
                table: "FastingPlans",
                columns: ["UserId", "Status"]);

            migrationBuilder.CreateIndex(
                name: "IX_FastingSessions_UserId",
                table: "FastingSessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_FastingSessions_UserId_IsCompleted",
                table: "FastingSessions",
                columns: ["UserId", "IsCompleted"]);

            migrationBuilder.CreateIndex(
                name: "IX_FastingTelemetryEvents_Name_OccurredAtUtc",
                table: "FastingTelemetryEvents",
                columns: ["Name", "OccurredAtUtc"]);

            migrationBuilder.CreateIndex(
                name: "IX_FastingTelemetryEvents_OccurredAtUtc",
                table: "FastingTelemetryEvents",
                column: "OccurredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_FastingTelemetryEvents_ReminderPresetId_Name_OccurredAtUtc",
                table: "FastingTelemetryEvents",
                columns: ["ReminderPresetId", "Name", "OccurredAtUtc"]);

            migrationBuilder.CreateIndex(
                name: "IX_FavoriteMeals_MealId",
                table: "FavoriteMeals",
                column: "MealId");

            migrationBuilder.CreateIndex(
                name: "IX_FavoriteMeals_UserId",
                table: "FavoriteMeals",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_FavoriteMeals_UserId_MealId",
                table: "FavoriteMeals",
                columns: ["UserId", "MealId"],
                unique: true,
                filter: "\"RemovedAtUtc\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_FavoriteProducts_ProductId",
                table: "FavoriteProducts",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_FavoriteProducts_UserId",
                table: "FavoriteProducts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_FavoriteProducts_UserId_ProductId",
                table: "FavoriteProducts",
                columns: ["UserId", "ProductId"],
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FavoriteRecipes_RecipeId",
                table: "FavoriteRecipes",
                column: "RecipeId");

            migrationBuilder.CreateIndex(
                name: "IX_FavoriteRecipes_UserId",
                table: "FavoriteRecipes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_FavoriteRecipes_UserId_RecipeId",
                table: "FavoriteRecipes",
                columns: ["UserId", "RecipeId"],
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FertilitySignals_CycleProfileId_Date",
                table: "FertilitySignals",
                columns: ["CycleProfileId", "Date"],
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FoodRecognitionJobImages_ImageAssetId",
                table: "FoodRecognitionJobImages",
                column: "ImageAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_FoodRecognitionJobImages_JobId_Position",
                table: "FoodRecognitionJobImages",
                columns: ["JobId", "Position"],
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FoodRecognitionJobs_ImageAssetId",
                table: "FoodRecognitionJobs",
                column: "ImageAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_FoodRecognitionJobs_Status_CreatedOnUtc",
                table: "FoodRecognitionJobs",
                columns: ["Status", "CreatedOnUtc"]);

            migrationBuilder.CreateIndex(
                name: "IX_FoodRecognitionJobs_UpdatedOnUtc",
                table: "FoodRecognitionJobs",
                column: "UpdatedOnUtc");

            migrationBuilder.CreateIndex(
                name: "IX_FoodRecognitionJobs_UserId_CreatedOnUtc",
                table: "FoodRecognitionJobs",
                columns: ["UserId", "CreatedOnUtc"]);

            migrationBuilder.CreateIndex(
                name: "IX_HydrationEntries_User_Timestamp",
                table: "HydrationEntries",
                columns: ["UserId", "Timestamp"]);

            migrationBuilder.CreateIndex(
                name: "IX_HydrationOperationReceipts_EntryId",
                table: "HydrationOperationReceipts",
                column: "EntryId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImageAssets_UserId",
                table: "ImageAssets",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ImageObjectDeletionOutbox_DueLease",
                table: "ImageObjectDeletionOutbox",
                columns: ["ProcessedOnUtc", "DeadLetteredOnUtc", "NextAttemptOnUtc", "LockedUntilUtc"]);

            migrationBuilder.CreateIndex(
                name: "IX_MarketingAttributionEvents_AnonymousId_OccurredAtUtc",
                table: "MarketingAttributionEvents",
                columns: ["AnonymousId", "OccurredAtUtc"]);

            migrationBuilder.CreateIndex(
                name: "IX_MarketingAttributionEvents_OccurredAtUtc",
                table: "MarketingAttributionEvents",
                column: "OccurredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_MarketingAttributionEvents_SessionId_OccurredAtUtc",
                table: "MarketingAttributionEvents",
                columns: ["SessionId", "OccurredAtUtc"]);

            migrationBuilder.CreateIndex(
                name: "IX_MarketingAttributionEvents_UserId_EventType",
                table: "MarketingAttributionEvents",
                columns: ["UserId", "EventType"],
                unique: true,
                filter: "\"UserId\" IS NOT NULL AND \"EventType\" IN ('signup_completed', 'premium_started')");

            migrationBuilder.CreateIndex(
                name: "IX_MarketingAttributionEvents_UserId_OccurredAtUtc",
                table: "MarketingAttributionEvents",
                columns: ["UserId", "OccurredAtUtc"]);

            migrationBuilder.CreateIndex(
                name: "IX_MarketingAttributionEvents_UtmSource_UtmMedium_UtmCampaign_~",
                table: "MarketingAttributionEvents",
                columns: ["UtmSource", "UtmMedium", "UtmCampaign", "OccurredAtUtc"]);

            migrationBuilder.CreateIndex(
                name: "IX_MealAiItems_MealAiSessionId",
                table: "MealAiItems",
                column: "MealAiSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_MealAiSessions_ImageAssetId",
                table: "MealAiSessions",
                column: "ImageAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_MealAiSessions_MealId",
                table: "MealAiSessions",
                column: "MealId");

            migrationBuilder.CreateIndex(
                name: "IX_MealItems_MealId",
                table: "MealItems",
                column: "MealId");

            migrationBuilder.CreateIndex(
                name: "IX_MealItems_ProductId",
                table: "MealItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_MealItems_RecipeId",
                table: "MealItems",
                column: "RecipeId");

            migrationBuilder.CreateIndex(
                name: "IX_MealPlanDays_MealPlanId_DayNumber",
                table: "MealPlanDays",
                columns: ["MealPlanId", "DayNumber"],
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MealPlanMeals_MealPlanDayId",
                table: "MealPlanMeals",
                column: "MealPlanDayId");

            migrationBuilder.CreateIndex(
                name: "IX_MealPlanMeals_RecipeId",
                table: "MealPlanMeals",
                column: "RecipeId");

            migrationBuilder.CreateIndex(
                name: "IX_MealPlans_DietType",
                table: "MealPlans",
                column: "DietType");

            migrationBuilder.CreateIndex(
                name: "IX_MealPlans_IsCurated",
                table: "MealPlans",
                column: "IsCurated");

            migrationBuilder.CreateIndex(
                name: "IX_MealPlans_UserId",
                table: "MealPlans",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MealRecognitionReceipts_UserId_RecognitionId",
                table: "MealRecognitionReceipts",
                columns: ["UserId", "RecognitionId"],
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Meals_ImageAssetId",
                table: "Meals",
                column: "ImageAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_Meals_UserId_Date_CreatedOnUtc",
                table: "Meals",
                columns: ["UserId", "Date", "CreatedOnUtc"]);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_IsRead",
                table: "Notifications",
                columns: ["UserId", "IsRead"]);

            migrationBuilder.CreateIndex(
                name: "IX_NotificationWebPushOutbox_DueLease",
                table: "NotificationWebPushOutbox",
                columns: ["ProcessedOnUtc", "DeadLetteredOnUtc", "NextAttemptOnUtc", "LockedUntilUtc"]);

            migrationBuilder.CreateIndex(
                name: "IX_NotificationWebPushOutbox_NotificationId",
                table: "NotificationWebPushOutbox",
                column: "NotificationId");

            migrationBuilder.CreateIndex(
                name: "IX_NutritionLessons_Locale_Category",
                table: "NutritionLessons",
                columns: ["Locale", "Category"]);

            migrationBuilder.CreateIndex(
                name: "IX_OpenFoodFactsProducts_Brand",
                table: "OpenFoodFactsProducts",
                column: "Brand")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_OpenFoodFactsProducts_Category",
                table: "OpenFoodFactsProducts",
                column: "Category")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_OpenFoodFactsProducts_LastSeenAtUtc",
                table: "OpenFoodFactsProducts",
                column: "LastSeenAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_OpenFoodFactsProducts_Name",
                table: "OpenFoodFactsProducts",
                column: "Name")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxReplayAudits_OutboxName_MessageId_RequestedOnUtc",
                table: "OutboxReplayAudits",
                columns: ["OutboxName", "MessageId", "RequestedOnUtc"]);

            migrationBuilder.CreateIndex(
                name: "IX_ProductImages_ImageAssetId",
                table: "ProductImages",
                column: "ImageAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Barcode",
                table: "Products",
                column: "Barcode")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_Brand",
                table: "Products",
                column: "Brand")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_Category",
                table: "Products",
                column: "Category")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_ImageAssetId",
                table: "Products",
                column: "ImageAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Name",
                table: "Products",
                column: "Name")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_UsdaFdcId",
                table: "Products",
                column: "UsdaFdcId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_UserId_CreatedOnUtc",
                table: "Products",
                columns: ["UserId", "CreatedOnUtc"]);

            migrationBuilder.CreateIndex(
                name: "IX_Products_Visibility_CreatedOnUtc",
                table: "Products",
                columns: ["Visibility", "CreatedOnUtc"]);

            migrationBuilder.CreateIndex(
                name: "IX_RecentItems_UserId_ItemType_ItemId",
                table: "RecentItems",
                columns: ["UserId", "ItemType", "ItemId"],
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecentItems_UserId_ItemType_LastUsedAtUtc",
                table: "RecentItems",
                columns: ["UserId", "ItemType", "LastUsedAtUtc"]);

            migrationBuilder.CreateIndex(
                name: "IX_RecipeComments_RecipeId_CreatedOnUtc",
                table: "RecipeComments",
                columns: ["RecipeId", "CreatedOnUtc"]);

            migrationBuilder.CreateIndex(
                name: "IX_RecipeComments_UserId",
                table: "RecipeComments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RecipeImages_ImageAssetId",
                table: "RecipeImages",
                column: "ImageAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_RecipeIngredients_NestedRecipeId",
                table: "RecipeIngredients",
                column: "NestedRecipeId");

            migrationBuilder.CreateIndex(
                name: "IX_RecipeIngredients_ProductId",
                table: "RecipeIngredients",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_RecipeIngredients_RecipeStepId",
                table: "RecipeIngredients",
                column: "RecipeStepId");

            migrationBuilder.CreateIndex(
                name: "IX_RecipeLikes_RecipeId",
                table: "RecipeLikes",
                column: "RecipeId");

            migrationBuilder.CreateIndex(
                name: "IX_RecipeLikes_UserId_RecipeId",
                table: "RecipeLikes",
                columns: ["UserId", "RecipeId"],
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_Category",
                table: "Recipes",
                column: "Category")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_Description",
                table: "Recipes",
                column: "Description")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_ImageAssetId",
                table: "Recipes",
                column: "ImageAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_Name",
                table: "Recipes",
                column: "Name")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_UserId_CreatedOnUtc",
                table: "Recipes",
                columns: ["UserId", "CreatedOnUtc"]);

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_Visibility_CreatedOnUtc",
                table: "Recipes",
                columns: ["Visibility", "CreatedOnUtc"]);

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_Visibility_Language_CreatedOnUtc",
                table: "Recipes",
                columns: ["Visibility", "Language", "CreatedOnUtc"]);

            migrationBuilder.CreateIndex(
                name: "IX_RecipeStepImages_ImageAssetId",
                table: "RecipeStepImages",
                column: "ImageAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_RecipeSteps_ImageAssetId",
                table: "RecipeSteps",
                column: "ImageAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_RecipeSteps_RecipeId",
                table: "RecipeSteps",
                column: "RecipeId");

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationBulkDispatches_ClientUserId",
                table: "RecommendationBulkDispatches",
                column: "ClientUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationBulkDispatches_DietologistUserId_IdempotencyK~",
                table: "RecommendationBulkDispatches",
                columns: ["DietologistUserId", "IdempotencyKey", "ClientUserId"],
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationBulkDispatches_RecommendationId",
                table: "RecommendationBulkDispatches",
                column: "RecommendationId");

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationComments_AuthorUserId",
                table: "RecommendationComments",
                column: "AuthorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationComments_RecommendationId_CreatedOnUtc",
                table: "RecommendationComments",
                columns: ["RecommendationId", "CreatedOnUtc"]);

            migrationBuilder.CreateIndex(
                name: "IX_Recommendations_ClientUserId",
                table: "Recommendations",
                column: "ClientUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Recommendations_DietologistUserId_ClientUserId",
                table: "Recommendations",
                columns: ["DietologistUserId", "ClientUserId"]);

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationTemplates_DietologistUserId_IsArchived_Name",
                table: "RecommendationTemplates",
                columns: ["DietologistUserId", "IsArchived", "Name"]);

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Name",
                table: "Roles",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingListItems_ProductId",
                table: "ShoppingListItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingListItems_ShoppingListId",
                table: "ShoppingListItems",
                column: "ShoppingListId");

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingListItemSources_MealPlanId",
                table: "ShoppingListItemSources",
                column: "MealPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingListItemSources_RecipeId",
                table: "ShoppingListItemSources",
                column: "RecipeId");

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingListItemSources_ShoppingListItemId",
                table: "ShoppingListItemSources",
                column: "ShoppingListItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingLists_UserId",
                table: "ShoppingLists",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TelegramLoginTickets_ExpiresAtUtc",
                table: "TelegramLoginTickets",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_TelegramOperations_BotId_Completed_NextAttemptAtUtc",
                table: "TelegramOperations",
                columns: ["BotId", "Completed", "NextAttemptAtUtc"]);

            migrationBuilder.CreateIndex(
                name: "IX_TelegramOperations_BotId_UpdateId",
                table: "TelegramOperations",
                columns: ["BotId", "UpdateId"],
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TelegramOperations_UserId",
                table: "TelegramOperations",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UsdaFoodNutrients_FdcId_NutrientId",
                table: "UsdaFoodNutrients",
                columns: ["FdcId", "NutrientId"],
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UsdaFoodNutrients_NutrientId",
                table: "UsdaFoodNutrients",
                column: "NutrientId");

            migrationBuilder.CreateIndex(
                name: "IX_UsdaFoodPortions_FdcId",
                table: "UsdaFoodPortions",
                column: "FdcId");

            migrationBuilder.CreateIndex(
                name: "IX_UsdaFoods_Description",
                table: "UsdaFoods",
                column: "Description")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_UsdaFoods_FoodCategoryId",
                table: "UsdaFoods",
                column: "FoodCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAchievements_UserId_AchievementKey",
                table: "UserAchievements",
                columns: ["UserId", "AchievementKey"],
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserAchievements_UserId_EarnedAtUtc",
                table: "UserAchievements",
                columns: ["UserId", "EarnedAtUtc"]);

            migrationBuilder.CreateIndex(
                name: "IX_UserLessonProgress_LessonId",
                table: "UserLessonProgress",
                column: "LessonId");

            migrationBuilder.CreateIndex(
                name: "IX_UserLessonProgress_UserId_LessonId",
                table: "UserLessonProgress",
                columns: ["UserId", "LessonId"],
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserLoginEvents_AuthProvider",
                table: "UserLoginEvents",
                column: "AuthProvider");

            migrationBuilder.CreateIndex(
                name: "IX_UserLoginEvents_BrowserName",
                table: "UserLoginEvents",
                column: "BrowserName");

            migrationBuilder.CreateIndex(
                name: "IX_UserLoginEvents_DeviceType",
                table: "UserLoginEvents",
                column: "DeviceType");

            migrationBuilder.CreateIndex(
                name: "IX_UserLoginEvents_LoggedInAtUtc",
                table: "UserLoginEvents",
                column: "LoggedInAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_UserLoginEvents_OperatingSystem",
                table: "UserLoginEvents",
                column: "OperatingSystem");

            migrationBuilder.CreateIndex(
                name: "IX_UserLoginEvents_UserId",
                table: "UserLoginEvents",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRefreshTokenSessions_LastRotatedAtUtc",
                table: "UserRefreshTokenSessions",
                column: "LastRotatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_UserRefreshTokenSessions_RevokedAtUtc",
                table: "UserRefreshTokenSessions",
                column: "RevokedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_UserRefreshTokenSessions_UserId",
                table: "UserRefreshTokenSessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleAuditEvents_Action",
                table: "UserRoleAuditEvents",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleAuditEvents_ActorUserId",
                table: "UserRoleAuditEvents",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleAuditEvents_OccurredAtUtc",
                table: "UserRoleAuditEvents",
                column: "OccurredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleAuditEvents_RoleId",
                table: "UserRoleAuditEvents",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleAuditEvents_RoleName",
                table: "UserRoleAuditEvents",
                column: "RoleName");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleAuditEvents_UserId",
                table: "UserRoleAuditEvents",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                table: "UserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_GoogleIssuer_GoogleSubject",
                table: "Users",
                columns: ["GoogleIssuer", "GoogleSubject"],
                unique: true,
                filter: "\"GoogleIssuer\" IS NOT NULL AND \"GoogleSubject\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Users_ProfileImageAssetId",
                table: "Users",
                column: "ProfileImageAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_TelegramOidcIssuer_TelegramOidcSubject",
                table: "Users",
                columns: ["TelegramOidcIssuer", "TelegramOidcSubject"],
                unique: true,
                filter: "\"TelegramOidcIssuer\" IS NOT NULL AND \"TelegramOidcSubject\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Users_TelegramUserId",
                table: "Users",
                column: "TelegramUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WaistEntries_UserId_Date",
                table: "WaistEntries",
                columns: ["UserId", "Date"],
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WaistGoals_UserId",
                table: "WaistGoals",
                column: "UserId",
                unique: true,
                filter: "\"Status\" = 'Active'");

            migrationBuilder.CreateIndex(
                name: "IX_WearableConnections_UserId_Provider",
                table: "WearableConnections",
                columns: ["UserId", "Provider"],
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WearableSyncEntries_UserId_Date",
                table: "WearableSyncEntries",
                columns: ["UserId", "Date"]);

            migrationBuilder.CreateIndex(
                name: "IX_WearableSyncEntries_UserId_Provider_DataType_Date",
                table: "WearableSyncEntries",
                columns: ["UserId", "Provider", "DataType", "Date"],
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WebPushSubscriptions_Endpoint",
                table: "WebPushSubscriptions",
                column: "Endpoint",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WebPushSubscriptions_UserId",
                table: "WebPushSubscriptions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyGoals_ReminderEnabled_WeekStartUtc",
                table: "WeeklyGoals",
                columns: ["ReminderEnabled", "WeekStartUtc"]);

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyGoals_UserId_WeekStartUtc",
                table: "WeeklyGoals",
                columns: ["UserId", "WeekStartUtc"],
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WeightEntries_UserId_Date",
                table: "WeightEntries",
                columns: ["UserId", "Date"],
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WeightGoals_UserId",
                table: "WeightGoals",
                column: "UserId",
                unique: true,
                filter: "\"Status\" = 'Active'");

            migrationBuilder.AddForeignKey(
                name: "FK_AdminImpersonationSessions_Users_ActorUserId",
                table: "AdminImpersonationSessions",
                column: "ActorUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AdminImpersonationSessions_Users_TargetUserId",
                table: "AdminImpersonationSessions",
                column: "TargetUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AiQuotaPeriods_Users_UserId",
                table: "AiQuotaPeriods",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AiUsages_Users_UserId",
                table: "AiUsages",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BillingPayments_BillingSubscriptions_BillingSubscriptionId",
                table: "BillingPayments",
                column: "BillingSubscriptionId",
                principalTable: "BillingSubscriptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_BillingPayments_Users_UserId",
                table: "BillingPayments",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BillingSubscriptions_Users_UserId",
                table: "BillingSubscriptions",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ClientTasks_Users_ClientUserId",
                table: "ClientTasks",
                column: "ClientUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ClientTasks_Users_DietologistUserId",
                table: "ClientTasks",
                column: "DietologistUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ContentReports_Users_UserId",
                table: "ContentReports",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CycleBleedingEntries_CycleProfiles_CycleProfileId",
                table: "CycleBleedingEntries",
                column: "CycleProfileId",
                principalTable: "CycleProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CycleConsents_CycleProfiles_CycleProfileId",
                table: "CycleConsents",
                column: "CycleProfileId",
                principalTable: "CycleProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CycleDayNotes_CycleProfiles_CycleProfileId",
                table: "CycleDayNotes",
                column: "CycleProfileId",
                principalTable: "CycleProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CycleFactors_CycleProfiles_CycleProfileId",
                table: "CycleFactors",
                column: "CycleProfileId",
                principalTable: "CycleProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CycleMenstrualEpisodes_CycleProfiles_CycleProfileId",
                table: "CycleMenstrualEpisodes",
                column: "CycleProfileId",
                principalTable: "CycleProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CyclePredictionRevisions_CycleProfiles_CycleProfileId",
                table: "CyclePredictionRevisions",
                column: "CycleProfileId",
                principalTable: "CycleProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CycleProfiles_Users_UserId",
                table: "CycleProfiles",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DietologistInvitations_Users_ClientUserId",
                table: "DietologistInvitations",
                column: "ClientUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DietologistInvitations_Users_DietologistUserId",
                table: "DietologistInvitations",
                column: "DietologistUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ExerciseEntries_Users_UserId",
                table: "ExerciseEntries",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FastingCheckIns_FastingOccurrences_OccurrenceId",
                table: "FastingCheckIns",
                column: "OccurrenceId",
                principalTable: "FastingOccurrences",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FastingCheckIns_Users_UserId",
                table: "FastingCheckIns",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FastingOccurrences_FastingPlans_PlanId",
                table: "FastingOccurrences",
                column: "PlanId",
                principalTable: "FastingPlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FastingOccurrences_Users_UserId",
                table: "FastingOccurrences",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FastingPlans_Users_UserId",
                table: "FastingPlans",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FastingSessions_Users_UserId",
                table: "FastingSessions",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FavoriteMeals_Meals_MealId",
                table: "FavoriteMeals",
                column: "MealId",
                principalTable: "Meals",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FavoriteMeals_Users_UserId",
                table: "FavoriteMeals",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FavoriteProducts_Products_ProductId",
                table: "FavoriteProducts",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FavoriteProducts_Users_UserId",
                table: "FavoriteProducts",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FavoriteRecipes_Recipes_RecipeId",
                table: "FavoriteRecipes",
                column: "RecipeId",
                principalTable: "Recipes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FavoriteRecipes_Users_UserId",
                table: "FavoriteRecipes",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FoodRecognitionJobImages_FoodRecognitionJobs_JobId",
                table: "FoodRecognitionJobImages",
                column: "JobId",
                principalTable: "FoodRecognitionJobs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FoodRecognitionJobImages_ImageAssets_ImageAssetId",
                table: "FoodRecognitionJobImages",
                column: "ImageAssetId",
                principalTable: "ImageAssets",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_FoodRecognitionJobs_ImageAssets_ImageAssetId",
                table: "FoodRecognitionJobs",
                column: "ImageAssetId",
                principalTable: "ImageAssets",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_FoodRecognitionJobs_Users_UserId",
                table: "FoodRecognitionJobs",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_HydrationEntries_Users_UserId",
                table: "HydrationEntries",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_HydrationOperationReceipts_Users_UserId",
                table: "HydrationOperationReceipts",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ImageAssets_Users_UserId",
                table: "ImageAssets",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            // Historical database defaults are not all represented in the EF model.
            // Keep fresh and already deployed schemas equivalent without changing model behavior.
            migrationBuilder.Sql("""
                ALTER TABLE "AchievementEvaluationOutbox" ALTER COLUMN "Revision" SET DEFAULT 0;
                ALTER TABLE "BillingWebhookEvents" ALTER COLUMN "AttemptCount" SET DEFAULT 0;
                ALTER TABLE "CycleProfiles" ALTER COLUMN "HideFromDashboard" SET DEFAULT false;
                ALTER TABLE "DietologistInvitations" ALTER COLUMN "ShareFasting" SET DEFAULT true;
                ALTER TABLE "DietologistInvitations" ALTER COLUMN "ShareProfile" SET DEFAULT true;
                ALTER TABLE "EmailOutbox" ALTER COLUMN "AutoSubmitted" SET DEFAULT false;
                ALTER TABLE "FastingSessions" ALTER COLUMN "AddedDurationHours" SET DEFAULT 0;
                ALTER TABLE "FastingSessions" ALTER COLUMN "InitialPlannedDurationHours" SET DEFAULT 0;
                ALTER TABLE "FoodRecognitionJobs" ALTER COLUMN "IsProductLabel" SET DEFAULT false;
                ALTER TABLE "Meals" ALTER COLUMN "PostMealSatietyLevel" SET DEFAULT 0;
                ALTER TABLE "Meals" ALTER COLUMN "PreMealSatietyLevel" SET DEFAULT 0;
                ALTER TABLE "Meals" ALTER COLUMN "TotalAlcohol" SET DEFAULT 0.0;
                ALTER TABLE "Meals" ALTER COLUMN "TotalCalories" SET DEFAULT 0.0;
                ALTER TABLE "Meals" ALTER COLUMN "TotalCarbs" SET DEFAULT 0.0;
                ALTER TABLE "Meals" ALTER COLUMN "TotalFats" SET DEFAULT 0.0;
                ALTER TABLE "Meals" ALTER COLUMN "TotalFiber" SET DEFAULT 0.0;
                ALTER TABLE "Meals" ALTER COLUMN "TotalProteins" SET DEFAULT 0.0;
                ALTER TABLE "Notifications" ALTER COLUMN "PayloadJson" SET DEFAULT '{}'::character varying;
                ALTER TABLE "Products" ALTER COLUMN "AlcoholPerBase" SET DEFAULT 0.0;
                ALTER TABLE "Products" ALTER COLUMN "DefaultPortionAmount" SET DEFAULT 0.0;
                ALTER TABLE "RecipeIngredients" ALTER COLUMN "Position" SET DEFAULT 0;
                ALTER TABLE "Recipes" ALTER COLUMN "LanguageConfirmed" SET DEFAULT false;
                ALTER TABLE "Recipes" ALTER COLUMN "MissingIngredientCount" SET DEFAULT 0;
                ALTER TABLE "Users" ALTER COLUMN "TelegramLinkVerifiedForRecovery" SET DEFAULT false;
                """);

            // This expression index was maintained through raw SQL in the old chain.
            migrationBuilder.Sql("""
                CREATE INDEX "IX_BillingPayments_EffectiveOccurredAtUtc" ON "BillingPayments"
                    (COALESCE("OccurredAtUtc", "CreatedOnUtc"))
                    WHERE "Amount" IS NOT NULL AND "Currency" IS NOT NULL;
                """);

            using Stream seed = typeof(InitialBaseline).Assembly.GetManifestResourceStream(
                "FoodDiary.Infrastructure.Migrations.InitialBaseline.Seed.sql")
                ?? throw new InvalidOperationException("The initial baseline seed resource is missing.");
            using var reader = new StreamReader(seed);
            migrationBuilder.Sql(reader.ReadToEnd());
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) =>
            throw new NotSupportedException("The squashed baseline cannot be rolled back. Restore a verified database backup instead.");
    }
}
