-- Frozen PostgreSQL 17 result of the 185 migrations at c8b050e39253a53c183d8076d0c19535e153151d.
-- Generated only from an empty disposable database; no user or production data.
SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

CREATE EXTENSION IF NOT EXISTS pg_trgm WITH SCHEMA public;

COMMENT ON EXTENSION pg_trgm IS 'text similarity measurement and index searching based on trigrams';

SET default_tablespace = '';

SET default_table_access_method = heap;

CREATE TABLE public."AchievementDefinitions" (
    "Id" uuid NOT NULL,
    "Key" character varying(100) NOT NULL,
    "Category" character varying(50) NOT NULL,
    "Metric" character varying(30) NOT NULL,
    "Threshold" integer NOT NULL,
    "TitleRu" character varying(160) NOT NULL,
    "TitleEn" character varying(160) NOT NULL,
    "DescriptionRu" character varying(500) NOT NULL,
    "DescriptionEn" character varying(500) NOT NULL,
    "Icon" character varying(50) NOT NULL,
    "SortOrder" integer NOT NULL,
    "IsActive" boolean NOT NULL,
    "Version" integer NOT NULL,
    "CreatedOnUtc" timestamp with time zone NOT NULL,
    "ModifiedOnUtc" timestamp with time zone
);

CREATE TABLE public."AchievementEvaluationOutbox" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "CreatedOnUtc" timestamp with time zone NOT NULL,
    "NextAttemptOnUtc" timestamp with time zone NOT NULL,
    "AttemptCount" integer NOT NULL,
    "ProcessedOnUtc" timestamp with time zone,
    "DeadLetteredOnUtc" timestamp with time zone,
    "LockedUntilUtc" timestamp with time zone,
    "LockedBy" character varying(128),
    "LastError" character varying(2048),
    "Revision" bigint DEFAULT 0 NOT NULL
);

CREATE TABLE public."AdminImpersonationSessions" (
    "Id" uuid NOT NULL,
    "ActorUserId" uuid NOT NULL,
    "TargetUserId" uuid NOT NULL,
    "Reason" character varying(500) NOT NULL,
    "ActorIpAddress" character varying(128),
    "ActorUserAgent" character varying(512),
    "StartedAtUtc" timestamp with time zone NOT NULL,
    "CreatedOnUtc" timestamp with time zone NOT NULL,
    "ModifiedOnUtc" timestamp with time zone
);

CREATE TABLE public."AiPromptRevisions" (
    "Id" uuid NOT NULL,
    "PromptText" character varying(4096) NOT NULL,
    "Version" integer NOT NULL,
    "IsActive" boolean NOT NULL,
    "SavedOnUtc" timestamp with time zone NOT NULL,
    "ArchivedOnUtc" timestamp with time zone NOT NULL,
    "TemplateId" uuid NOT NULL
);

CREATE TABLE public."AiPromptTemplates" (
    "Id" uuid NOT NULL,
    "Key" character varying(64) NOT NULL,
    "Locale" character varying(8) NOT NULL,
    "PromptText" character varying(4096) NOT NULL,
    "Version" integer NOT NULL,
    "IsActive" boolean NOT NULL,
    "CreatedOnUtc" timestamp with time zone NOT NULL,
    "ModifiedOnUtc" timestamp with time zone
);

CREATE TABLE public."AiQuotaPeriods" (
    "UserId" uuid NOT NULL,
    "PeriodStartUtc" timestamp with time zone NOT NULL,
    "ConsumedInputTokens" bigint NOT NULL,
    "ConsumedOutputTokens" bigint NOT NULL,
    "ReservedInputTokens" bigint NOT NULL,
    "ReservedOutputTokens" bigint NOT NULL,
    "UpdatedOnUtc" timestamp with time zone NOT NULL,
    CONSTRAINT "CK_AiQuotaPeriods_ConsumedInputTokens" CHECK (("ConsumedInputTokens" >= 0)),
    CONSTRAINT "CK_AiQuotaPeriods_ConsumedOutputTokens" CHECK (("ConsumedOutputTokens" >= 0)),
    CONSTRAINT "CK_AiQuotaPeriods_ReservedInputTokens" CHECK (("ReservedInputTokens" >= 0)),
    CONSTRAINT "CK_AiQuotaPeriods_ReservedOutputTokens" CHECK (("ReservedOutputTokens" >= 0))
);

CREATE TABLE public."AiQuotaReservations" (
    "RequestId" character varying(64) NOT NULL,
    "UserId" uuid NOT NULL,
    "PeriodStartUtc" timestamp with time zone NOT NULL,
    "Operation" character varying(32) NOT NULL,
    "ReservedInputTokens" bigint NOT NULL,
    "ReservedOutputTokens" bigint NOT NULL,
    "ActualInputTokens" bigint,
    "ActualOutputTokens" bigint,
    "State" character varying(16) NOT NULL,
    "ExpiresOnUtc" timestamp with time zone NOT NULL,
    "CreatedOnUtc" timestamp with time zone NOT NULL,
    "UpdatedOnUtc" timestamp with time zone NOT NULL,
    CONSTRAINT "CK_AiQuotaReservations_ReservedInputTokens" CHECK (("ReservedInputTokens" >= 0)),
    CONSTRAINT "CK_AiQuotaReservations_ReservedOutputTokens" CHECK (("ReservedOutputTokens" >= 0))
);

CREATE TABLE public."AiUsages" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "Operation" character varying(32) NOT NULL,
    "Model" character varying(64) NOT NULL,
    "InputTokens" integer NOT NULL,
    "OutputTokens" integer NOT NULL,
    "TotalTokens" integer NOT NULL,
    "CreatedOnUtc" timestamp with time zone NOT NULL,
    "ModifiedOnUtc" timestamp with time zone
);

CREATE TABLE public."AtomicCommandReceipts" (
    "Key" character varying(64) NOT NULL,
    "UserId" uuid NOT NULL,
    "RequestHash" character varying(64) NOT NULL,
    "ResponseType" character varying(512) NOT NULL,
    "ResponseJson" jsonb NOT NULL,
    "ExpiresOnUtc" timestamp with time zone NOT NULL
);

CREATE TABLE public."AuditEntries" (
    "Id" uuid NOT NULL,
    "ActorUserId" uuid NOT NULL,
    "SubjectClientUserId" uuid,
    "Action" character varying(120) NOT NULL,
    "TargetType" character varying(80) NOT NULL,
    "TargetId" character varying(100),
    "Metadata" character varying(1000),
    "CreatedAtUtc" timestamp with time zone NOT NULL
);

CREATE TABLE public."BillingPayments" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "BillingSubscriptionId" uuid,
    "Provider" character varying(32) NOT NULL,
    "ExternalPaymentId" character varying(255) NOT NULL,
    "ExternalCustomerId" character varying(255),
    "ExternalSubscriptionId" character varying(255),
    "ExternalPaymentMethodId" character varying(255),
    "ExternalPriceId" character varying(255),
    "Plan" character varying(32),
    "Status" character varying(64) NOT NULL,
    "Kind" character varying(32) NOT NULL,
    "Amount" numeric(19,3),
    "Currency" character varying(3),
    "CurrentPeriodStartUtc" timestamp with time zone,
    "CurrentPeriodEndUtc" timestamp with time zone,
    "WebhookEventId" character varying(255),
    "ProviderMetadataJson" jsonb,
    "CreatedOnUtc" timestamp with time zone NOT NULL,
    "ModifiedOnUtc" timestamp with time zone,
    "Earnings" numeric(19,3),
    "Fee" numeric(19,3),
    "PayoutCurrency" character varying(3),
    "PayoutEarnings" numeric(19,3),
    "Tax" numeric(19,3),
    "OccurredAtUtc" timestamp with time zone
);

CREATE TABLE public."BillingSubscriptions" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "Provider" character varying(32) NOT NULL,
    "ExternalCustomerId" character varying(255) NOT NULL,
    "ExternalSubscriptionId" character varying(255),
    "ExternalPriceId" character varying(255),
    "Plan" character varying(32),
    "Status" character varying(64) NOT NULL,
    "CurrentPeriodStartUtc" timestamp with time zone,
    "CurrentPeriodEndUtc" timestamp with time zone,
    "CancelAtPeriodEnd" boolean NOT NULL,
    "CanceledAtUtc" timestamp with time zone,
    "TrialStartUtc" timestamp with time zone,
    "TrialEndUtc" timestamp with time zone,
    "LastWebhookEventId" character varying(255),
    "LastSyncedAtUtc" timestamp with time zone,
    "CreatedOnUtc" timestamp with time zone NOT NULL,
    "ModifiedOnUtc" timestamp with time zone,
    "ExternalPaymentMethodId" character varying(255),
    "NextBillingAttemptUtc" timestamp with time zone,
    "ProviderMetadataJson" jsonb,
    "PremiumRoleManagedByBilling" boolean DEFAULT false NOT NULL,
    "LastWebhookOccurredAtUtc" timestamp with time zone
);

CREATE TABLE public."BillingWebhookEvents" (
    "Id" uuid NOT NULL,
    "Provider" character varying(32) NOT NULL,
    "EventId" character varying(255) NOT NULL,
    "EventType" character varying(128) NOT NULL,
    "ExternalObjectId" character varying(255),
    "Status" character varying(32) NOT NULL,
    "ProcessedAtUtc" timestamp with time zone,
    "PayloadJson" jsonb,
    "ErrorMessage" character varying(1024),
    "CreatedOnUtc" timestamp with time zone NOT NULL,
    "ModifiedOnUtc" timestamp with time zone,
    "AttemptCount" integer DEFAULT 0 NOT NULL,
    "NextAttemptAtUtc" timestamp with time zone,
    "ParsedEventJson" jsonb,
    "ReceivedAtUtc" timestamp with time zone NOT NULL
);

CREATE TABLE public."BugAcknowledgementReceipts" (
    "InboxId" uuid NOT NULL
);

CREATE TABLE public."ClientTasks" (
    "Id" uuid NOT NULL,
    "DietologistUserId" uuid NOT NULL,
    "ClientUserId" uuid NOT NULL,
    "Title" character varying(200) NOT NULL,
    "Details" character varying(2000),
    "DueAtUtc" timestamp with time zone,
    "Status" character varying(32) NOT NULL,
    "StatusChangedAtUtc" timestamp with time zone,
    "CreatedOnUtc" timestamp with time zone NOT NULL,
    "ModifiedOnUtc" timestamp with time zone,
    "DueReminderSentAtUtc" timestamp with time zone
);

CREATE TABLE public."ConsumedTelegramAssertions" (
    "Fingerprint" character varying(67) NOT NULL,
    "ExpiresAtUtc" timestamp with time zone NOT NULL,
    CONSTRAINT "CK_ConsumedTelegramAssertions_V2" CHECK ((("Fingerprint")::text ~~ 'v2:%'::text))
);

CREATE TABLE public."ContentReports" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "TargetType" character varying(20) NOT NULL,
    "TargetId" uuid NOT NULL,
    "Reason" character varying(1000) NOT NULL,
    "Status" character varying(20) NOT NULL,
    "AdminNote" character varying(2000),
    "ReviewedAtUtc" timestamp with time zone,
    "CreatedOnUtc" timestamp with time zone NOT NULL,
    "ModifiedOnUtc" timestamp with time zone,
    "ReviewedByUserId" uuid
);

CREATE TABLE public."CycleBleedingEntries" (
    "Id" uuid NOT NULL,
    "CycleProfileId" uuid NOT NULL,
    "Date" date NOT NULL,
    "Type" character varying(32) NOT NULL,
    "Flow" character varying(32) NOT NULL,
    "PainImpact" integer,
    "Notes" character varying(1024),
    "CreatedOnUtc" timestamp with time zone NOT NULL,
    "ModifiedOnUtc" timestamp with time zone
);

CREATE TABLE public."CycleConsents" (
    "Id" uuid NOT NULL,
    "CycleProfileId" uuid NOT NULL,
    "Purpose" character varying(64) NOT NULL,
    "GrantedAtUtc" timestamp with time zone NOT NULL,
    "RevokedAtUtc" timestamp with time zone,
    "CreatedOnUtc" timestamp with time zone NOT NULL,
    "ModifiedOnUtc" timestamp with time zone
);

CREATE TABLE public."CycleDayNotes" (
    "Id" uuid NOT NULL,
    "CycleProfileId" uuid NOT NULL,
    "Date" date NOT NULL,
    "Notes" character varying(1024) NOT NULL,
    "CreatedOnUtc" timestamp with time zone NOT NULL,
    "ModifiedOnUtc" timestamp with time zone
);

CREATE TABLE public."CycleFactors" (
    "Id" uuid NOT NULL,
    "CycleProfileId" uuid NOT NULL,
    "Type" character varying(64) NOT NULL,
    "StartDate" date NOT NULL,
    "EndDate" date,
    "Notes" character varying(1024),
    "CreatedOnUtc" timestamp with time zone NOT NULL,
    "ModifiedOnUtc" timestamp with time zone
);

CREATE TABLE public."CycleMenstrualEpisodes" (
    "Id" uuid NOT NULL,
    "CycleProfileId" uuid NOT NULL,
    "StartDate" date NOT NULL,
    "EndDate" date,
    "Status" character varying(32) NOT NULL,
    "ExcludedFromPredictions" boolean NOT NULL,
    "CreatedOnUtc" timestamp with time zone NOT NULL,
    "ModifiedOnUtc" timestamp with time zone
);

CREATE TABLE public."CyclePredictionRevisions" (
    "Id" uuid NOT NULL,
    "CycleProfileId" uuid NOT NULL,
    "GeneratedAtUtc" timestamp with time zone NOT NULL,
    "NextPeriodStartFrom" date,
    "NextPeriodStartTo" date,
    "Confidence" character varying(32) NOT NULL,
    "DataSufficiency" character varying(32) NOT NULL,
    "PatternConsistency" character varying(32) NOT NULL,
    "CompletedCycleCount" integer NOT NULL,
    "CalibrationSampleCount" integer NOT NULL,
    "HistoricalCoveragePercent" double precision,
    "MeanAbsoluteErrorDays" double precision,
    "ReasonCodes" character varying(512) NOT NULL,
    "AlgorithmVersion" character varying(64) NOT NULL,
    "CreatedOnUtc" timestamp with time zone NOT NULL,
    "ModifiedOnUtc" timestamp with time zone
);

CREATE TABLE public."CycleProfiles" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "Mode" character varying(64) NOT NULL,
    "Confidence" character varying(32) NOT NULL,
    "TrackingStartDate" date NOT NULL,
    "AverageCycleLength" integer DEFAULT 28 NOT NULL,
    "AveragePeriodLength" integer DEFAULT 5 NOT NULL,
    "LutealLength" integer DEFAULT 14 NOT NULL,
    "IsRegular" boolean NOT NULL,
    "IsOnboardingComplete" boolean NOT NULL,
    "ShowFertilityEstimates" boolean NOT NULL,
    "DiscreetNotifications" boolean NOT NULL,
    "Notes" character varying(1024),
    "CreatedOnUtc" timestamp with time zone NOT NULL,
    "ModifiedOnUtc" timestamp with time zone,
    "Goal" character varying(64) NOT NULL,
    "HideFromDashboard" boolean DEFAULT false NOT NULL,
    "ReproductiveState" character varying(64) NOT NULL
);

CREATE TABLE public."CycleSymptomEntries" (
    "Id" uuid NOT NULL,
    "CycleProfileId" uuid NOT NULL,
    "Date" date NOT NULL,
    "Category" character varying(64) NOT NULL,
    "Intensity" integer NOT NULL,
    "TagsJson" jsonb NOT NULL,
    "Note" character varying(1024),
    "CreatedOnUtc" timestamp with time zone NOT NULL,
    "ModifiedOnUtc" timestamp with time zone
);

CREATE TABLE public."DailyAdvices" (
    "Id" uuid NOT NULL,
    "Value" character varying(512) NOT NULL,
    "Locale" character varying(10) NOT NULL,
    "Weight" integer DEFAULT 1 NOT NULL,
    "Tag" character varying(64),
    "CreatedOnUtc" timestamp with time zone NOT NULL,
    "ModifiedOnUtc" timestamp with time zone,
    "GroupId" uuid DEFAULT gen_random_uuid() NOT NULL
);

CREATE TABLE public."DailyReferenceValues" (
    "Id" integer NOT NULL,
    "NutrientId" integer NOT NULL,
    "Value" double precision NOT NULL,
    "Unit" character varying(32) NOT NULL,
    "AgeGroup" character varying(64) NOT NULL,
    "Gender" character varying(16) NOT NULL
);

ALTER TABLE public."DailyReferenceValues" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."DailyReferenceValues_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);

CREATE TABLE public."DietologistInvitations" (
    "Id" uuid NOT NULL,
    "ClientUserId" uuid NOT NULL,
    "DietologistUserId" uuid,
    "DietologistEmail" character varying(256) NOT NULL,
    "TokenHash" character varying(256) NOT NULL,
    "ExpiresAtUtc" timestamp with time zone NOT NULL,
    "Status" character varying(32) NOT NULL,
    "ShareMeals" boolean NOT NULL,
    "ShareStatistics" boolean NOT NULL,
    "ShareWeight" boolean NOT NULL,
    "ShareWaist" boolean NOT NULL,
    "ShareGoals" boolean NOT NULL,
    "ShareHydration" boolean NOT NULL,
    "AcceptedAtUtc" timestamp with time zone,
    "RevokedAtUtc" timestamp with time zone,
    "CreatedOnUtc" timestamp with time zone NOT NULL,
    "ModifiedOnUtc" timestamp with time zone,
    "ShareFasting" boolean DEFAULT true NOT NULL,
    "ShareProfile" boolean DEFAULT true NOT NULL
);

CREATE TABLE public."EmailOutbox" (
    "Id" uuid NOT NULL,
    "FromAddress" character varying(320) NOT NULL,
    "FromName" character varying(200) NOT NULL,
    "ToAddressesJson" text NOT NULL,
    "Subject" character varying(500) NOT NULL,
    "HtmlBody" text NOT NULL,
    "TextBody" text,
    "CreatedOnUtc" timestamp with time zone NOT NULL,
    "NextAttemptOnUtc" timestamp with time zone NOT NULL,
    "AttemptCount" integer NOT NULL,
    "ProcessedOnUtc" timestamp with time zone,
    "LastError" character varying(2048),
    "LockedBy" character varying(128),
    "LockedUntilUtc" timestamp with time zone,
    "DeadLetteredOnUtc" timestamp with time zone,
    "AutoSubmitted" boolean DEFAULT false NOT NULL,
    "CorrelationId" character varying(128),
    "InReplyTo" character varying(998),
    "Purpose" character varying(64) DEFAULT 'other'::character varying NOT NULL,
    "ReplyTo" character varying(320)
);

CREATE TABLE public."EmailTemplateRevisions" (
    "Id" uuid NOT NULL,
    "Subject" character varying(256) NOT NULL,
    "HtmlBody" text NOT NULL,
    "TextBody" text NOT NULL,
    "IsActive" boolean NOT NULL,
    "SavedOnUtc" timestamp with time zone NOT NULL,
    "ArchivedOnUtc" timestamp with time zone NOT NULL,
    "TemplateId" uuid NOT NULL
);

CREATE TABLE public."EmailTemplates" (
    "Id" uuid NOT NULL,
    "Key" character varying(64) NOT NULL,
    "Locale" character varying(8) NOT NULL,
    "Subject" character varying(256) NOT NULL,
    "HtmlBody" text NOT NULL,
    "TextBody" text NOT NULL,
    "IsActive" boolean DEFAULT true NOT NULL,
    "CreatedOnUtc" timestamp with time zone NOT NULL,
    "ModifiedOnUtc" timestamp with time zone
);

CREATE TABLE public."ExerciseEntries" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "Date" date NOT NULL,
    "ExerciseType" character varying(32) NOT NULL,
    "Name" character varying(256),
    "DurationMinutes" integer NOT NULL,
    "CaloriesBurned" double precision NOT NULL,
    "Notes" character varying(500),
    "CreatedOnUtc" timestamp with time zone NOT NULL,
    "ModifiedOnUtc" timestamp with time zone
);

CREATE TABLE public."FastingCheckIns" (
    "Id" uuid NOT NULL,
    "OccurrenceId" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "CheckedInAtUtc" timestamp with time zone NOT NULL,
    "HungerLevel" integer NOT NULL,
    "EnergyLevel" integer NOT NULL,
    "MoodLevel" integer NOT NULL,
    "Symptoms" character varying(200),
    "Notes" character varying(500),
    "CreatedOnUtc" timestamp with time zone NOT NULL,
    "ModifiedOnUtc" timestamp with time zone
);

CREATE TABLE public."FastingOccurrences" (
    "Id" uuid NOT NULL,
    "PlanId" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "Kind" character varying(24) NOT NULL,
    "Status" character varying(16) NOT NULL,
    "SequenceNumber" integer NOT NULL,
    "ScheduledForUtc" timestamp with time zone,
    "StartedAtUtc" timestamp with time zone NOT NULL,
    "EndedAtUtc" timestamp with time zone,
    "InitialTargetHours" integer,
    "AddedTargetHours" integer NOT NULL,
    "Notes" character varying(500),
    "CreatedOnUtc" timestamp with time zone NOT NULL,
    "ModifiedOnUtc" timestamp with time zone,
    "CheckInAtUtc" timestamp with time zone,
    "CheckInNotes" character varying(500),
    "EnergyLevel" integer,
    "HungerLevel" integer,
    "MoodLevel" integer,
    "Symptoms" character varying(200)
);

CREATE TABLE public."FastingPlans" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "Type" character varying(24) NOT NULL,
    "Status" character varying(16) NOT NULL,
    "Protocol" character varying(32),
    "Title" character varying(120),
    "StartedAtUtc" timestamp with time zone NOT NULL,
    "StoppedAtUtc" timestamp with time zone,
    "IntermittentFastHours" integer,
    "IntermittentEatingWindowHours" integer,
    "ExtendedTargetHours" integer,
    "CyclicFastDays" integer,
    "CyclicEatDays" integer,
    "CyclicEatDayFastHours" integer,
    "CyclicEatDayEatingWindowHours" integer,
    "CyclicAnchorDateUtc" timestamp with time zone,
    "CyclicNextPhaseDateUtc" timestamp with time zone,
    "CreatedOnUtc" timestamp with time zone NOT NULL,
    "ModifiedOnUtc" timestamp with time zone
);

CREATE TABLE public."FastingSessions" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "StartedAtUtc" timestamp with time zone NOT NULL,
    "EndedAtUtc" timestamp with time zone,
    "Protocol" character varying(16) NOT NULL,
    "IsCompleted" boolean NOT NULL,
    "Notes" character varying(500),
    "CreatedOnUtc" timestamp with time zone NOT NULL,
    "ModifiedOnUtc" timestamp with time zone,
    "AddedDurationHours" integer DEFAULT 0 NOT NULL,
    "InitialPlannedDurationHours" integer DEFAULT 0 NOT NULL
);

CREATE TABLE public."FastingTelemetryEvents" (
    "Id" uuid NOT NULL,
    "OccurredAtUtc" timestamp with time zone NOT NULL,
    "Name" character varying(64) NOT NULL,
    "SessionId" character varying(64),
    "Protocol" character varying(32),
    "PlanType" character varying(16),
    "Status" character varying(16),
    "OccurrenceKind" character varying(16),
    "ReminderPresetId" character varying(32),
    "ReminderSource" character varying(16),
    "FirstReminderHours" integer,
    "FollowUpReminderHours" integer,
    "PlannedDurationHours" integer,
    "ActualDurationHours" double precision,
    "HungerLevel" integer,
    "EnergyLevel" integer,
    "MoodLevel" integer,
    "SymptomsCount" integer,
    "HadNotes" boolean,
    "CreatedOnUtc" timestamp with time zone NOT NULL,
    "ModifiedOnUtc" timestamp with time zone
);

CREATE TABLE public."FavoriteMeals" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "MealId" uuid NOT NULL,
    "Name" character varying(500),
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "CreatedOnUtc" timestamp with time zone NOT NULL,
    "ModifiedOnUtc" timestamp with time zone,
    "RemovedAtUtc" timestamp with time zone
);

CREATE TABLE public."FavoriteProducts" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "ProductId" uuid NOT NULL,
    "Name" character varying(500),
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "CreatedOnUtc" timestamp with time zone NOT NULL,
    "ModifiedOnUtc" timestamp with time zone,
    "PreferredPortionAmount" double precision
);

CREATE TABLE public."FavoriteRecipes" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "RecipeId" uuid NOT NULL,
    "Name" character varying(500),
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "CreatedOnUtc" timestamp with time zone NOT NULL,
    "ModifiedOnUtc" timestamp with time zone
);

CREATE TABLE public."FertilitySignals" (
    "Id" uuid NOT NULL,
    "CycleProfileId" uuid NOT NULL,
    "Date" date NOT NULL,
    "BasalBodyTemperatureCelsius" double precision,
    "OvulationTestResult" character varying(32),
    "CervicalFluid" character varying(128),
    "HadSex" boolean,
    "Notes" character varying(1024),
    "CreatedOnUtc" timestamp with time zone NOT NULL,
    "ModifiedOnUtc" timestamp with time zone
);

CREATE TABLE public."FoodRecognitionJobImages" (
    "JobId" uuid NOT NULL,
    "ImageAssetId" uuid NOT NULL,
    "ImageUrl" character varying(2048) NOT NULL,
    "Position" integer NOT NULL
);

CREATE TABLE public."FoodRecognitionJobs" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "ImageAssetId" uuid NOT NULL,
    "ImageUrl" character varying(2048) NOT NULL,
    "Description" character varying(2048),
    "Status" character varying(16) NOT NULL,
    "CreatedOnUtc" timestamp with time zone NOT NULL,
    "UpdatedOnUtc" timestamp with time zone NOT NULL,
    "VisionJson" text,
    "NutritionJson" text,
    "ErrorCode" character varying(128),
    "NutritionErrorCode" character varying(128),
    "IsProductLabel" boolean DEFAULT false NOT NULL
);

CREATE TABLE public."HydrationEntries" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "Timestamp" timestamp with time zone NOT NULL,
    "AmountMl" integer NOT NULL,
    "CreatedOnUtc" timestamp with time zone NOT NULL,
    "ModifiedOnUtc" timestamp with time zone,
    CONSTRAINT "CK_HydrationEntries_AmountMl" CHECK ((("AmountMl" > 0) AND ("AmountMl" <= 10000)))
);

CREATE TABLE public."HydrationOperationReceipts" (
    "OperationId" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "EntryId" uuid NOT NULL,
    "TimestampUtc" timestamp with time zone NOT NULL,
    "AmountMl" integer NOT NULL
);

CREATE TABLE public."ImageAssets" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "ObjectKey" text NOT NULL,
    "Url" text NOT NULL,
    "CreatedOnUtc" timestamp with time zone NOT NULL,
    "ModifiedOnUtc" timestamp with time zone,
    "IsConfirmed" boolean NOT NULL
);

CREATE TABLE public."ImageObjectDeletionOutbox" (
    "Id" uuid NOT NULL,
    "ObjectKey" character varying(1024) NOT NULL,
    "CreatedOnUtc" timestamp with time zone NOT NULL,
    "NextAttemptOnUtc" timestamp with time zone NOT NULL,
    "AttemptCount" integer NOT NULL,
    "ProcessedOnUtc" timestamp with time zone,
    "LastError" character varying(2048),
    "LockedBy" character varying(128),
    "LockedUntilUtc" timestamp with time zone,
    "DeadLetteredOnUtc" timestamp with time zone,
    "IsConfirmed" boolean NOT NULL
);

CREATE TABLE public."MarketingAttributionEvents" (
    "Id" uuid NOT NULL,
    "OccurredAtUtc" timestamp with time zone NOT NULL,
    "UserId" uuid,
    "EventType" character varying(32) NOT NULL,
    "AnonymousId" character varying(96) NOT NULL,
    "SessionId" character varying(96) NOT NULL,
    "LandingPath" character varying(512) NOT NULL,
    "ReferrerHost" character varying(128),
    "UtmSource" character varying(160),
    "UtmMedium" character varying(160),
    "UtmCampaign" character varying(160),
    "UtmContent" character varying(160),
    "UtmTerm" character varying(160),
    "BuildVersion" character varying(64),
    "CreatedOnUtc" timestamp with time zone NOT NULL,
    "ModifiedOnUtc" timestamp with time zone
);

CREATE TABLE public."MealAiItems" (
    "Id" uuid NOT NULL,
    "MealAiSessionId" uuid NOT NULL,
    "NameEn" character varying(256) NOT NULL,
    "Amount" double precision NOT NULL,
    "Unit" character varying(32) NOT NULL,
    "Calories" double precision NOT NULL,
    "Proteins" double precision NOT NULL,
    "Fats" double precision NOT NULL,
    "Carbs" double precision NOT NULL,
    "Fiber" double precision NOT NULL,
    "Alcohol" double precision NOT NULL,
    "CreatedOnUtc" timestamp with time zone NOT NULL,
    "ModifiedOnUtc" timestamp with time zone,
    "NameLocal" character varying(256),
    "Confidence" double precision DEFAULT 1.0 NOT NULL,
    "Resolution" character varying(16) DEFAULT 'Accepted'::character varying NOT NULL
);

CREATE TABLE public."MealAiSessions" (
    "Id" uuid NOT NULL,
    "MealId" uuid NOT NULL,
    "ImageAssetId" uuid,
    "RecognizedAtUtc" timestamp with time zone NOT NULL,
    "Notes" character varying(2048),
    "CreatedOnUtc" timestamp with time zone NOT NULL,
    "ModifiedOnUtc" timestamp with time zone,
    "Source" character varying(16) DEFAULT 'Text'::character varying NOT NULL,
    "Status" character varying(16) DEFAULT 'Reviewed'::character varying NOT NULL
);

CREATE TABLE public."MealItems" (
    "Id" uuid NOT NULL,
    "MealId" uuid NOT NULL,
    "ProductId" uuid,
    "Amount" double precision NOT NULL,
    "CreatedOnUtc" timestamp with time zone NOT NULL,
    "ModifiedOnUtc" timestamp with time zone,
    "RecipeId" uuid,
    "Origin" character varying(16) DEFAULT 'Manual'::character varying NOT NULL,
    "SnapshotAlcoholPerBase" double precision,
    "SnapshotBaseAmount" double precision,
    "SnapshotCaloriesPerBase" double precision,
    "SnapshotCarbsPerBase" double precision,
    "SnapshotFatsPerBase" double precision,
    "SnapshotFiberPerBase" double precision,
    "SnapshotImageUrl" character varying(2048),
    "SnapshotName" character varying(256),
    "SnapshotProteinsPerBase" double precision,
    "SnapshotUnit" character varying(32),
    "SourceAiItemId" uuid
);

CREATE TABLE public."MealPlanDays" (
    "Id" uuid NOT NULL,
    "MealPlanId" uuid NOT NULL,
    "DayNumber" integer NOT NULL,
    "CreatedOnUtc" timestamp with time zone NOT NULL,
    "ModifiedOnUtc" timestamp with time zone
);

CREATE TABLE public."MealPlanMeals" (
    "Id" uuid NOT NULL,
    "MealPlanDayId" uuid NOT NULL,
    "MealType" text NOT NULL,
    "RecipeId" uuid NOT NULL,
    "Servings" integer NOT NULL,
    "CreatedOnUtc" timestamp with time zone NOT NULL,
    "ModifiedOnUtc" timestamp with time zone
);

CREATE TABLE public."MealPlans" (
    "Id" uuid NOT NULL,
    "Name" character varying(256) NOT NULL,
    "Description" character varying(2048),
    "DietType" text NOT NULL,
    "DurationDays" integer NOT NULL,
    "TargetCaloriesPerDay" double precision,
    "IsCurated" boolean NOT NULL,
    "UserId" uuid,
    "CreatedOnUtc" timestamp with time zone NOT NULL,
    "ModifiedOnUtc" timestamp with time zone
);

CREATE TABLE public."MealRecognitionReceipts" (
    "OperationId" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "RecognitionId" uuid NOT NULL,
    "MealId" uuid NOT NULL,
    "MealVersion" bigint NOT NULL,
    "MealOccurredAtUtc" timestamp with time zone NOT NULL,
    "SavedAtUtc" timestamp with time zone NOT NULL,
    "UndoUntilUtc" timestamp with time zone NOT NULL,
    "UndoneAtUtc" timestamp with time zone
);

CREATE TABLE public."Meals" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "Date" timestamp with time zone NOT NULL,
    "MealType" integer,
    "Comment" text,
    "CreatedOnUtc" timestamp with time zone NOT NULL,
    "ModifiedOnUtc" timestamp with time zone,
    "TotalCalories" double precision DEFAULT 0.0 NOT NULL,
    "TotalCarbs" double precision DEFAULT 0.0 NOT NULL,
    "TotalFats" double precision DEFAULT 0.0 NOT NULL,
    "TotalFiber" double precision DEFAULT 0.0 NOT NULL,
    "TotalProteins" double precision DEFAULT 0.0 NOT NULL,
    "ImageUrl" text,
    "IsNutritionAutoCalculated" boolean DEFAULT true NOT NULL,
    "ManualCalories" double precision,
    "ManualCarbs" double precision,
    "ManualFats" double precision,
    "ManualFiber" double precision,
    "ManualProteins" double precision,
    "PostMealSatietyLevel" integer DEFAULT 0 NOT NULL,
    "PreMealSatietyLevel" integer DEFAULT 0 NOT NULL,
    "ImageAssetId" uuid,
    "TotalAlcohol" double precision DEFAULT 0.0 NOT NULL,
    "ManualAlcohol" double precision
);

CREATE TABLE public."NotificationWebPushOutbox" (
    "Id" uuid NOT NULL,
    "NotificationId" uuid NOT NULL,
    "CreatedOnUtc" timestamp with time zone NOT NULL,
    "NextAttemptOnUtc" timestamp with time zone NOT NULL,
    "AttemptCount" integer NOT NULL,
    "ProcessedOnUtc" timestamp with time zone,
    "LastError" character varying(2048),
    "LockedBy" character varying(128),
    "LockedUntilUtc" timestamp with time zone,
    "DeadLetteredOnUtc" timestamp with time zone,
    "CompletedSubscriptionIdsJson" jsonb DEFAULT '[]'::jsonb NOT NULL
);

CREATE TABLE public."Notifications" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "Type" character varying(64) NOT NULL,
    "ReferenceId" character varying(128),
    "IsRead" boolean NOT NULL,
    "ReadAtUtc" timestamp with time zone,
    "CreatedOnUtc" timestamp with time zone NOT NULL,
    "ModifiedOnUtc" timestamp with time zone,
    "PayloadJson" character varying(4000) DEFAULT '{}'::character varying NOT NULL
);

CREATE TABLE public."NutritionLessons" (
    "Id" uuid NOT NULL,
    "Title" character varying(256) NOT NULL,
    "Content" character varying(65536) NOT NULL,
    "Summary" character varying(512),
    "Locale" character varying(10) NOT NULL,
    "Category" character varying(32) NOT NULL,
    "Difficulty" character varying(16) NOT NULL,
    "EstimatedReadMinutes" integer NOT NULL,
    "SortOrder" integer NOT NULL,
    "CreatedOnUtc" timestamp with time zone NOT NULL,
    "ModifiedOnUtc" timestamp with time zone,
    "IsPublished" boolean DEFAULT true NOT NULL
);

CREATE TABLE public."OpenFoodFactsProducts" (
    "Barcode" character varying(64) NOT NULL,
    "Name" character varying(512) NOT NULL,
    "Brand" character varying(512),
    "Category" character varying(1024),
    "ImageUrl" character varying(2048),
    "CaloriesPer100G" double precision,
    "ProteinsPer100G" double precision,
    "FatsPer100G" double precision,
    "CarbsPer100G" double precision,
    "FiberPer100G" double precision,
    "LastSyncedAtUtc" timestamp with time zone NOT NULL,
    "LastSeenAtUtc" timestamp with time zone NOT NULL,
    "SearchHitCount" integer NOT NULL
);

CREATE TABLE public."OutboxReplayAudits" (
    "Id" uuid NOT NULL,
    "OutboxName" character varying(64) NOT NULL,
    "MessageId" uuid NOT NULL,
    "RequestedBy" character varying(256) NOT NULL,
    "Reason" character varying(1024) NOT NULL,
    "RequestedOnUtc" timestamp with time zone NOT NULL,
    "PreviousAttemptCount" integer NOT NULL,
    "PreviousError" character varying(2048)
);

CREATE TABLE public."ProductImages" (
    "ImageAssetId" uuid NOT NULL,
    "ProductId" uuid NOT NULL,
    "ImageUrl" character varying(2048) NOT NULL,
    "Position" integer NOT NULL
);

CREATE TABLE public."Products" (
    "Id" uuid NOT NULL,
    "Barcode" text,
    "Name" text NOT NULL,
    "Brand" text,
    "Category" text,
    "Description" text,
    "ImageUrl" text,
    "BaseUnit" integer NOT NULL,
    "BaseAmount" double precision NOT NULL,
    "CaloriesPerBase" double precision NOT NULL,
    "ProteinsPerBase" double precision NOT NULL,
    "FatsPerBase" double precision NOT NULL,
    "CarbsPerBase" double precision NOT NULL,
    "FiberPerBase" double precision NOT NULL,
    "Visibility" integer DEFAULT 0 NOT NULL,
    "UserId" uuid NOT NULL,
    "CreatedOnUtc" timestamp with time zone NOT NULL,
    "ModifiedOnUtc" timestamp with time zone,
    "Comment" text,
    "ProductType" integer DEFAULT 0 NOT NULL,
    "DefaultPortionAmount" double precision DEFAULT 0.0 NOT NULL,
    "ImageAssetId" uuid,
    "AlcoholPerBase" double precision DEFAULT 0.0 NOT NULL,
    "UsdaFdcId" integer
);

CREATE TABLE public."RecentItems" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "ItemType" character varying(32) NOT NULL,
    "ItemId" uuid NOT NULL,
    "LastUsedAtUtc" timestamp with time zone NOT NULL,
    "UsageCount" integer NOT NULL,
    "CreatedOnUtc" timestamp with time zone NOT NULL,
    "ModifiedOnUtc" timestamp with time zone
);

CREATE TABLE public."RecipeComments" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "RecipeId" uuid NOT NULL,
    "Text" character varying(2000) NOT NULL,
    "CreatedOnUtc" timestamp with time zone NOT NULL,
    "ModifiedOnUtc" timestamp with time zone
);

CREATE TABLE public."RecipeImages" (
    "ImageAssetId" uuid NOT NULL,
    "RecipeId" uuid NOT NULL,
    "ImageUrl" character varying(2048) NOT NULL,
    "Position" integer NOT NULL
);

CREATE TABLE public."RecipeIngredients" (
    "Id" uuid NOT NULL,
    "RecipeStepId" uuid NOT NULL,
    "ProductId" uuid,
    "NestedRecipeId" uuid,
    "Amount" double precision NOT NULL,
    "CreatedOnUtc" timestamp with time zone NOT NULL,
    "ModifiedOnUtc" timestamp with time zone,
    "AmountText" character varying(128),
    "TextName" character varying(256),
    "Position" integer DEFAULT 0 NOT NULL,
    "PublicName" character varying(256),
    "PublicUnit" character varying(128)
);

CREATE TABLE public."RecipeLikes" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "RecipeId" uuid NOT NULL,
    "CreatedOnUtc" timestamp with time zone NOT NULL,
    "ModifiedOnUtc" timestamp with time zone
);

CREATE TABLE public."RecipeStepImages" (
    "ImageAssetId" uuid NOT NULL,
    "RecipeStepId" uuid NOT NULL,
    "ImageUrl" character varying(2048) NOT NULL,
    "Position" integer NOT NULL
);

CREATE TABLE public."RecipeSteps" (
    "Id" uuid NOT NULL,
    "RecipeId" uuid NOT NULL,
    "StepNumber" integer NOT NULL,
    "Instruction" text NOT NULL,
    "ImageUrl" text,
    "CreatedOnUtc" timestamp with time zone NOT NULL,
    "ModifiedOnUtc" timestamp with time zone,
    "Title" text,
    "ImageAssetId" uuid
);

CREATE TABLE public."Recipes" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "Name" text NOT NULL,
    "Description" text,
    "Category" character varying(128) DEFAULT 'other'::character varying NOT NULL,
    "ImageUrl" text,
    "PrepTime" integer,
    "CookTime" integer,
    "Servings" integer NOT NULL,
    "TotalCalories" double precision,
    "TotalProteins" double precision,
    "TotalFats" double precision,
    "TotalCarbs" double precision,
    "CreatedOnUtc" timestamp with time zone NOT NULL,
    "ModifiedOnUtc" timestamp with time zone,
    "Visibility" integer DEFAULT 0 NOT NULL,
    "IsNutritionAutoCalculated" boolean DEFAULT true NOT NULL,
    "ManualCalories" double precision,
    "ManualCarbs" double precision,
    "ManualFats" double precision,
    "ManualFiber" double precision,
    "ManualProteins" double precision,
    "TotalFiber" double precision,
    "ImageAssetId" uuid,
    "TotalAlcohol" double precision,
    "ManualAlcohol" double precision,
    "Comment" text,
    "MissingIngredientCount" integer DEFAULT 0 NOT NULL,
    "Language" character varying(2) DEFAULT 'en'::character varying NOT NULL,
    "LanguageConfirmed" boolean DEFAULT false NOT NULL,
    CONSTRAINT "CK_Recipes_Category" CHECK ((("Category")::text = ANY ((ARRAY['other'::character varying, 'breakfast'::character varying, 'soups'::character varying, 'salads'::character varying, 'main_courses'::character varying, 'side_dishes'::character varying, 'appetizers'::character varying, 'sandwiches'::character varying, 'pasta'::character varying, 'baking'::character varying, 'desserts'::character varying, 'drinks'::character varying, 'sauces'::character varying, 'snacks'::character varying, 'preserves'::character varying])::text[])))
);

CREATE TABLE public."RecommendationBulkDispatches" (
    "Id" uuid NOT NULL,
    "DietologistUserId" uuid NOT NULL,
    "ClientUserId" uuid NOT NULL,
    "RecommendationId" uuid NOT NULL,
    "IdempotencyKey" character varying(100) NOT NULL,
    "CreatedOnUtc" timestamp with time zone NOT NULL,
    "ModifiedOnUtc" timestamp with time zone
);

CREATE TABLE public."RecommendationComments" (
    "Id" uuid NOT NULL,
    "RecommendationId" uuid NOT NULL,
    "AuthorUserId" uuid NOT NULL,
    "Text" character varying(2000) NOT NULL,
    "CreatedOnUtc" timestamp with time zone NOT NULL,
    "ModifiedOnUtc" timestamp with time zone
);

CREATE TABLE public."RecommendationTemplates" (
    "Id" uuid NOT NULL,
    "DietologistUserId" uuid NOT NULL,
    "Name" character varying(120) NOT NULL,
    "Text" character varying(2000) NOT NULL,
    "IsArchived" boolean NOT NULL,
    "CreatedOnUtc" timestamp with time zone NOT NULL,
    "ModifiedOnUtc" timestamp with time zone
);

CREATE TABLE public."Recommendations" (
    "Id" uuid NOT NULL,
    "DietologistUserId" uuid NOT NULL,
    "ClientUserId" uuid NOT NULL,
    "Text" character varying(2000) NOT NULL,
    "IsRead" boolean NOT NULL,
    "ReadAtUtc" timestamp with time zone,
    "CreatedOnUtc" timestamp with time zone NOT NULL,
    "ModifiedOnUtc" timestamp with time zone
);

CREATE TABLE public."Roles" (
    "Id" uuid NOT NULL,
    "Name" character varying(64) NOT NULL,
    "CreatedOnUtc" timestamp with time zone NOT NULL,
    "ModifiedOnUtc" timestamp with time zone
);

CREATE TABLE public."ShoppingListItemSources" (
    "Id" uuid NOT NULL,
    "ShoppingListItemId" uuid NOT NULL,
    "SourceType" character varying(32) NOT NULL,
    "MealPlanId" uuid,
    "MealPlanMealId" uuid,
    "RecipeId" uuid,
    "Label" character varying(256) NOT NULL,
    "DayNumber" integer,
    "MealType" character varying(256),
    "Amount" double precision NOT NULL,
    "Unit" integer,
    "CreatedOnUtc" timestamp with time zone NOT NULL,
    "ModifiedOnUtc" timestamp with time zone
);

CREATE TABLE public."ShoppingListItems" (
    "Id" uuid NOT NULL,
    "ShoppingListId" uuid NOT NULL,
    "ProductId" uuid,
    "Name" character varying(256) NOT NULL,
    "Amount" double precision,
    "Unit" integer,
    "Category" character varying(128),
    "IsChecked" boolean DEFAULT false NOT NULL,
    "SortOrder" integer DEFAULT 0 NOT NULL,
    "CreatedOnUtc" timestamp with time zone NOT NULL,
    "ModifiedOnUtc" timestamp with time zone,
    "Aisle" character varying(128),
    "CheckedOnUtc" timestamp with time zone,
    "Note" character varying(512)
);

CREATE TABLE public."ShoppingLists" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "Name" character varying(128) NOT NULL,
    "CreatedOnUtc" timestamp with time zone NOT NULL,
    "ModifiedOnUtc" timestamp with time zone
);

CREATE TABLE public."TelegramAssertionPolicies" (
    "Id" integer NOT NULL,
    "NotBeforeUtc" timestamp with time zone NOT NULL,
    CONSTRAINT "CK_TelegramAssertionPolicies_Singleton" CHECK (("Id" = 1))
);

CREATE TABLE public."TelegramLoginTickets" (
    "Fingerprint" character varying(64) NOT NULL,
    "Purpose" character varying(64) NOT NULL,
    "BrowserBindingHash" character varying(64) NOT NULL,
    "ProtectedPayload" character varying(16384) NOT NULL,
    "ExpiresAtUtc" timestamp with time zone NOT NULL
);

CREATE TABLE public."TelegramOperations" (
    "Id" uuid NOT NULL,
    "BotId" bigint NOT NULL,
    "UpdateId" bigint NOT NULL,
    "UserId" uuid NOT NULL,
    "SecurityVersion" bigint NOT NULL,
    "PayloadHash" character varying(64) NOT NULL,
    "ProtectedPayload" character varying(65536) NOT NULL,
    "ProtectedCheckpoint" character varying(65536),
    "LeaseId" uuid,
    "LeaseExpiresAtUtc" timestamp with time zone,
    "NextAttemptAtUtc" timestamp with time zone NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "Completed" boolean NOT NULL
);

CREATE TABLE public."UsdaFoodNutrients" (
    "Id" integer NOT NULL,
    "FdcId" integer NOT NULL,
    "NutrientId" integer NOT NULL,
    "Amount" double precision NOT NULL
);

CREATE TABLE public."UsdaFoodPortions" (
    "Id" integer NOT NULL,
    "FdcId" integer NOT NULL,
    "Amount" double precision NOT NULL,
    "MeasureUnitName" character varying(128) NOT NULL,
    "GramWeight" double precision NOT NULL,
    "PortionDescription" character varying(256),
    "Modifier" character varying(128)
);

CREATE TABLE public."UsdaFoods" (
    "FdcId" integer NOT NULL,
    "Description" character varying(512) NOT NULL,
    "FoodCategoryId" integer,
    "FoodCategory" character varying(256)
);

CREATE TABLE public."UsdaNutrients" (
    "Id" integer NOT NULL,
    "Name" character varying(256) NOT NULL,
    "UnitName" character varying(32) NOT NULL
);

CREATE TABLE public."UserAchievements" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "AchievementKey" character varying(100) NOT NULL,
    "EarnedAtUtc" timestamp with time zone NOT NULL,
    "EarnedValue" integer NOT NULL,
    "DefinitionVersion" integer NOT NULL,
    "CreatedOnUtc" timestamp with time zone NOT NULL,
    "ModifiedOnUtc" timestamp with time zone
);

CREATE TABLE public."UserLessonProgress" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "LessonId" uuid NOT NULL,
    "ReadAtUtc" timestamp with time zone NOT NULL,
    "CreatedOnUtc" timestamp with time zone NOT NULL,
    "ModifiedOnUtc" timestamp with time zone
);

CREATE TABLE public."UserLoginEvents" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "AuthProvider" character varying(64) NOT NULL,
    "IpAddress" character varying(128),
    "UserAgent" character varying(512),
    "BrowserName" character varying(64),
    "BrowserVersion" character varying(64),
    "OperatingSystem" character varying(64),
    "DeviceType" character varying(32),
    "LoggedInAtUtc" timestamp with time zone NOT NULL,
    "CreatedOnUtc" timestamp with time zone NOT NULL,
    "ModifiedOnUtc" timestamp with time zone
);

CREATE TABLE public."UserNutritionProfiles" (
    "Id" uuid NOT NULL,
    "BirthDate" timestamp with time zone,
    "Gender" text,
    "WeightKg" double precision,
    "DesiredWeightKg" double precision,
    "DesiredWaistCm" double precision,
    "HeightCm" double precision,
    "ActivityLevel" text NOT NULL,
    "DailyCalorieTarget" double precision,
    "ProteinTarget" double precision,
    "FatTarget" double precision,
    "CarbTarget" double precision,
    "FiberTarget" double precision,
    "StepGoal" integer,
    "WaterGoal" double precision,
    "HydrationGoal" double precision,
    "CalorieCyclingEnabled" boolean NOT NULL,
    "MondayCalories" double precision,
    "TuesdayCalories" double precision,
    "WednesdayCalories" double precision,
    "ThursdayCalories" double precision,
    "FridayCalories" double precision,
    "SaturdayCalories" double precision,
    "SundayCalories" double precision,
    "CreatedOnUtc" timestamp with time zone NOT NULL,
    "ModifiedOnUtc" timestamp with time zone
);

CREATE TABLE public."UserPreferences" (
    "Id" uuid NOT NULL,
    "DashboardLayout" jsonb,
    "Language" text DEFAULT 'en'::text,
    "Theme" text DEFAULT 'ocean'::text,
    "UiStyle" text DEFAULT 'classic'::text,
    "SurfaceStyle" character varying(16) DEFAULT 'normal'::character varying NOT NULL,
    "PushNotificationsEnabled" boolean DEFAULT false NOT NULL,
    "FastingPushNotificationsEnabled" boolean DEFAULT true NOT NULL,
    "SocialPushNotificationsEnabled" boolean DEFAULT true NOT NULL,
    "FastingCheckInReminderHours" integer DEFAULT 12 NOT NULL,
    "FastingCheckInFollowUpReminderHours" integer DEFAULT 20 NOT NULL,
    "TimeZoneId" character varying(100),
    "CreatedOnUtc" timestamp with time zone NOT NULL,
    "ModifiedOnUtc" timestamp with time zone
);

CREATE TABLE public."UserRefreshTokenSessions" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "RefreshTokenHash" character varying(512) NOT NULL,
    "RememberMe" boolean NOT NULL,
    "AuthProvider" character varying(64),
    "IpAddress" character varying(128),
    "UserAgent" character varying(512),
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "LastRotatedAtUtc" timestamp with time zone NOT NULL,
    "RevokedAtUtc" timestamp with time zone,
    "CreatedOnUtc" timestamp with time zone NOT NULL,
    "ModifiedOnUtc" timestamp with time zone,
    "PreviousRefreshTokenHash" character varying(512),
    "PreviousRefreshTokenValidUntilUtc" timestamp with time zone
);

CREATE TABLE public."UserRoleAuditEvents" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "RoleId" uuid NOT NULL,
    "RoleName" character varying(64) NOT NULL,
    "Action" character varying(32) NOT NULL,
    "ActorUserId" uuid,
    "Source" character varying(64) NOT NULL,
    "OccurredAtUtc" timestamp with time zone NOT NULL,
    "CreatedOnUtc" timestamp with time zone NOT NULL,
    "ModifiedOnUtc" timestamp with time zone
);

CREATE TABLE public."UserRoles" (
    "UserId" uuid NOT NULL,
    "RoleId" uuid NOT NULL
);

CREATE TABLE public."Users" (
    "Id" uuid NOT NULL,
    "Email" text,
    "Password" text NOT NULL,
    "Username" text,
    "FirstName" text,
    "LastName" text,
    "ProfileImage" text,
    "IsActive" boolean DEFAULT true NOT NULL,
    "CreatedOnUtc" timestamp with time zone NOT NULL,
    "ModifiedOnUtc" timestamp with time zone,
    "ProfileImageAssetId" uuid,
    "DeletedAt" timestamp with time zone,
    "TelegramUserId" bigint,
    "AiInputTokenLimit" bigint DEFAULT 5000000 NOT NULL,
    "AiOutputTokenLimit" bigint DEFAULT 1000000 NOT NULL,
    "EmailConfirmationTokenHash" text,
    "EmailConfirmationTokenExpiresAtUtc" timestamp with time zone,
    "IsEmailConfirmed" boolean DEFAULT false NOT NULL,
    "PasswordResetTokenHash" text,
    "PasswordResetTokenExpiresAtUtc" timestamp with time zone,
    "EmailConfirmationSentAtUtc" timestamp with time zone,
    "PasswordResetSentAtUtc" timestamp with time zone,
    "LastLoginAtUtc" timestamp with time zone,
    "AiConsentAcceptedAt" timestamp with time zone,
    "HasPassword" boolean DEFAULT true NOT NULL,
    "PremiumTrialEndsAtUtc" timestamp with time zone,
    "PremiumTrialStartedAtUtc" timestamp with time zone,
    "GoogleIssuer" character varying(200),
    "GoogleSubject" character varying(255),
    "MustChangePassword" boolean DEFAULT false NOT NULL,
    "SecurityVersion" bigint DEFAULT 0 NOT NULL,
    "TelegramOidcIssuer" character varying(200),
    "TelegramOidcSubject" character varying(255),
    "TelegramLinkVerifiedForRecovery" boolean DEFAULT false NOT NULL
);

CREATE TABLE public."WaistEntries" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "Date" date NOT NULL,
    "CircumferenceCm" double precision NOT NULL,
    "CreatedOnUtc" timestamp with time zone NOT NULL,
    "ModifiedOnUtc" timestamp with time zone
);

CREATE TABLE public."WaistGoals" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "TargetWaistCm" double precision NOT NULL,
    "StartWaistCm" double precision NOT NULL,
    "StartedAtUtc" timestamp with time zone NOT NULL,
    "Status" character varying(32) NOT NULL,
    "EndedAtUtc" timestamp with time zone,
    "EndWaistCm" double precision,
    "CreatedOnUtc" timestamp with time zone NOT NULL,
    "ModifiedOnUtc" timestamp with time zone
);

CREATE TABLE public."WearableConnections" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "Provider" character varying(32) NOT NULL,
    "ExternalUserId" character varying(256) NOT NULL,
    "AccessToken" character varying(8192) NOT NULL,
    "RefreshToken" character varying(8192),
    "TokenExpiresAtUtc" timestamp with time zone,
    "LastSyncedAtUtc" timestamp with time zone,
    "IsActive" boolean NOT NULL,
    "CreatedOnUtc" timestamp with time zone NOT NULL,
    "ModifiedOnUtc" timestamp with time zone,
    "LastConnectRequestHash" character varying(64),
    "LastConnectRequestId" character varying(64)
);

CREATE TABLE public."WearableSyncEntries" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "Provider" character varying(32) NOT NULL,
    "DataType" character varying(32) NOT NULL,
    "Date" date NOT NULL,
    "Value" double precision NOT NULL,
    "CreatedOnUtc" timestamp with time zone NOT NULL,
    "ModifiedOnUtc" timestamp with time zone
);

CREATE TABLE public."WebPushSubscriptions" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "Endpoint" character varying(2048) NOT NULL,
    "P256Dh" character varying(512) NOT NULL,
    "Auth" character varying(512) NOT NULL,
    "ExpirationTimeUtc" timestamp with time zone,
    "Locale" character varying(16),
    "UserAgent" character varying(512),
    "CreatedOnUtc" timestamp with time zone NOT NULL,
    "ModifiedOnUtc" timestamp with time zone
);

CREATE TABLE public."WeeklyGoals" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "WeekStartUtc" timestamp with time zone NOT NULL,
    "Type" character varying(32) NOT NULL,
    "TargetDays" integer NOT NULL,
    "ReminderEnabled" boolean NOT NULL,
    "ReminderTimeMinutes" integer,
    "TimeZoneOffsetMinutes" integer,
    "LastReminderLocalDate" date,
    "CreatedOnUtc" timestamp with time zone NOT NULL,
    "ModifiedOnUtc" timestamp with time zone
);

CREATE TABLE public."WeightEntries" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "Date" date NOT NULL,
    "WeightKg" double precision NOT NULL,
    "CreatedOnUtc" timestamp with time zone NOT NULL,
    "ModifiedOnUtc" timestamp with time zone
);

CREATE TABLE public."WeightGoals" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "TargetWeightKg" double precision NOT NULL,
    "StartWeightKg" double precision NOT NULL,
    "StartedAtUtc" timestamp with time zone NOT NULL,
    "Status" character varying(32) NOT NULL,
    "EndedAtUtc" timestamp with time zone,
    "EndWeightKg" double precision,
    "CreatedOnUtc" timestamp with time zone NOT NULL,
    "ModifiedOnUtc" timestamp with time zone
);

CREATE TABLE public."__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL
);

ALTER TABLE ONLY public."AchievementDefinitions"
    ADD CONSTRAINT "PK_AchievementDefinitions" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."AchievementEvaluationOutbox"
    ADD CONSTRAINT "PK_AchievementEvaluationOutbox" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."AdminImpersonationSessions"
    ADD CONSTRAINT "PK_AdminImpersonationSessions" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."AiPromptRevisions"
    ADD CONSTRAINT "PK_AiPromptRevisions" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."AiPromptTemplates"
    ADD CONSTRAINT "PK_AiPromptTemplates" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."AiQuotaPeriods"
    ADD CONSTRAINT "PK_AiQuotaPeriods" PRIMARY KEY ("UserId", "PeriodStartUtc");

ALTER TABLE ONLY public."AiQuotaReservations"
    ADD CONSTRAINT "PK_AiQuotaReservations" PRIMARY KEY ("RequestId");

ALTER TABLE ONLY public."AiUsages"
    ADD CONSTRAINT "PK_AiUsages" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."AtomicCommandReceipts"
    ADD CONSTRAINT "PK_AtomicCommandReceipts" PRIMARY KEY ("Key");

ALTER TABLE ONLY public."AuditEntries"
    ADD CONSTRAINT "PK_AuditEntries" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."BillingPayments"
    ADD CONSTRAINT "PK_BillingPayments" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."BillingSubscriptions"
    ADD CONSTRAINT "PK_BillingSubscriptions" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."BillingWebhookEvents"
    ADD CONSTRAINT "PK_BillingWebhookEvents" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."BugAcknowledgementReceipts"
    ADD CONSTRAINT "PK_BugAcknowledgementReceipts" PRIMARY KEY ("InboxId");

ALTER TABLE ONLY public."ClientTasks"
    ADD CONSTRAINT "PK_ClientTasks" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."ConsumedTelegramAssertions"
    ADD CONSTRAINT "PK_ConsumedTelegramAssertions" PRIMARY KEY ("Fingerprint");

ALTER TABLE ONLY public."ContentReports"
    ADD CONSTRAINT "PK_ContentReports" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."CycleBleedingEntries"
    ADD CONSTRAINT "PK_CycleBleedingEntries" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."CycleConsents"
    ADD CONSTRAINT "PK_CycleConsents" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."CycleDayNotes"
    ADD CONSTRAINT "PK_CycleDayNotes" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."CycleFactors"
    ADD CONSTRAINT "PK_CycleFactors" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."CycleMenstrualEpisodes"
    ADD CONSTRAINT "PK_CycleMenstrualEpisodes" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."CyclePredictionRevisions"
    ADD CONSTRAINT "PK_CyclePredictionRevisions" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."CycleProfiles"
    ADD CONSTRAINT "PK_CycleProfiles" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."CycleSymptomEntries"
    ADD CONSTRAINT "PK_CycleSymptomEntries" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."DailyAdvices"
    ADD CONSTRAINT "PK_DailyAdvices" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."DailyReferenceValues"
    ADD CONSTRAINT "PK_DailyReferenceValues" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."DietologistInvitations"
    ADD CONSTRAINT "PK_DietologistInvitations" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."EmailOutbox"
    ADD CONSTRAINT "PK_EmailOutbox" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."EmailTemplateRevisions"
    ADD CONSTRAINT "PK_EmailTemplateRevisions" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."EmailTemplates"
    ADD CONSTRAINT "PK_EmailTemplates" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."ExerciseEntries"
    ADD CONSTRAINT "PK_ExerciseEntries" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."FastingCheckIns"
    ADD CONSTRAINT "PK_FastingCheckIns" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."FastingOccurrences"
    ADD CONSTRAINT "PK_FastingOccurrences" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."FastingPlans"
    ADD CONSTRAINT "PK_FastingPlans" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."FastingSessions"
    ADD CONSTRAINT "PK_FastingSessions" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."FastingTelemetryEvents"
    ADD CONSTRAINT "PK_FastingTelemetryEvents" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."FavoriteMeals"
    ADD CONSTRAINT "PK_FavoriteMeals" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."FavoriteProducts"
    ADD CONSTRAINT "PK_FavoriteProducts" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."FavoriteRecipes"
    ADD CONSTRAINT "PK_FavoriteRecipes" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."FertilitySignals"
    ADD CONSTRAINT "PK_FertilitySignals" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."FoodRecognitionJobImages"
    ADD CONSTRAINT "PK_FoodRecognitionJobImages" PRIMARY KEY ("JobId", "ImageAssetId");

ALTER TABLE ONLY public."FoodRecognitionJobs"
    ADD CONSTRAINT "PK_FoodRecognitionJobs" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."HydrationEntries"
    ADD CONSTRAINT "PK_HydrationEntries" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."HydrationOperationReceipts"
    ADD CONSTRAINT "PK_HydrationOperationReceipts" PRIMARY KEY ("UserId", "OperationId");

ALTER TABLE ONLY public."ImageAssets"
    ADD CONSTRAINT "PK_ImageAssets" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."ImageObjectDeletionOutbox"
    ADD CONSTRAINT "PK_ImageObjectDeletionOutbox" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."MarketingAttributionEvents"
    ADD CONSTRAINT "PK_MarketingAttributionEvents" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."MealAiItems"
    ADD CONSTRAINT "PK_MealAiItems" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."MealAiSessions"
    ADD CONSTRAINT "PK_MealAiSessions" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."MealItems"
    ADD CONSTRAINT "PK_MealItems" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."MealPlanDays"
    ADD CONSTRAINT "PK_MealPlanDays" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."MealPlanMeals"
    ADD CONSTRAINT "PK_MealPlanMeals" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."MealPlans"
    ADD CONSTRAINT "PK_MealPlans" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."MealRecognitionReceipts"
    ADD CONSTRAINT "PK_MealRecognitionReceipts" PRIMARY KEY ("OperationId");

ALTER TABLE ONLY public."Meals"
    ADD CONSTRAINT "PK_Meals" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."NotificationWebPushOutbox"
    ADD CONSTRAINT "PK_NotificationWebPushOutbox" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."Notifications"
    ADD CONSTRAINT "PK_Notifications" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."NutritionLessons"
    ADD CONSTRAINT "PK_NutritionLessons" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."OpenFoodFactsProducts"
    ADD CONSTRAINT "PK_OpenFoodFactsProducts" PRIMARY KEY ("Barcode");

ALTER TABLE ONLY public."OutboxReplayAudits"
    ADD CONSTRAINT "PK_OutboxReplayAudits" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."ProductImages"
    ADD CONSTRAINT "PK_ProductImages" PRIMARY KEY ("ProductId", "ImageAssetId");

ALTER TABLE ONLY public."Products"
    ADD CONSTRAINT "PK_Products" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."RecentItems"
    ADD CONSTRAINT "PK_RecentItems" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."RecipeComments"
    ADD CONSTRAINT "PK_RecipeComments" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."RecipeImages"
    ADD CONSTRAINT "PK_RecipeImages" PRIMARY KEY ("RecipeId", "ImageAssetId");

ALTER TABLE ONLY public."RecipeIngredients"
    ADD CONSTRAINT "PK_RecipeIngredients" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."RecipeLikes"
    ADD CONSTRAINT "PK_RecipeLikes" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."RecipeStepImages"
    ADD CONSTRAINT "PK_RecipeStepImages" PRIMARY KEY ("RecipeStepId", "ImageAssetId");

ALTER TABLE ONLY public."RecipeSteps"
    ADD CONSTRAINT "PK_RecipeSteps" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."Recipes"
    ADD CONSTRAINT "PK_Recipes" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."RecommendationBulkDispatches"
    ADD CONSTRAINT "PK_RecommendationBulkDispatches" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."RecommendationComments"
    ADD CONSTRAINT "PK_RecommendationComments" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."RecommendationTemplates"
    ADD CONSTRAINT "PK_RecommendationTemplates" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."Recommendations"
    ADD CONSTRAINT "PK_Recommendations" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."Roles"
    ADD CONSTRAINT "PK_Roles" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."ShoppingListItemSources"
    ADD CONSTRAINT "PK_ShoppingListItemSources" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."ShoppingListItems"
    ADD CONSTRAINT "PK_ShoppingListItems" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."ShoppingLists"
    ADD CONSTRAINT "PK_ShoppingLists" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."TelegramAssertionPolicies"
    ADD CONSTRAINT "PK_TelegramAssertionPolicies" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."TelegramLoginTickets"
    ADD CONSTRAINT "PK_TelegramLoginTickets" PRIMARY KEY ("Fingerprint");

ALTER TABLE ONLY public."TelegramOperations"
    ADD CONSTRAINT "PK_TelegramOperations" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."UsdaFoodNutrients"
    ADD CONSTRAINT "PK_UsdaFoodNutrients" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."UsdaFoodPortions"
    ADD CONSTRAINT "PK_UsdaFoodPortions" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."UsdaFoods"
    ADD CONSTRAINT "PK_UsdaFoods" PRIMARY KEY ("FdcId");

ALTER TABLE ONLY public."UsdaNutrients"
    ADD CONSTRAINT "PK_UsdaNutrients" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."UserAchievements"
    ADD CONSTRAINT "PK_UserAchievements" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."UserLessonProgress"
    ADD CONSTRAINT "PK_UserLessonProgress" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."UserLoginEvents"
    ADD CONSTRAINT "PK_UserLoginEvents" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."UserNutritionProfiles"
    ADD CONSTRAINT "PK_UserNutritionProfiles" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."UserPreferences"
    ADD CONSTRAINT "PK_UserPreferences" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."UserRefreshTokenSessions"
    ADD CONSTRAINT "PK_UserRefreshTokenSessions" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."UserRoleAuditEvents"
    ADD CONSTRAINT "PK_UserRoleAuditEvents" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."UserRoles"
    ADD CONSTRAINT "PK_UserRoles" PRIMARY KEY ("UserId", "RoleId");

ALTER TABLE ONLY public."Users"
    ADD CONSTRAINT "PK_Users" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."WaistEntries"
    ADD CONSTRAINT "PK_WaistEntries" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."WaistGoals"
    ADD CONSTRAINT "PK_WaistGoals" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."WearableConnections"
    ADD CONSTRAINT "PK_WearableConnections" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."WearableSyncEntries"
    ADD CONSTRAINT "PK_WearableSyncEntries" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."WebPushSubscriptions"
    ADD CONSTRAINT "PK_WebPushSubscriptions" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."WeeklyGoals"
    ADD CONSTRAINT "PK_WeeklyGoals" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."WeightEntries"
    ADD CONSTRAINT "PK_WeightEntries" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."WeightGoals"
    ADD CONSTRAINT "PK_WeightGoals" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."__EFMigrationsHistory"
    ADD CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId");

CREATE INDEX "IX_AchievementDefinitions_IsActive_SortOrder" ON public."AchievementDefinitions" USING btree ("IsActive", "SortOrder");

CREATE UNIQUE INDEX "IX_AchievementDefinitions_Key" ON public."AchievementDefinitions" USING btree ("Key");

CREATE INDEX "IX_AchievementEvaluationOutbox_DueLease" ON public."AchievementEvaluationOutbox" USING btree ("ProcessedOnUtc", "DeadLetteredOnUtc", "NextAttemptOnUtc", "LockedUntilUtc");

CREATE UNIQUE INDEX "IX_AchievementEvaluationOutbox_UserId" ON public."AchievementEvaluationOutbox" USING btree ("UserId");

CREATE INDEX "IX_AdminImpersonationSessions_ActorUserId" ON public."AdminImpersonationSessions" USING btree ("ActorUserId");

CREATE INDEX "IX_AdminImpersonationSessions_StartedAtUtc" ON public."AdminImpersonationSessions" USING btree ("StartedAtUtc");

CREATE INDEX "IX_AdminImpersonationSessions_TargetUserId" ON public."AdminImpersonationSessions" USING btree ("TargetUserId");

CREATE INDEX "IX_AiPromptRevisions_TemplateId_ArchivedOnUtc" ON public."AiPromptRevisions" USING btree ("TemplateId", "ArchivedOnUtc");

CREATE INDEX "IX_AiPromptTemplates_Key_IsActive" ON public."AiPromptTemplates" USING btree ("Key", "IsActive");

CREATE UNIQUE INDEX "IX_AiPromptTemplates_Key_Locale" ON public."AiPromptTemplates" USING btree ("Key", "Locale");

CREATE INDEX "IX_AiQuotaReservations_UserId_PeriodStartUtc_State_ExpiresOnUtc" ON public."AiQuotaReservations" USING btree ("UserId", "PeriodStartUtc", "State", "ExpiresOnUtc");

CREATE INDEX "IX_AiUsages_CreatedOnUtc" ON public."AiUsages" USING btree ("CreatedOnUtc");

CREATE INDEX "IX_AiUsages_UserId" ON public."AiUsages" USING btree ("UserId");

CREATE INDEX "IX_AiUsages_UserId_CreatedOnUtc" ON public."AiUsages" USING btree ("UserId", "CreatedOnUtc");

CREATE INDEX "IX_AtomicCommandReceipts_ExpiresOnUtc" ON public."AtomicCommandReceipts" USING btree ("ExpiresOnUtc");

CREATE INDEX "IX_AtomicCommandReceipts_UserId" ON public."AtomicCommandReceipts" USING btree ("UserId");

CREATE INDEX "IX_AuditEntries_CreatedAtUtc" ON public."AuditEntries" USING btree ("CreatedAtUtc");

CREATE INDEX "IX_AuditEntries_SubjectClientUserId_CreatedAtUtc" ON public."AuditEntries" USING btree ("SubjectClientUserId", "CreatedAtUtc");

CREATE INDEX "IX_BillingPayments_BillingSubscriptionId" ON public."BillingPayments" USING btree ("BillingSubscriptionId");

CREATE INDEX "IX_BillingPayments_EffectiveOccurredAtUtc" ON public."BillingPayments" USING btree (COALESCE("OccurredAtUtc", "CreatedOnUtc")) WHERE (("Amount" IS NOT NULL) AND ("Currency" IS NOT NULL));

CREATE INDEX "IX_BillingPayments_Provider_ExternalCustomerId" ON public."BillingPayments" USING btree ("Provider", "ExternalCustomerId");

CREATE UNIQUE INDEX "IX_BillingPayments_Provider_ExternalPaymentId" ON public."BillingPayments" USING btree ("Provider", "ExternalPaymentId");

CREATE INDEX "IX_BillingPayments_Provider_ExternalPaymentMethodId" ON public."BillingPayments" USING btree ("Provider", "ExternalPaymentMethodId");

CREATE INDEX "IX_BillingPayments_Provider_ExternalSubscriptionId" ON public."BillingPayments" USING btree ("Provider", "ExternalSubscriptionId");

CREATE INDEX "IX_BillingPayments_UserId" ON public."BillingPayments" USING btree ("UserId");

CREATE UNIQUE INDEX "IX_BillingSubscriptions_Provider_ExternalCustomerId" ON public."BillingSubscriptions" USING btree ("Provider", "ExternalCustomerId");

CREATE INDEX "IX_BillingSubscriptions_Provider_ExternalPaymentMethodId" ON public."BillingSubscriptions" USING btree ("Provider", "ExternalPaymentMethodId");

CREATE UNIQUE INDEX "IX_BillingSubscriptions_Provider_ExternalSubscriptionId" ON public."BillingSubscriptions" USING btree ("Provider", "ExternalSubscriptionId");

CREATE UNIQUE INDEX "IX_BillingSubscriptions_UserId" ON public."BillingSubscriptions" USING btree ("UserId");

CREATE INDEX "IX_BillingWebhookEvents_ProcessedAtUtc" ON public."BillingWebhookEvents" USING btree ("ProcessedAtUtc");

CREATE UNIQUE INDEX "IX_BillingWebhookEvents_Provider_EventId" ON public."BillingWebhookEvents" USING btree ("Provider", "EventId");

CREATE INDEX "IX_BillingWebhookEvents_Status_NextAttemptAtUtc_ReceivedAtUtc" ON public."BillingWebhookEvents" USING btree ("Status", "NextAttemptAtUtc", "ReceivedAtUtc");

CREATE INDEX "IX_ClientTasks_ClientUserId_CreatedOnUtc" ON public."ClientTasks" USING btree ("ClientUserId", "CreatedOnUtc");

CREATE INDEX "IX_ClientTasks_DietologistUserId_ClientUserId" ON public."ClientTasks" USING btree ("DietologistUserId", "ClientUserId");

CREATE INDEX "IX_ClientTasks_Status_DueAtUtc_DueReminderSentAtUtc" ON public."ClientTasks" USING btree ("Status", "DueAtUtc", "DueReminderSentAtUtc");

CREATE INDEX "IX_ConsumedTelegramAssertions_ExpiresAtUtc" ON public."ConsumedTelegramAssertions" USING btree ("ExpiresAtUtc");

CREATE INDEX "IX_ContentReports_Status_CreatedOnUtc" ON public."ContentReports" USING btree ("Status", "CreatedOnUtc");

CREATE UNIQUE INDEX "IX_ContentReports_UserId_TargetType_TargetId" ON public."ContentReports" USING btree ("UserId", "TargetType", "TargetId");

CREATE UNIQUE INDEX "IX_CycleBleedingEntries_CycleProfileId_Date_Type" ON public."CycleBleedingEntries" USING btree ("CycleProfileId", "Date", "Type");

CREATE UNIQUE INDEX "IX_CycleConsents_CycleProfileId_Purpose" ON public."CycleConsents" USING btree ("CycleProfileId", "Purpose");

CREATE UNIQUE INDEX "IX_CycleDayNotes_CycleProfileId_Date" ON public."CycleDayNotes" USING btree ("CycleProfileId", "Date");

CREATE UNIQUE INDEX "IX_CycleFactors_CycleProfileId_Type_StartDate" ON public."CycleFactors" USING btree ("CycleProfileId", "Type", "StartDate");

CREATE UNIQUE INDEX "IX_CycleMenstrualEpisodes_CycleProfileId_StartDate" ON public."CycleMenstrualEpisodes" USING btree ("CycleProfileId", "StartDate");

CREATE INDEX "IX_CyclePredictionRevisions_CycleProfileId_GeneratedAtUtc" ON public."CyclePredictionRevisions" USING btree ("CycleProfileId", "GeneratedAtUtc");

CREATE UNIQUE INDEX "IX_CycleProfiles_UserId" ON public."CycleProfiles" USING btree ("UserId");

CREATE UNIQUE INDEX "IX_CycleSymptomEntries_CycleProfileId_Date_Category" ON public."CycleSymptomEntries" USING btree ("CycleProfileId", "Date", "Category");

CREATE UNIQUE INDEX "IX_DailyAdvices_GroupId_Locale" ON public."DailyAdvices" USING btree ("GroupId", "Locale");

CREATE INDEX "IX_DailyAdvices_Locale_Tag" ON public."DailyAdvices" USING btree ("Locale", "Tag");

CREATE UNIQUE INDEX "IX_DailyReferenceValues_NutrientId_AgeGroup_Gender" ON public."DailyReferenceValues" USING btree ("NutrientId", "AgeGroup", "Gender");

CREATE INDEX "IX_DietologistInvitations_ClientUserId" ON public."DietologistInvitations" USING btree ("ClientUserId");

CREATE INDEX "IX_DietologistInvitations_ClientUserId_Status" ON public."DietologistInvitations" USING btree ("ClientUserId", "Status");

CREATE INDEX "IX_DietologistInvitations_DietologistEmail_Status" ON public."DietologistInvitations" USING btree ("DietologistEmail", "Status");

CREATE INDEX "IX_DietologistInvitations_DietologistUserId" ON public."DietologistInvitations" USING btree ("DietologistUserId");

CREATE INDEX "IX_EmailOutbox_DueLease" ON public."EmailOutbox" USING btree ("ProcessedOnUtc", "DeadLetteredOnUtc", "NextAttemptOnUtc", "LockedUntilUtc");

CREATE INDEX "IX_EmailTemplateRevisions_TemplateId_ArchivedOnUtc" ON public."EmailTemplateRevisions" USING btree ("TemplateId", "ArchivedOnUtc");

CREATE UNIQUE INDEX "IX_EmailTemplates_Key_Locale" ON public."EmailTemplates" USING btree ("Key", "Locale");

CREATE INDEX "IX_ExerciseEntries_UserId_Date" ON public."ExerciseEntries" USING btree ("UserId", "Date");

CREATE INDEX "IX_FastingCheckIns_OccurrenceId_CheckedInAtUtc" ON public."FastingCheckIns" USING btree ("OccurrenceId", "CheckedInAtUtc");

CREATE INDEX "IX_FastingCheckIns_UserId_CheckedInAtUtc" ON public."FastingCheckIns" USING btree ("UserId", "CheckedInAtUtc");

CREATE INDEX "IX_FastingOccurrences_PlanId" ON public."FastingOccurrences" USING btree ("PlanId");

CREATE UNIQUE INDEX "IX_FastingOccurrences_PlanId_SequenceNumber" ON public."FastingOccurrences" USING btree ("PlanId", "SequenceNumber");

CREATE INDEX "IX_FastingOccurrences_StartedAtUtc_Active" ON public."FastingOccurrences" USING btree ("StartedAtUtc") WHERE (("Status")::text = 'Active'::text);

CREATE INDEX "IX_FastingOccurrences_UserId" ON public."FastingOccurrences" USING btree ("UserId");

CREATE INDEX "IX_FastingOccurrences_UserId_StartedAtUtc" ON public."FastingOccurrences" USING btree ("UserId", "StartedAtUtc");

CREATE INDEX "IX_FastingOccurrences_UserId_Status" ON public."FastingOccurrences" USING btree ("UserId", "Status");

CREATE INDEX "IX_FastingPlans_UserId" ON public."FastingPlans" USING btree ("UserId");

CREATE INDEX "IX_FastingPlans_UserId_Status" ON public."FastingPlans" USING btree ("UserId", "Status");

CREATE INDEX "IX_FastingSessions_UserId" ON public."FastingSessions" USING btree ("UserId");

CREATE INDEX "IX_FastingSessions_UserId_IsCompleted" ON public."FastingSessions" USING btree ("UserId", "IsCompleted");

CREATE INDEX "IX_FastingTelemetryEvents_Name_OccurredAtUtc" ON public."FastingTelemetryEvents" USING btree ("Name", "OccurredAtUtc");

CREATE INDEX "IX_FastingTelemetryEvents_OccurredAtUtc" ON public."FastingTelemetryEvents" USING btree ("OccurredAtUtc");

CREATE INDEX "IX_FastingTelemetryEvents_ReminderPresetId_Name_OccurredAtUtc" ON public."FastingTelemetryEvents" USING btree ("ReminderPresetId", "Name", "OccurredAtUtc");

CREATE INDEX "IX_FavoriteMeals_MealId" ON public."FavoriteMeals" USING btree ("MealId");

CREATE INDEX "IX_FavoriteMeals_UserId" ON public."FavoriteMeals" USING btree ("UserId");

CREATE UNIQUE INDEX "IX_FavoriteMeals_UserId_MealId" ON public."FavoriteMeals" USING btree ("UserId", "MealId") WHERE ("RemovedAtUtc" IS NULL);

CREATE INDEX "IX_FavoriteProducts_ProductId" ON public."FavoriteProducts" USING btree ("ProductId");

CREATE INDEX "IX_FavoriteProducts_UserId" ON public."FavoriteProducts" USING btree ("UserId");

CREATE UNIQUE INDEX "IX_FavoriteProducts_UserId_ProductId" ON public."FavoriteProducts" USING btree ("UserId", "ProductId");

CREATE INDEX "IX_FavoriteRecipes_RecipeId" ON public."FavoriteRecipes" USING btree ("RecipeId");

CREATE INDEX "IX_FavoriteRecipes_UserId" ON public."FavoriteRecipes" USING btree ("UserId");

CREATE UNIQUE INDEX "IX_FavoriteRecipes_UserId_RecipeId" ON public."FavoriteRecipes" USING btree ("UserId", "RecipeId");

CREATE UNIQUE INDEX "IX_FertilitySignals_CycleProfileId_Date" ON public."FertilitySignals" USING btree ("CycleProfileId", "Date");

CREATE INDEX "IX_FoodRecognitionJobImages_ImageAssetId" ON public."FoodRecognitionJobImages" USING btree ("ImageAssetId");

CREATE UNIQUE INDEX "IX_FoodRecognitionJobImages_JobId_Position" ON public."FoodRecognitionJobImages" USING btree ("JobId", "Position");

CREATE INDEX "IX_FoodRecognitionJobs_ImageAssetId" ON public."FoodRecognitionJobs" USING btree ("ImageAssetId");

CREATE INDEX "IX_FoodRecognitionJobs_Status_CreatedOnUtc" ON public."FoodRecognitionJobs" USING btree ("Status", "CreatedOnUtc");

CREATE INDEX "IX_FoodRecognitionJobs_UpdatedOnUtc" ON public."FoodRecognitionJobs" USING btree ("UpdatedOnUtc");

CREATE INDEX "IX_FoodRecognitionJobs_UserId_CreatedOnUtc" ON public."FoodRecognitionJobs" USING btree ("UserId", "CreatedOnUtc");

CREATE INDEX "IX_HydrationEntries_User_Timestamp" ON public."HydrationEntries" USING btree ("UserId", "Timestamp");

CREATE UNIQUE INDEX "IX_HydrationOperationReceipts_EntryId" ON public."HydrationOperationReceipts" USING btree ("EntryId");

CREATE INDEX "IX_ImageAssets_UserId" ON public."ImageAssets" USING btree ("UserId");

CREATE INDEX "IX_ImageObjectDeletionOutbox_DueLease" ON public."ImageObjectDeletionOutbox" USING btree ("ProcessedOnUtc", "DeadLetteredOnUtc", "NextAttemptOnUtc", "LockedUntilUtc");

CREATE INDEX "IX_MarketingAttributionEvents_AnonymousId_OccurredAtUtc" ON public."MarketingAttributionEvents" USING btree ("AnonymousId", "OccurredAtUtc");

CREATE INDEX "IX_MarketingAttributionEvents_OccurredAtUtc" ON public."MarketingAttributionEvents" USING btree ("OccurredAtUtc");

CREATE INDEX "IX_MarketingAttributionEvents_SessionId_OccurredAtUtc" ON public."MarketingAttributionEvents" USING btree ("SessionId", "OccurredAtUtc");

CREATE UNIQUE INDEX "IX_MarketingAttributionEvents_UserId_EventType" ON public."MarketingAttributionEvents" USING btree ("UserId", "EventType") WHERE (("UserId" IS NOT NULL) AND (("EventType")::text = ANY ((ARRAY['signup_completed'::character varying, 'premium_started'::character varying])::text[])));

CREATE INDEX "IX_MarketingAttributionEvents_UserId_OccurredAtUtc" ON public."MarketingAttributionEvents" USING btree ("UserId", "OccurredAtUtc");

CREATE INDEX "IX_MarketingAttributionEvents_UtmSource_UtmMedium_UtmCampaign_~" ON public."MarketingAttributionEvents" USING btree ("UtmSource", "UtmMedium", "UtmCampaign", "OccurredAtUtc");

CREATE INDEX "IX_MealAiItems_MealAiSessionId" ON public."MealAiItems" USING btree ("MealAiSessionId");

CREATE INDEX "IX_MealAiSessions_ImageAssetId" ON public."MealAiSessions" USING btree ("ImageAssetId");

CREATE INDEX "IX_MealAiSessions_MealId" ON public."MealAiSessions" USING btree ("MealId");

CREATE INDEX "IX_MealItems_MealId" ON public."MealItems" USING btree ("MealId");

CREATE INDEX "IX_MealItems_ProductId" ON public."MealItems" USING btree ("ProductId");

CREATE INDEX "IX_MealItems_RecipeId" ON public."MealItems" USING btree ("RecipeId");

CREATE UNIQUE INDEX "IX_MealPlanDays_MealPlanId_DayNumber" ON public."MealPlanDays" USING btree ("MealPlanId", "DayNumber");

CREATE INDEX "IX_MealPlanMeals_MealPlanDayId" ON public."MealPlanMeals" USING btree ("MealPlanDayId");

CREATE INDEX "IX_MealPlanMeals_RecipeId" ON public."MealPlanMeals" USING btree ("RecipeId");

CREATE INDEX "IX_MealPlans_DietType" ON public."MealPlans" USING btree ("DietType");

CREATE INDEX "IX_MealPlans_IsCurated" ON public."MealPlans" USING btree ("IsCurated");

CREATE INDEX "IX_MealPlans_UserId" ON public."MealPlans" USING btree ("UserId");

CREATE UNIQUE INDEX "IX_MealRecognitionReceipts_UserId_RecognitionId" ON public."MealRecognitionReceipts" USING btree ("UserId", "RecognitionId");

CREATE INDEX "IX_Meals_ImageAssetId" ON public."Meals" USING btree ("ImageAssetId");

CREATE INDEX "IX_Meals_UserId_Date_CreatedOnUtc" ON public."Meals" USING btree ("UserId", "Date", "CreatedOnUtc");

CREATE INDEX "IX_NotificationWebPushOutbox_DueLease" ON public."NotificationWebPushOutbox" USING btree ("ProcessedOnUtc", "DeadLetteredOnUtc", "NextAttemptOnUtc", "LockedUntilUtc");

CREATE INDEX "IX_NotificationWebPushOutbox_NotificationId" ON public."NotificationWebPushOutbox" USING btree ("NotificationId");

CREATE INDEX "IX_Notifications_UserId" ON public."Notifications" USING btree ("UserId");

CREATE INDEX "IX_Notifications_UserId_IsRead" ON public."Notifications" USING btree ("UserId", "IsRead");

CREATE INDEX "IX_NutritionLessons_Locale_Category" ON public."NutritionLessons" USING btree ("Locale", "Category");

CREATE INDEX "IX_OpenFoodFactsProducts_Brand" ON public."OpenFoodFactsProducts" USING gin ("Brand" public.gin_trgm_ops);

CREATE INDEX "IX_OpenFoodFactsProducts_Category" ON public."OpenFoodFactsProducts" USING gin ("Category" public.gin_trgm_ops);

CREATE INDEX "IX_OpenFoodFactsProducts_LastSeenAtUtc" ON public."OpenFoodFactsProducts" USING btree ("LastSeenAtUtc");

CREATE INDEX "IX_OpenFoodFactsProducts_Name" ON public."OpenFoodFactsProducts" USING gin ("Name" public.gin_trgm_ops);

CREATE INDEX "IX_OutboxReplayAudits_OutboxName_MessageId_RequestedOnUtc" ON public."OutboxReplayAudits" USING btree ("OutboxName", "MessageId", "RequestedOnUtc");

CREATE INDEX "IX_ProductImages_ImageAssetId" ON public."ProductImages" USING btree ("ImageAssetId");

CREATE INDEX "IX_Products_Barcode" ON public."Products" USING gin ("Barcode" public.gin_trgm_ops);

CREATE INDEX "IX_Products_Brand" ON public."Products" USING gin ("Brand" public.gin_trgm_ops);

CREATE INDEX "IX_Products_Category" ON public."Products" USING gin ("Category" public.gin_trgm_ops);

CREATE INDEX "IX_Products_ImageAssetId" ON public."Products" USING btree ("ImageAssetId");

CREATE INDEX "IX_Products_Name" ON public."Products" USING gin ("Name" public.gin_trgm_ops);

CREATE INDEX "IX_Products_UsdaFdcId" ON public."Products" USING btree ("UsdaFdcId");

CREATE INDEX "IX_Products_UserId_CreatedOnUtc" ON public."Products" USING btree ("UserId", "CreatedOnUtc");

CREATE INDEX "IX_Products_Visibility_CreatedOnUtc" ON public."Products" USING btree ("Visibility", "CreatedOnUtc");

CREATE UNIQUE INDEX "IX_RecentItems_UserId_ItemType_ItemId" ON public."RecentItems" USING btree ("UserId", "ItemType", "ItemId");

CREATE INDEX "IX_RecentItems_UserId_ItemType_LastUsedAtUtc" ON public."RecentItems" USING btree ("UserId", "ItemType", "LastUsedAtUtc");

CREATE INDEX "IX_RecipeComments_RecipeId_CreatedOnUtc" ON public."RecipeComments" USING btree ("RecipeId", "CreatedOnUtc");

CREATE INDEX "IX_RecipeComments_UserId" ON public."RecipeComments" USING btree ("UserId");

CREATE INDEX "IX_RecipeImages_ImageAssetId" ON public."RecipeImages" USING btree ("ImageAssetId");

CREATE INDEX "IX_RecipeIngredients_NestedRecipeId" ON public."RecipeIngredients" USING btree ("NestedRecipeId");

CREATE INDEX "IX_RecipeIngredients_ProductId" ON public."RecipeIngredients" USING btree ("ProductId");

CREATE INDEX "IX_RecipeIngredients_RecipeStepId" ON public."RecipeIngredients" USING btree ("RecipeStepId");

CREATE INDEX "IX_RecipeLikes_RecipeId" ON public."RecipeLikes" USING btree ("RecipeId");

CREATE UNIQUE INDEX "IX_RecipeLikes_UserId_RecipeId" ON public."RecipeLikes" USING btree ("UserId", "RecipeId");

CREATE INDEX "IX_RecipeStepImages_ImageAssetId" ON public."RecipeStepImages" USING btree ("ImageAssetId");

CREATE INDEX "IX_RecipeSteps_ImageAssetId" ON public."RecipeSteps" USING btree ("ImageAssetId");

CREATE INDEX "IX_RecipeSteps_RecipeId" ON public."RecipeSteps" USING btree ("RecipeId");

CREATE INDEX "IX_Recipes_Category" ON public."Recipes" USING gin ("Category" public.gin_trgm_ops);

CREATE INDEX "IX_Recipes_Description" ON public."Recipes" USING gin ("Description" public.gin_trgm_ops);

CREATE INDEX "IX_Recipes_ImageAssetId" ON public."Recipes" USING btree ("ImageAssetId");

CREATE INDEX "IX_Recipes_Name" ON public."Recipes" USING gin ("Name" public.gin_trgm_ops);

CREATE INDEX "IX_Recipes_UserId_CreatedOnUtc" ON public."Recipes" USING btree ("UserId", "CreatedOnUtc");

CREATE INDEX "IX_Recipes_Visibility_CreatedOnUtc" ON public."Recipes" USING btree ("Visibility", "CreatedOnUtc");

CREATE INDEX "IX_Recipes_Visibility_Language_CreatedOnUtc" ON public."Recipes" USING btree ("Visibility", "Language", "CreatedOnUtc");

CREATE INDEX "IX_RecommendationBulkDispatches_ClientUserId" ON public."RecommendationBulkDispatches" USING btree ("ClientUserId");

CREATE UNIQUE INDEX "IX_RecommendationBulkDispatches_DietologistUserId_IdempotencyK~" ON public."RecommendationBulkDispatches" USING btree ("DietologistUserId", "IdempotencyKey", "ClientUserId");

CREATE INDEX "IX_RecommendationBulkDispatches_RecommendationId" ON public."RecommendationBulkDispatches" USING btree ("RecommendationId");

CREATE INDEX "IX_RecommendationComments_AuthorUserId" ON public."RecommendationComments" USING btree ("AuthorUserId");

CREATE INDEX "IX_RecommendationComments_RecommendationId_CreatedOnUtc" ON public."RecommendationComments" USING btree ("RecommendationId", "CreatedOnUtc");

CREATE INDEX "IX_RecommendationTemplates_DietologistUserId_IsArchived_Name" ON public."RecommendationTemplates" USING btree ("DietologistUserId", "IsArchived", "Name");

CREATE INDEX "IX_Recommendations_ClientUserId" ON public."Recommendations" USING btree ("ClientUserId");

CREATE INDEX "IX_Recommendations_DietologistUserId_ClientUserId" ON public."Recommendations" USING btree ("DietologistUserId", "ClientUserId");

CREATE UNIQUE INDEX "IX_Roles_Name" ON public."Roles" USING btree ("Name");

CREATE INDEX "IX_ShoppingListItemSources_MealPlanId" ON public."ShoppingListItemSources" USING btree ("MealPlanId");

CREATE INDEX "IX_ShoppingListItemSources_RecipeId" ON public."ShoppingListItemSources" USING btree ("RecipeId");

CREATE INDEX "IX_ShoppingListItemSources_ShoppingListItemId" ON public."ShoppingListItemSources" USING btree ("ShoppingListItemId");

CREATE INDEX "IX_ShoppingListItems_ProductId" ON public."ShoppingListItems" USING btree ("ProductId");

CREATE INDEX "IX_ShoppingListItems_ShoppingListId" ON public."ShoppingListItems" USING btree ("ShoppingListId");

CREATE INDEX "IX_ShoppingLists_UserId" ON public."ShoppingLists" USING btree ("UserId");

CREATE INDEX "IX_TelegramLoginTickets_ExpiresAtUtc" ON public."TelegramLoginTickets" USING btree ("ExpiresAtUtc");

CREATE INDEX "IX_TelegramOperations_BotId_Completed_NextAttemptAtUtc" ON public."TelegramOperations" USING btree ("BotId", "Completed", "NextAttemptAtUtc");

CREATE UNIQUE INDEX "IX_TelegramOperations_BotId_UpdateId" ON public."TelegramOperations" USING btree ("BotId", "UpdateId");

CREATE INDEX "IX_TelegramOperations_UserId" ON public."TelegramOperations" USING btree ("UserId");

CREATE UNIQUE INDEX "IX_UsdaFoodNutrients_FdcId_NutrientId" ON public."UsdaFoodNutrients" USING btree ("FdcId", "NutrientId");

CREATE INDEX "IX_UsdaFoodNutrients_NutrientId" ON public."UsdaFoodNutrients" USING btree ("NutrientId");

CREATE INDEX "IX_UsdaFoodPortions_FdcId" ON public."UsdaFoodPortions" USING btree ("FdcId");

CREATE INDEX "IX_UsdaFoods_Description" ON public."UsdaFoods" USING gin ("Description" public.gin_trgm_ops);

CREATE INDEX "IX_UsdaFoods_FoodCategoryId" ON public."UsdaFoods" USING btree ("FoodCategoryId");

CREATE UNIQUE INDEX "IX_UserAchievements_UserId_AchievementKey" ON public."UserAchievements" USING btree ("UserId", "AchievementKey");

CREATE INDEX "IX_UserAchievements_UserId_EarnedAtUtc" ON public."UserAchievements" USING btree ("UserId", "EarnedAtUtc");

CREATE INDEX "IX_UserLessonProgress_LessonId" ON public."UserLessonProgress" USING btree ("LessonId");

CREATE UNIQUE INDEX "IX_UserLessonProgress_UserId_LessonId" ON public."UserLessonProgress" USING btree ("UserId", "LessonId");

CREATE INDEX "IX_UserLoginEvents_AuthProvider" ON public."UserLoginEvents" USING btree ("AuthProvider");

CREATE INDEX "IX_UserLoginEvents_BrowserName" ON public."UserLoginEvents" USING btree ("BrowserName");

CREATE INDEX "IX_UserLoginEvents_DeviceType" ON public."UserLoginEvents" USING btree ("DeviceType");

CREATE INDEX "IX_UserLoginEvents_LoggedInAtUtc" ON public."UserLoginEvents" USING btree ("LoggedInAtUtc");

CREATE INDEX "IX_UserLoginEvents_OperatingSystem" ON public."UserLoginEvents" USING btree ("OperatingSystem");

CREATE INDEX "IX_UserLoginEvents_UserId" ON public."UserLoginEvents" USING btree ("UserId");

CREATE INDEX "IX_UserRefreshTokenSessions_LastRotatedAtUtc" ON public."UserRefreshTokenSessions" USING btree ("LastRotatedAtUtc");

CREATE INDEX "IX_UserRefreshTokenSessions_RevokedAtUtc" ON public."UserRefreshTokenSessions" USING btree ("RevokedAtUtc");

CREATE INDEX "IX_UserRefreshTokenSessions_UserId" ON public."UserRefreshTokenSessions" USING btree ("UserId");

CREATE INDEX "IX_UserRoleAuditEvents_Action" ON public."UserRoleAuditEvents" USING btree ("Action");

CREATE INDEX "IX_UserRoleAuditEvents_ActorUserId" ON public."UserRoleAuditEvents" USING btree ("ActorUserId");

CREATE INDEX "IX_UserRoleAuditEvents_OccurredAtUtc" ON public."UserRoleAuditEvents" USING btree ("OccurredAtUtc");

CREATE INDEX "IX_UserRoleAuditEvents_RoleId" ON public."UserRoleAuditEvents" USING btree ("RoleId");

CREATE INDEX "IX_UserRoleAuditEvents_RoleName" ON public."UserRoleAuditEvents" USING btree ("RoleName");

CREATE INDEX "IX_UserRoleAuditEvents_UserId" ON public."UserRoleAuditEvents" USING btree ("UserId");

CREATE INDEX "IX_UserRoles_RoleId" ON public."UserRoles" USING btree ("RoleId");

CREATE UNIQUE INDEX "IX_Users_Email" ON public."Users" USING btree ("Email");

CREATE UNIQUE INDEX "IX_Users_GoogleIssuer_GoogleSubject" ON public."Users" USING btree ("GoogleIssuer", "GoogleSubject") WHERE (("GoogleIssuer" IS NOT NULL) AND ("GoogleSubject" IS NOT NULL));

CREATE INDEX "IX_Users_ProfileImageAssetId" ON public."Users" USING btree ("ProfileImageAssetId");

CREATE UNIQUE INDEX "IX_Users_TelegramOidcIssuer_TelegramOidcSubject" ON public."Users" USING btree ("TelegramOidcIssuer", "TelegramOidcSubject") WHERE (("TelegramOidcIssuer" IS NOT NULL) AND ("TelegramOidcSubject" IS NOT NULL));

CREATE UNIQUE INDEX "IX_Users_TelegramUserId" ON public."Users" USING btree ("TelegramUserId");

CREATE UNIQUE INDEX "IX_WaistEntries_UserId_Date" ON public."WaistEntries" USING btree ("UserId", "Date");

CREATE UNIQUE INDEX "IX_WaistGoals_UserId" ON public."WaistGoals" USING btree ("UserId") WHERE (("Status")::text = 'Active'::text);

CREATE UNIQUE INDEX "IX_WearableConnections_UserId_Provider" ON public."WearableConnections" USING btree ("UserId", "Provider");

CREATE INDEX "IX_WearableSyncEntries_UserId_Date" ON public."WearableSyncEntries" USING btree ("UserId", "Date");

CREATE UNIQUE INDEX "IX_WearableSyncEntries_UserId_Provider_DataType_Date" ON public."WearableSyncEntries" USING btree ("UserId", "Provider", "DataType", "Date");

CREATE UNIQUE INDEX "IX_WebPushSubscriptions_Endpoint" ON public."WebPushSubscriptions" USING btree ("Endpoint");

CREATE INDEX "IX_WebPushSubscriptions_UserId" ON public."WebPushSubscriptions" USING btree ("UserId");

CREATE INDEX "IX_WeeklyGoals_ReminderEnabled_WeekStartUtc" ON public."WeeklyGoals" USING btree ("ReminderEnabled", "WeekStartUtc");

CREATE UNIQUE INDEX "IX_WeeklyGoals_UserId_WeekStartUtc" ON public."WeeklyGoals" USING btree ("UserId", "WeekStartUtc");

CREATE UNIQUE INDEX "IX_WeightEntries_UserId_Date" ON public."WeightEntries" USING btree ("UserId", "Date");

CREATE UNIQUE INDEX "IX_WeightGoals_UserId" ON public."WeightGoals" USING btree ("UserId") WHERE (("Status")::text = 'Active'::text);

ALTER TABLE ONLY public."AdminImpersonationSessions"
    ADD CONSTRAINT "FK_AdminImpersonationSessions_Users_ActorUserId" FOREIGN KEY ("ActorUserId") REFERENCES public."Users"("Id") ON DELETE RESTRICT;

ALTER TABLE ONLY public."AdminImpersonationSessions"
    ADD CONSTRAINT "FK_AdminImpersonationSessions_Users_TargetUserId" FOREIGN KEY ("TargetUserId") REFERENCES public."Users"("Id") ON DELETE RESTRICT;

ALTER TABLE ONLY public."AiPromptRevisions"
    ADD CONSTRAINT "FK_AiPromptRevisions_AiPromptTemplates_TemplateId" FOREIGN KEY ("TemplateId") REFERENCES public."AiPromptTemplates"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."AiQuotaPeriods"
    ADD CONSTRAINT "FK_AiQuotaPeriods_Users_UserId" FOREIGN KEY ("UserId") REFERENCES public."Users"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."AiQuotaReservations"
    ADD CONSTRAINT "FK_AiQuotaReservations_AiQuotaPeriods_UserId_PeriodStartUtc" FOREIGN KEY ("UserId", "PeriodStartUtc") REFERENCES public."AiQuotaPeriods"("UserId", "PeriodStartUtc") ON DELETE CASCADE;

ALTER TABLE ONLY public."AiUsages"
    ADD CONSTRAINT "FK_AiUsages_Users_UserId" FOREIGN KEY ("UserId") REFERENCES public."Users"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."BillingPayments"
    ADD CONSTRAINT "FK_BillingPayments_BillingSubscriptions_BillingSubscriptionId" FOREIGN KEY ("BillingSubscriptionId") REFERENCES public."BillingSubscriptions"("Id") ON DELETE SET NULL;

ALTER TABLE ONLY public."BillingPayments"
    ADD CONSTRAINT "FK_BillingPayments_Users_UserId" FOREIGN KEY ("UserId") REFERENCES public."Users"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."BillingSubscriptions"
    ADD CONSTRAINT "FK_BillingSubscriptions_Users_UserId" FOREIGN KEY ("UserId") REFERENCES public."Users"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."ClientTasks"
    ADD CONSTRAINT "FK_ClientTasks_Users_ClientUserId" FOREIGN KEY ("ClientUserId") REFERENCES public."Users"("Id") ON DELETE RESTRICT;

ALTER TABLE ONLY public."ClientTasks"
    ADD CONSTRAINT "FK_ClientTasks_Users_DietologistUserId" FOREIGN KEY ("DietologistUserId") REFERENCES public."Users"("Id") ON DELETE RESTRICT;

ALTER TABLE ONLY public."ContentReports"
    ADD CONSTRAINT "FK_ContentReports_Users_UserId" FOREIGN KEY ("UserId") REFERENCES public."Users"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."CycleBleedingEntries"
    ADD CONSTRAINT "FK_CycleBleedingEntries_CycleProfiles_CycleProfileId" FOREIGN KEY ("CycleProfileId") REFERENCES public."CycleProfiles"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."CycleConsents"
    ADD CONSTRAINT "FK_CycleConsents_CycleProfiles_CycleProfileId" FOREIGN KEY ("CycleProfileId") REFERENCES public."CycleProfiles"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."CycleDayNotes"
    ADD CONSTRAINT "FK_CycleDayNotes_CycleProfiles_CycleProfileId" FOREIGN KEY ("CycleProfileId") REFERENCES public."CycleProfiles"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."CycleFactors"
    ADD CONSTRAINT "FK_CycleFactors_CycleProfiles_CycleProfileId" FOREIGN KEY ("CycleProfileId") REFERENCES public."CycleProfiles"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."CycleMenstrualEpisodes"
    ADD CONSTRAINT "FK_CycleMenstrualEpisodes_CycleProfiles_CycleProfileId" FOREIGN KEY ("CycleProfileId") REFERENCES public."CycleProfiles"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."CyclePredictionRevisions"
    ADD CONSTRAINT "FK_CyclePredictionRevisions_CycleProfiles_CycleProfileId" FOREIGN KEY ("CycleProfileId") REFERENCES public."CycleProfiles"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."CycleProfiles"
    ADD CONSTRAINT "FK_CycleProfiles_Users_UserId" FOREIGN KEY ("UserId") REFERENCES public."Users"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."CycleSymptomEntries"
    ADD CONSTRAINT "FK_CycleSymptomEntries_CycleProfiles_CycleProfileId" FOREIGN KEY ("CycleProfileId") REFERENCES public."CycleProfiles"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."DailyReferenceValues"
    ADD CONSTRAINT "FK_DailyReferenceValues_UsdaNutrients_NutrientId" FOREIGN KEY ("NutrientId") REFERENCES public."UsdaNutrients"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."DietologistInvitations"
    ADD CONSTRAINT "FK_DietologistInvitations_Users_ClientUserId" FOREIGN KEY ("ClientUserId") REFERENCES public."Users"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."DietologistInvitations"
    ADD CONSTRAINT "FK_DietologistInvitations_Users_DietologistUserId" FOREIGN KEY ("DietologistUserId") REFERENCES public."Users"("Id") ON DELETE SET NULL;

ALTER TABLE ONLY public."EmailTemplateRevisions"
    ADD CONSTRAINT "FK_EmailTemplateRevisions_EmailTemplates_TemplateId" FOREIGN KEY ("TemplateId") REFERENCES public."EmailTemplates"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."ExerciseEntries"
    ADD CONSTRAINT "FK_ExerciseEntries_Users_UserId" FOREIGN KEY ("UserId") REFERENCES public."Users"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."FastingCheckIns"
    ADD CONSTRAINT "FK_FastingCheckIns_FastingOccurrences_OccurrenceId" FOREIGN KEY ("OccurrenceId") REFERENCES public."FastingOccurrences"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."FastingCheckIns"
    ADD CONSTRAINT "FK_FastingCheckIns_Users_UserId" FOREIGN KEY ("UserId") REFERENCES public."Users"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."FastingOccurrences"
    ADD CONSTRAINT "FK_FastingOccurrences_FastingPlans_PlanId" FOREIGN KEY ("PlanId") REFERENCES public."FastingPlans"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."FastingOccurrences"
    ADD CONSTRAINT "FK_FastingOccurrences_Users_UserId" FOREIGN KEY ("UserId") REFERENCES public."Users"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."FastingPlans"
    ADD CONSTRAINT "FK_FastingPlans_Users_UserId" FOREIGN KEY ("UserId") REFERENCES public."Users"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."FastingSessions"
    ADD CONSTRAINT "FK_FastingSessions_Users_UserId" FOREIGN KEY ("UserId") REFERENCES public."Users"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."FavoriteMeals"
    ADD CONSTRAINT "FK_FavoriteMeals_Meals_MealId" FOREIGN KEY ("MealId") REFERENCES public."Meals"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."FavoriteMeals"
    ADD CONSTRAINT "FK_FavoriteMeals_Users_UserId" FOREIGN KEY ("UserId") REFERENCES public."Users"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."FavoriteProducts"
    ADD CONSTRAINT "FK_FavoriteProducts_Products_ProductId" FOREIGN KEY ("ProductId") REFERENCES public."Products"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."FavoriteProducts"
    ADD CONSTRAINT "FK_FavoriteProducts_Users_UserId" FOREIGN KEY ("UserId") REFERENCES public."Users"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."FavoriteRecipes"
    ADD CONSTRAINT "FK_FavoriteRecipes_Recipes_RecipeId" FOREIGN KEY ("RecipeId") REFERENCES public."Recipes"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."FavoriteRecipes"
    ADD CONSTRAINT "FK_FavoriteRecipes_Users_UserId" FOREIGN KEY ("UserId") REFERENCES public."Users"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."FertilitySignals"
    ADD CONSTRAINT "FK_FertilitySignals_CycleProfiles_CycleProfileId" FOREIGN KEY ("CycleProfileId") REFERENCES public."CycleProfiles"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."FoodRecognitionJobImages"
    ADD CONSTRAINT "FK_FoodRecognitionJobImages_FoodRecognitionJobs_JobId" FOREIGN KEY ("JobId") REFERENCES public."FoodRecognitionJobs"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."FoodRecognitionJobImages"
    ADD CONSTRAINT "FK_FoodRecognitionJobImages_ImageAssets_ImageAssetId" FOREIGN KEY ("ImageAssetId") REFERENCES public."ImageAssets"("Id");

ALTER TABLE ONLY public."FoodRecognitionJobs"
    ADD CONSTRAINT "FK_FoodRecognitionJobs_ImageAssets_ImageAssetId" FOREIGN KEY ("ImageAssetId") REFERENCES public."ImageAssets"("Id");

ALTER TABLE ONLY public."FoodRecognitionJobs"
    ADD CONSTRAINT "FK_FoodRecognitionJobs_Users_UserId" FOREIGN KEY ("UserId") REFERENCES public."Users"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."HydrationEntries"
    ADD CONSTRAINT "FK_HydrationEntries_Users_UserId" FOREIGN KEY ("UserId") REFERENCES public."Users"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."HydrationOperationReceipts"
    ADD CONSTRAINT "FK_HydrationOperationReceipts_Users_UserId" FOREIGN KEY ("UserId") REFERENCES public."Users"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."ImageAssets"
    ADD CONSTRAINT "FK_ImageAssets_Users_UserId" FOREIGN KEY ("UserId") REFERENCES public."Users"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."MealAiItems"
    ADD CONSTRAINT "FK_MealAiItems_MealAiSessions_MealAiSessionId" FOREIGN KEY ("MealAiSessionId") REFERENCES public."MealAiSessions"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."MealAiSessions"
    ADD CONSTRAINT "FK_MealAiSessions_ImageAssets_ImageAssetId" FOREIGN KEY ("ImageAssetId") REFERENCES public."ImageAssets"("Id");

ALTER TABLE ONLY public."MealAiSessions"
    ADD CONSTRAINT "FK_MealAiSessions_Meals_MealId" FOREIGN KEY ("MealId") REFERENCES public."Meals"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."MealItems"
    ADD CONSTRAINT "FK_MealItems_Meals_MealId" FOREIGN KEY ("MealId") REFERENCES public."Meals"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."MealItems"
    ADD CONSTRAINT "FK_MealItems_Products_ProductId" FOREIGN KEY ("ProductId") REFERENCES public."Products"("Id");

ALTER TABLE ONLY public."MealItems"
    ADD CONSTRAINT "FK_MealItems_Recipes_RecipeId" FOREIGN KEY ("RecipeId") REFERENCES public."Recipes"("Id");

ALTER TABLE ONLY public."MealPlanDays"
    ADD CONSTRAINT "FK_MealPlanDays_MealPlans_MealPlanId" FOREIGN KEY ("MealPlanId") REFERENCES public."MealPlans"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."MealPlanMeals"
    ADD CONSTRAINT "FK_MealPlanMeals_MealPlanDays_MealPlanDayId" FOREIGN KEY ("MealPlanDayId") REFERENCES public."MealPlanDays"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."MealPlanMeals"
    ADD CONSTRAINT "FK_MealPlanMeals_Recipes_RecipeId" FOREIGN KEY ("RecipeId") REFERENCES public."Recipes"("Id") ON DELETE RESTRICT;

ALTER TABLE ONLY public."MealPlans"
    ADD CONSTRAINT "FK_MealPlans_Users_UserId" FOREIGN KEY ("UserId") REFERENCES public."Users"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."MealRecognitionReceipts"
    ADD CONSTRAINT "FK_MealRecognitionReceipts_Users_UserId" FOREIGN KEY ("UserId") REFERENCES public."Users"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."Meals"
    ADD CONSTRAINT "FK_Meals_ImageAssets_ImageAssetId" FOREIGN KEY ("ImageAssetId") REFERENCES public."ImageAssets"("Id");

ALTER TABLE ONLY public."Meals"
    ADD CONSTRAINT "FK_Meals_Users_UserId" FOREIGN KEY ("UserId") REFERENCES public."Users"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."NotificationWebPushOutbox"
    ADD CONSTRAINT "FK_NotificationWebPushOutbox_Notifications_NotificationId" FOREIGN KEY ("NotificationId") REFERENCES public."Notifications"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."Notifications"
    ADD CONSTRAINT "FK_Notifications_Users_UserId" FOREIGN KEY ("UserId") REFERENCES public."Users"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."ProductImages"
    ADD CONSTRAINT "FK_ProductImages_ImageAssets_ImageAssetId" FOREIGN KEY ("ImageAssetId") REFERENCES public."ImageAssets"("Id");

ALTER TABLE ONLY public."ProductImages"
    ADD CONSTRAINT "FK_ProductImages_Products_ProductId" FOREIGN KEY ("ProductId") REFERENCES public."Products"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."Products"
    ADD CONSTRAINT "FK_Products_ImageAssets_ImageAssetId" FOREIGN KEY ("ImageAssetId") REFERENCES public."ImageAssets"("Id");

ALTER TABLE ONLY public."Products"
    ADD CONSTRAINT "FK_Products_Users_UserId" FOREIGN KEY ("UserId") REFERENCES public."Users"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."RecentItems"
    ADD CONSTRAINT "FK_RecentItems_Users_UserId" FOREIGN KEY ("UserId") REFERENCES public."Users"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."RecipeComments"
    ADD CONSTRAINT "FK_RecipeComments_Recipes_RecipeId" FOREIGN KEY ("RecipeId") REFERENCES public."Recipes"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."RecipeComments"
    ADD CONSTRAINT "FK_RecipeComments_Users_UserId" FOREIGN KEY ("UserId") REFERENCES public."Users"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."RecipeImages"
    ADD CONSTRAINT "FK_RecipeImages_ImageAssets_ImageAssetId" FOREIGN KEY ("ImageAssetId") REFERENCES public."ImageAssets"("Id");

ALTER TABLE ONLY public."RecipeImages"
    ADD CONSTRAINT "FK_RecipeImages_Recipes_RecipeId" FOREIGN KEY ("RecipeId") REFERENCES public."Recipes"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."RecipeIngredients"
    ADD CONSTRAINT "FK_RecipeIngredients_Products_ProductId" FOREIGN KEY ("ProductId") REFERENCES public."Products"("Id");

ALTER TABLE ONLY public."RecipeIngredients"
    ADD CONSTRAINT "FK_RecipeIngredients_RecipeSteps_RecipeStepId" FOREIGN KEY ("RecipeStepId") REFERENCES public."RecipeSteps"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."RecipeIngredients"
    ADD CONSTRAINT "FK_RecipeIngredients_Recipes_NestedRecipeId" FOREIGN KEY ("NestedRecipeId") REFERENCES public."Recipes"("Id") ON DELETE RESTRICT;

ALTER TABLE ONLY public."RecipeLikes"
    ADD CONSTRAINT "FK_RecipeLikes_Users_UserId" FOREIGN KEY ("UserId") REFERENCES public."Users"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."RecipeStepImages"
    ADD CONSTRAINT "FK_RecipeStepImages_ImageAssets_ImageAssetId" FOREIGN KEY ("ImageAssetId") REFERENCES public."ImageAssets"("Id");

ALTER TABLE ONLY public."RecipeStepImages"
    ADD CONSTRAINT "FK_RecipeStepImages_RecipeSteps_RecipeStepId" FOREIGN KEY ("RecipeStepId") REFERENCES public."RecipeSteps"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."RecipeSteps"
    ADD CONSTRAINT "FK_RecipeSteps_ImageAssets_ImageAssetId" FOREIGN KEY ("ImageAssetId") REFERENCES public."ImageAssets"("Id");

ALTER TABLE ONLY public."RecipeSteps"
    ADD CONSTRAINT "FK_RecipeSteps_Recipes_RecipeId" FOREIGN KEY ("RecipeId") REFERENCES public."Recipes"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."Recipes"
    ADD CONSTRAINT "FK_Recipes_ImageAssets_ImageAssetId" FOREIGN KEY ("ImageAssetId") REFERENCES public."ImageAssets"("Id");

ALTER TABLE ONLY public."Recipes"
    ADD CONSTRAINT "FK_Recipes_Users_UserId" FOREIGN KEY ("UserId") REFERENCES public."Users"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."RecommendationBulkDispatches"
    ADD CONSTRAINT "FK_RecommendationBulkDispatches_Recommendations_Recommendation~" FOREIGN KEY ("RecommendationId") REFERENCES public."Recommendations"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."RecommendationBulkDispatches"
    ADD CONSTRAINT "FK_RecommendationBulkDispatches_Users_ClientUserId" FOREIGN KEY ("ClientUserId") REFERENCES public."Users"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."RecommendationBulkDispatches"
    ADD CONSTRAINT "FK_RecommendationBulkDispatches_Users_DietologistUserId" FOREIGN KEY ("DietologistUserId") REFERENCES public."Users"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."RecommendationComments"
    ADD CONSTRAINT "FK_RecommendationComments_Recommendations_RecommendationId" FOREIGN KEY ("RecommendationId") REFERENCES public."Recommendations"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."RecommendationComments"
    ADD CONSTRAINT "FK_RecommendationComments_Users_AuthorUserId" FOREIGN KEY ("AuthorUserId") REFERENCES public."Users"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."RecommendationTemplates"
    ADD CONSTRAINT "FK_RecommendationTemplates_Users_DietologistUserId" FOREIGN KEY ("DietologistUserId") REFERENCES public."Users"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."Recommendations"
    ADD CONSTRAINT "FK_Recommendations_Users_ClientUserId" FOREIGN KEY ("ClientUserId") REFERENCES public."Users"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."Recommendations"
    ADD CONSTRAINT "FK_Recommendations_Users_DietologistUserId" FOREIGN KEY ("DietologistUserId") REFERENCES public."Users"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."ShoppingListItemSources"
    ADD CONSTRAINT "FK_ShoppingListItemSources_ShoppingListItems_ShoppingListItemId" FOREIGN KEY ("ShoppingListItemId") REFERENCES public."ShoppingListItems"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."ShoppingListItems"
    ADD CONSTRAINT "FK_ShoppingListItems_Products_ProductId" FOREIGN KEY ("ProductId") REFERENCES public."Products"("Id") ON DELETE SET NULL;

ALTER TABLE ONLY public."ShoppingListItems"
    ADD CONSTRAINT "FK_ShoppingListItems_ShoppingLists_ShoppingListId" FOREIGN KEY ("ShoppingListId") REFERENCES public."ShoppingLists"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."ShoppingLists"
    ADD CONSTRAINT "FK_ShoppingLists_Users_UserId" FOREIGN KEY ("UserId") REFERENCES public."Users"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."UsdaFoodNutrients"
    ADD CONSTRAINT "FK_UsdaFoodNutrients_UsdaFoods_FdcId" FOREIGN KEY ("FdcId") REFERENCES public."UsdaFoods"("FdcId") ON DELETE CASCADE;

ALTER TABLE ONLY public."UsdaFoodNutrients"
    ADD CONSTRAINT "FK_UsdaFoodNutrients_UsdaNutrients_NutrientId" FOREIGN KEY ("NutrientId") REFERENCES public."UsdaNutrients"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."UsdaFoodPortions"
    ADD CONSTRAINT "FK_UsdaFoodPortions_UsdaFoods_FdcId" FOREIGN KEY ("FdcId") REFERENCES public."UsdaFoods"("FdcId") ON DELETE CASCADE;

ALTER TABLE ONLY public."UserAchievements"
    ADD CONSTRAINT "FK_UserAchievements_Users_UserId" FOREIGN KEY ("UserId") REFERENCES public."Users"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."UserLessonProgress"
    ADD CONSTRAINT "FK_UserLessonProgress_NutritionLessons_LessonId" FOREIGN KEY ("LessonId") REFERENCES public."NutritionLessons"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."UserLessonProgress"
    ADD CONSTRAINT "FK_UserLessonProgress_Users_UserId" FOREIGN KEY ("UserId") REFERENCES public."Users"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."UserLoginEvents"
    ADD CONSTRAINT "FK_UserLoginEvents_Users_UserId" FOREIGN KEY ("UserId") REFERENCES public."Users"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."UserNutritionProfiles"
    ADD CONSTRAINT "FK_UserNutritionProfiles_Users_Id" FOREIGN KEY ("Id") REFERENCES public."Users"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."UserPreferences"
    ADD CONSTRAINT "FK_UserPreferences_Users_Id" FOREIGN KEY ("Id") REFERENCES public."Users"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."UserRefreshTokenSessions"
    ADD CONSTRAINT "FK_UserRefreshTokenSessions_Users_UserId" FOREIGN KEY ("UserId") REFERENCES public."Users"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."UserRoleAuditEvents"
    ADD CONSTRAINT "FK_UserRoleAuditEvents_Users_ActorUserId" FOREIGN KEY ("ActorUserId") REFERENCES public."Users"("Id") ON DELETE SET NULL;

ALTER TABLE ONLY public."UserRoleAuditEvents"
    ADD CONSTRAINT "FK_UserRoleAuditEvents_Users_UserId" FOREIGN KEY ("UserId") REFERENCES public."Users"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."UserRoles"
    ADD CONSTRAINT "FK_UserRoles_Roles_RoleId" FOREIGN KEY ("RoleId") REFERENCES public."Roles"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."UserRoles"
    ADD CONSTRAINT "FK_UserRoles_Users_UserId" FOREIGN KEY ("UserId") REFERENCES public."Users"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."Users"
    ADD CONSTRAINT "FK_Users_ImageAssets_ProfileImageAssetId" FOREIGN KEY ("ProfileImageAssetId") REFERENCES public."ImageAssets"("Id");

ALTER TABLE ONLY public."WaistEntries"
    ADD CONSTRAINT "FK_WaistEntries_Users_UserId" FOREIGN KEY ("UserId") REFERENCES public."Users"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."WaistGoals"
    ADD CONSTRAINT "FK_WaistGoals_Users_UserId" FOREIGN KEY ("UserId") REFERENCES public."Users"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."WearableConnections"
    ADD CONSTRAINT "FK_WearableConnections_Users_UserId" FOREIGN KEY ("UserId") REFERENCES public."Users"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."WearableSyncEntries"
    ADD CONSTRAINT "FK_WearableSyncEntries_Users_UserId" FOREIGN KEY ("UserId") REFERENCES public."Users"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."WebPushSubscriptions"
    ADD CONSTRAINT "FK_WebPushSubscriptions_Users_UserId" FOREIGN KEY ("UserId") REFERENCES public."Users"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."WeeklyGoals"
    ADD CONSTRAINT "FK_WeeklyGoals_Users_UserId" FOREIGN KEY ("UserId") REFERENCES public."Users"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."WeightEntries"
    ADD CONSTRAINT "FK_WeightEntries_Users_UserId" FOREIGN KEY ("UserId") REFERENCES public."Users"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."WeightGoals"
    ADD CONSTRAINT "FK_WeightGoals_Users_UserId" FOREIGN KEY ("UserId") REFERENCES public."Users"("Id") ON DELETE CASCADE;

INSERT INTO public."AchievementDefinitions" ("Id", "Key", "Category", "Metric", "Threshold", "TitleRu", "TitleEn", "DescriptionRu", "DescriptionEn", "Icon", "SortOrder", "IsActive", "Version", "CreatedOnUtc", "ModifiedOnUtc") VALUES (E'10000000-0000-0000-0000-000000000003', E'streak_3', E'streak', E'LongestStreak', 3, E'Серия 3 дня', E'3-day streak', E'Отличный шаг к устойчивой привычке.', E'A strong first step toward a lasting habit.', E'local_fire_department', 10, TRUE, 1, E'2026-10-17T13:01:49.009871+00:00', NULL);
INSERT INTO public."AchievementDefinitions" ("Id", "Key", "Category", "Metric", "Threshold", "TitleRu", "TitleEn", "DescriptionRu", "DescriptionEn", "Icon", "SortOrder", "IsActive", "Version", "CreatedOnUtc", "ModifiedOnUtc") VALUES (E'10000000-0000-0000-0000-000000000007', E'streak_7', E'streak', E'LongestStreak', 7, E'Серия 7 дней', E'7-day streak', E'Неделя регулярного ведения дневника.', E'A full week of consistent tracking.', E'local_fire_department', 20, TRUE, 1, E'2026-10-17T13:01:49.009871+00:00', NULL);
INSERT INTO public."AchievementDefinitions" ("Id", "Key", "Category", "Metric", "Threshold", "TitleRu", "TitleEn", "DescriptionRu", "DescriptionEn", "Icon", "SortOrder", "IsActive", "Version", "CreatedOnUtc", "ModifiedOnUtc") VALUES (E'10000000-0000-0000-0000-000000000014', E'streak_14', E'streak', E'LongestStreak', 14, E'Серия 14 дней', E'14-day streak', E'Две недели устойчивой привычки.', E'Two weeks of a lasting habit.', E'local_fire_department', 30, TRUE, 1, E'2026-10-17T13:01:49.009871+00:00', NULL);
INSERT INTO public."AchievementDefinitions" ("Id", "Key", "Category", "Metric", "Threshold", "TitleRu", "TitleEn", "DescriptionRu", "DescriptionEn", "Icon", "SortOrder", "IsActive", "Version", "CreatedOnUtc", "ModifiedOnUtc") VALUES (E'10000000-0000-0000-0000-000000000030', E'streak_30', E'streak', E'LongestStreak', 30, E'Серия 30 дней', E'30-day streak', E'Месяц последовательного прогресса.', E'A month of consistent progress.', E'local_fire_department', 40, TRUE, 1, E'2026-10-17T13:01:49.009871+00:00', NULL);
INSERT INTO public."AchievementDefinitions" ("Id", "Key", "Category", "Metric", "Threshold", "TitleRu", "TitleEn", "DescriptionRu", "DescriptionEn", "Icon", "SortOrder", "IsActive", "Version", "CreatedOnUtc", "ModifiedOnUtc") VALUES (E'10000000-0000-0000-0000-000000000060', E'streak_60', E'streak', E'LongestStreak', 60, E'Серия 60 дней', E'60-day streak', E'Привычка стала частью образа жизни.', E'The habit has become part of your lifestyle.', E'local_fire_department', 50, TRUE, 1, E'2026-10-17T13:01:49.009871+00:00', NULL);
INSERT INTO public."AchievementDefinitions" ("Id", "Key", "Category", "Metric", "Threshold", "TitleRu", "TitleEn", "DescriptionRu", "DescriptionEn", "Icon", "SortOrder", "IsActive", "Version", "CreatedOnUtc", "ModifiedOnUtc") VALUES (E'10000000-0000-0000-0000-000000000100', E'streak_100', E'streak', E'LongestStreak', 100, E'Серия 100 дней', E'100-day streak', E'Сто дней осознанного питания.', E'One hundred days of mindful nutrition.', E'local_fire_department', 60, TRUE, 1, E'2026-10-17T13:01:49.009871+00:00', NULL);
INSERT INTO public."AchievementDefinitions" ("Id", "Key", "Category", "Metric", "Threshold", "TitleRu", "TitleEn", "DescriptionRu", "DescriptionEn", "Icon", "SortOrder", "IsActive", "Version", "CreatedOnUtc", "ModifiedOnUtc") VALUES (E'20000000-0000-0000-0000-000000000010', E'meals_10', E'meals', E'TotalMeals', 10, E'10 приёмов', E'10 meals', E'Первые десять записей в дневнике.', E'Your first ten diary entries.', E'restaurant', 70, TRUE, 1, E'2026-10-17T13:01:49.009871+00:00', NULL);
INSERT INTO public."AchievementDefinitions" ("Id", "Key", "Category", "Metric", "Threshold", "TitleRu", "TitleEn", "DescriptionRu", "DescriptionEn", "Icon", "SortOrder", "IsActive", "Version", "CreatedOnUtc", "ModifiedOnUtc") VALUES (E'20000000-0000-0000-0000-000000000050', E'meals_50', E'meals', E'TotalMeals', 50, E'50 приёмов', E'50 meals', E'Регулярность набирает силу.', E'Consistency is gaining momentum.', E'restaurant', 80, TRUE, 1, E'2026-10-17T13:01:49.009871+00:00', NULL);
INSERT INTO public."AchievementDefinitions" ("Id", "Key", "Category", "Metric", "Threshold", "TitleRu", "TitleEn", "DescriptionRu", "DescriptionEn", "Icon", "SortOrder", "IsActive", "Version", "CreatedOnUtc", "ModifiedOnUtc") VALUES (E'20000000-0000-0000-0000-000000000100', E'meals_100', E'meals', E'TotalMeals', 100, E'100 приёмов', E'100 meals', E'Сотня осознанных записей.', E'One hundred mindful entries.', E'restaurant', 90, TRUE, 1, E'2026-10-17T13:01:49.009871+00:00', NULL);
INSERT INTO public."AchievementDefinitions" ("Id", "Key", "Category", "Metric", "Threshold", "TitleRu", "TitleEn", "DescriptionRu", "DescriptionEn", "Icon", "SortOrder", "IsActive", "Version", "CreatedOnUtc", "ModifiedOnUtc") VALUES (E'20000000-0000-0000-0000-000000000500', E'meals_500', E'meals', E'TotalMeals', 500, E'500 приёмов', E'500 meals', E'Настоящее мастерство ведения дневника.', E'True diary tracking mastery.', E'restaurant', 100, TRUE, 1, E'2026-10-17T13:01:49.009871+00:00', NULL);
INSERT INTO public."AchievementDefinitions" ("Id", "Key", "Category", "Metric", "Threshold", "TitleRu", "TitleEn", "DescriptionRu", "DescriptionEn", "Icon", "SortOrder", "IsActive", "Version", "CreatedOnUtc", "ModifiedOnUtc") VALUES (E'30000000-0000-0000-0000-000000000001', E'academy_articles_1', E'academy', E'TotalAcademyArticlesRead', 1, E'Первая статья', E'First article', E'Первый шаг к более осознанному питанию.', E'Your first step toward more mindful nutrition.', E'school', 110, TRUE, 1, E'2026-10-17T13:01:49.568551+00:00', NULL);
INSERT INTO public."AchievementDefinitions" ("Id", "Key", "Category", "Metric", "Threshold", "TitleRu", "TitleEn", "DescriptionRu", "DescriptionEn", "Icon", "SortOrder", "IsActive", "Version", "CreatedOnUtc", "ModifiedOnUtc") VALUES (E'30000000-0000-0000-0000-000000000005', E'academy_articles_5', E'academy', E'TotalAcademyArticlesRead', 5, E'Любознательный читатель', E'Curious reader', E'Пять статей Академии уже прочитано.', E'You have read five Academy articles.', E'school', 120, TRUE, 1, E'2026-10-17T13:01:49.568551+00:00', NULL);
INSERT INTO public."AchievementDefinitions" ("Id", "Key", "Category", "Metric", "Threshold", "TitleRu", "TitleEn", "DescriptionRu", "DescriptionEn", "Icon", "SortOrder", "IsActive", "Version", "CreatedOnUtc", "ModifiedOnUtc") VALUES (E'30000000-0000-0000-0000-000000000010', E'academy_articles_10', E'academy', E'TotalAcademyArticlesRead', 10, E'Знаток питания', E'Nutrition scholar', E'Десять статей превращают знания в привычку.', E'Ten articles are turning knowledge into habit.', E'school', 130, TRUE, 1, E'2026-10-17T13:01:49.568551+00:00', NULL);
INSERT INTO public."AchievementDefinitions" ("Id", "Key", "Category", "Metric", "Threshold", "TitleRu", "TitleEn", "DescriptionRu", "DescriptionEn", "Icon", "SortOrder", "IsActive", "Version", "CreatedOnUtc", "ModifiedOnUtc") VALUES (E'30000000-0000-0000-0000-000000000025', E'academy_articles_25', E'academy', E'TotalAcademyArticlesRead', 25, E'Выпускник Академии', E'Academy graduate', E'Серьёзная база знаний о питании собрана.', E'You have built a strong foundation of nutrition knowledge.', E'school', 140, TRUE, 1, E'2026-10-17T13:01:49.568551+00:00', NULL);
INSERT INTO public."DailyAdvices" ("Id", "Value", "Locale", "Weight", "Tag", "CreatedOnUtc", "ModifiedOnUtc", "GroupId") VALUES (E'fce76c5a-3066-46aa-9193-3c8cbdb06f50', E'Start your day with a glass of water before coffee.', E'en', 3, E'hydration', E'2025-12-12T00:00:00+00:00', NULL, E'f14a9d08-4ffb-4ac0-9625-b9feef883c17');
INSERT INTO public."DailyAdvices" ("Id", "Value", "Locale", "Weight", "Tag", "CreatedOnUtc", "ModifiedOnUtc", "GroupId") VALUES (E'bd3f9b4a-3a42-44fb-ac3b-6c5ccda5beaf', E'Add a protein source to your breakfast to stay full longer.', E'en', 3, E'protein', E'2025-12-12T00:00:00+00:00', NULL, E'f21af817-08f2-45a3-b8fa-178b2e20b758');
INSERT INTO public."DailyAdvices" ("Id", "Value", "Locale", "Weight", "Tag", "CreatedOnUtc", "ModifiedOnUtc", "GroupId") VALUES (E'6a3ecbbf-4706-4e0c-bd8b-328f96ed58a1', E'Keep chopped veggies or berries ready for quick snacks.', E'en', 2, E'prep', E'2025-12-12T00:00:00+00:00', NULL, E'ad67eb46-efe6-4036-9d4a-d70f51b3dfd9');
INSERT INTO public."DailyAdvices" ("Id", "Value", "Locale", "Weight", "Tag", "CreatedOnUtc", "ModifiedOnUtc", "GroupId") VALUES (E'9e3ee3be-9fcb-4de1-8ee4-17663fd42de2', E'Plan a 10-minute walk after meals to stabilize energy.', E'en', 2, E'movement', E'2025-12-12T00:00:00+00:00', NULL, E'7b44b3c8-f1d7-47e7-9a17-3fb0f6e11364');
INSERT INTO public."DailyAdvices" ("Id", "Value", "Locale", "Weight", "Tag", "CreatedOnUtc", "ModifiedOnUtc", "GroupId") VALUES (E'1bb2b64a-d13c-439f-9909-cc038bd74fa8', E'Pre-log dinner to stay within your calorie target.', E'en', 1, E'planning', E'2025-12-12T00:00:00+00:00', NULL, E'0d267dca-a231-4e92-9fbf-e442d22fdc1c');
INSERT INTO public."DailyAdvices" ("Id", "Value", "Locale", "Weight", "Tag", "CreatedOnUtc", "ModifiedOnUtc", "GroupId") VALUES (E'78c1d056-e4bd-4236-9e76-6aeff024927c', E'Начните день со стакана воды до кофе.', E'ru', 3, E'hydration', E'2025-12-12T00:00:00+00:00', NULL, E'b6bc990d-2504-4095-b250-ed447e256b02');
INSERT INTO public."DailyAdvices" ("Id", "Value", "Locale", "Weight", "Tag", "CreatedOnUtc", "ModifiedOnUtc", "GroupId") VALUES (E'd7e3b08a-93dd-4f35-8e0c-4df445f64ae8', E'Добавьте источник белка к завтраку, чтобы дольше держать сытость.', E'ru', 3, E'protein', E'2025-12-12T00:00:00+00:00', NULL, E'31b52e2a-7823-4f1c-8d48-61f3765d3618');
INSERT INTO public."DailyAdvices" ("Id", "Value", "Locale", "Weight", "Tag", "CreatedOnUtc", "ModifiedOnUtc", "GroupId") VALUES (E'c887d6e8-7d8f-4a91-9af8-9b777787a8f2', E'Держите нарезанные овощи или ягоды под рукой для быстрых перекусов.', E'ru', 2, E'prep', E'2025-12-12T00:00:00+00:00', NULL, E'6137ea0a-0d78-40e0-b529-1633b9048389');
INSERT INTO public."DailyAdvices" ("Id", "Value", "Locale", "Weight", "Tag", "CreatedOnUtc", "ModifiedOnUtc", "GroupId") VALUES (E'ba59c34b-282f-41bb-9e5e-91debf9cc5ee', E'Запланируйте 10-минутную прогулку после еды, чтобы стабилизировать энергию.', E'ru', 2, E'movement', E'2025-12-12T00:00:00+00:00', NULL, E'5a856f2f-871d-4ef5-b063-18826a590a4f');
INSERT INTO public."DailyAdvices" ("Id", "Value", "Locale", "Weight", "Tag", "CreatedOnUtc", "ModifiedOnUtc", "GroupId") VALUES (E'7c26d24d-7df2-4553-8c09-c22b7a9c780b', E'Заранее занесите ужин в дневник, чтобы уложиться в калораж.', E'ru', 1, E'planning', E'2025-12-12T00:00:00+00:00', NULL, E'fe4e45c7-56a4-473d-a433-a90f34d882a9');
INSERT INTO public."EmailTemplates" ("Id", "Key", "Locale", "Subject", "HtmlBody", "TextBody", "IsActive", "CreatedOnUtc", "ModifiedOnUtc") VALUES (E'5f7c78b6-051b-4864-af88-c9fc023a86b1', E'account_created', E'en', E'Your {{brand}} account is ready', E'<h1>Your account is ready</h1><p>An account has been created for you.</p><p><strong>Email:</strong> {{email}}<br><strong>Temporary password:</strong> <code>{{temporaryPassword}}</code></p><p><a href="{{loginLink}}">Sign in</a></p><p>You will be asked to choose a new password after signing in.</p>', E'Your account is ready.\n\nEmail: {{email}}\nTemporary password: {{temporaryPassword}}\nSign in: {{loginLink}}\n\nYou will be asked to choose a new password after signing in.', TRUE, E'2026-10-17T13:01:46.002332+00:00', E'2026-10-17T13:01:46.002332+00:00');
INSERT INTO public."EmailTemplates" ("Id", "Key", "Locale", "Subject", "HtmlBody", "TextBody", "IsActive", "CreatedOnUtc", "ModifiedOnUtc") VALUES (E'904493fd-f261-4f5b-8a11-af61cdb14452', E'account_created', E'ru', E'Ваш аккаунт {{brand}} готов', E'<h1>Ваш аккаунт готов</h1><p>Для вас создан аккаунт.</p><p><strong>Email:</strong> {{email}}<br><strong>Временный пароль:</strong> <code>{{temporaryPassword}}</code></p><p><a href="{{loginLink}}">Войти</a></p><p>После входа потребуется установить новый пароль.</p>', E'Ваш аккаунт готов.\n\nEmail: {{email}}\nВременный пароль: {{temporaryPassword}}\nВойти: {{loginLink}}\n\nПосле входа потребуется установить новый пароль.', TRUE, E'2026-10-17T13:01:46.002332+00:00', E'2026-10-17T13:01:46.002332+00:00');
INSERT INTO public."EmailTemplates" ("Id", "Key", "Locale", "Subject", "HtmlBody", "TextBody", "IsActive", "CreatedOnUtc", "ModifiedOnUtc") VALUES (E'331bcfa0-d0e3-46c9-a732-4e88493fa9f0', E'bug_report_received', E'en', E'We received your bug report', E'<p>Thank you for helping improve {{brand}}!</p><p>We received your report and will look into it. If we need more details, we will reply in this conversation.</p><p>The {{brand}} team</p>', E'Thank you for helping improve {{brand}}! We received your report and will look into it. If we need more details, we will reply in this conversation. The {{brand}} team', TRUE, E'2026-10-17T13:01:53.760332+00:00', NULL);
INSERT INTO public."EmailTemplates" ("Id", "Key", "Locale", "Subject", "HtmlBody", "TextBody", "IsActive", "CreatedOnUtc", "ModifiedOnUtc") VALUES (E'331bcfa0-d0e3-46c9-a732-4e88493fa9f1', E'bug_report_received', E'ru', E'Получили ваше сообщение об ошибке', E'<p>Спасибо, что помогаете улучшать {{brand}}!</p><p>Мы получили ваше сообщение и изучим его. Если понадобятся подробности, ответим в этой переписке.</p><p>Команда {{brand}}</p>', E'Спасибо, что помогаете улучшать {{brand}}! Мы получили ваше сообщение и изучим его. Если понадобятся подробности, ответим в этой переписке. Команда {{brand}}', TRUE, E'2026-10-17T13:01:53.760332+00:00', NULL);
INSERT INTO public."EmailTemplates" ("Id", "Key", "Locale", "Subject", "HtmlBody", "TextBody", "IsActive", "CreatedOnUtc", "ModifiedOnUtc") VALUES (E'9f5d2a1b-2d2d-4f12-9c4f-3a0bd0d4e6c1', E'email_verification', E'en', E'Confirm your email', E'<!doctype html>\n<html lang="en">\n  <head>\n    <meta charset="UTF-8">\n    <meta name="viewport" content="width=device-width, initial-scale=1.0">\n    <title>Confirm your email</title>\n  </head>\n  <body style="margin:0;padding:0;background-color:#f4f6fb;">\n    <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="background-color:#f4f6fb;padding:32px 16px;">\n      <tr>\n        <td align="center">\n          <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="max-width:600px;background:#ffffff;border-radius:16px;box-shadow:0 12px 30px rgba(15,23,42,0.12);overflow:hidden;">\n            <tr>\n              <td style="padding:24px 28px;background:#101827;color:#ffffff;font-family:Segoe UI,Arial,sans-serif;font-size:18px;font-weight:600;">\n                {{brand}}\n              </td>\n            </tr>\n            <tr>\n              <td style="padding:28px;font-family:Segoe UI,Arial,sans-serif;color:#0f172a;">\n                <h1 style="margin:0 0 12px;font-size:22px;line-height:1.3;">Confirm your email</h1>\n                <p style="margin:0 0 20px;font-size:15px;line-height:1.6;color:#475569;">Thanks for registering in {{brand}}.</p>\n                <table role="presentation" cellspacing="0" cellpadding="0">\n                  <tr>\n                    <td style="border-radius:10px;background:#2563eb;">\n                      <a href="{{link}}" style="display:inline-block;padding:12px 20px;font-size:15px;color:#ffffff;text-decoration:none;font-weight:600;">\n                        Confirm email\n                      </a>\n                    </td>\n                  </tr>\n                </table>\n                <p style="margin:20px 0 0;font-size:13px;line-height:1.6;color:#64748b;">If you did not request this, you can ignore this email.</p>\n              </td>\n            </tr>\n            <tr>\n              <td style="padding:16px 28px;background:#f8fafc;color:#64748b;font-family:Segoe UI,Arial,sans-serif;font-size:12px;">\n                If the button doesn''t work, copy and paste this link into your browser:<br>\n                <span style="word-break:break-all;color:#64748b;">{{link}}</span>\n              </td>\n            </tr>\n          </table>\n        </td>\n      </tr>\n    </table>\n  </body>\n</html>', E'Thanks for registering in {{brand}}.\nPlease confirm your email: {{link}}\nIf you did not request this, you can ignore this email.', TRUE, E'2026-02-08T00:00:00+00:00', NULL);
INSERT INTO public."EmailTemplates" ("Id", "Key", "Locale", "Subject", "HtmlBody", "TextBody", "IsActive", "CreatedOnUtc", "ModifiedOnUtc") VALUES (E'3b8b6b24-7e11-4e0a-9f94-93b1a2a2a1b6', E'email_verification', E'ru', E'Подтвердите email', E'<!doctype html>\n<html lang="ru">\n  <head>\n    <meta charset="UTF-8">\n    <meta name="viewport" content="width=device-width, initial-scale=1.0">\n    <title>Подтвердите email</title>\n  </head>\n  <body style="margin:0;padding:0;background-color:#f4f6fb;">\n    <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="background-color:#f4f6fb;padding:32px 16px;">\n      <tr>\n        <td align="center">\n          <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="max-width:600px;background:#ffffff;border-radius:16px;box-shadow:0 12px 30px rgba(15,23,42,0.12);overflow:hidden;">\n            <tr>\n              <td style="padding:24px 28px;background:#101827;color:#ffffff;font-family:Segoe UI,Arial,sans-serif;font-size:18px;font-weight:600;">\n                {{brand}}\n              </td>\n            </tr>\n            <tr>\n              <td style="padding:28px;font-family:Segoe UI,Arial,sans-serif;color:#0f172a;">\n                <h1 style="margin:0 0 12px;font-size:22px;line-height:1.3;">Подтвердите email</h1>\n                <p style="margin:0 0 20px;font-size:15px;line-height:1.6;color:#475569;">Спасибо за регистрацию в {{brand}}.</p>\n                <table role="presentation" cellspacing="0" cellpadding="0">\n                  <tr>\n                    <td style="border-radius:10px;background:#2563eb;">\n                      <a href="{{link}}" style="display:inline-block;padding:12px 20px;font-size:15px;color:#ffffff;text-decoration:none;font-weight:600;">\n                        Подтвердить email\n                      </a>\n                    </td>\n                  </tr>\n                </table>\n                <p style="margin:20px 0 0;font-size:13px;line-height:1.6;color:#64748b;">Если вы не запрашивали это, просто проигнорируйте письмо.</p>\n              </td>\n            </tr>\n            <tr>\n              <td style="padding:16px 28px;background:#f8fafc;color:#64748b;font-family:Segoe UI,Arial,sans-serif;font-size:12px;">\n                Если кнопка не работает, скопируйте ссылку в браузер:<br>\n                <span style="word-break:break-all;color:#64748b;">{{link}}</span>\n              </td>\n            </tr>\n          </table>\n        </td>\n      </tr>\n    </table>\n  </body>\n</html>', E'Спасибо за регистрацию в {{brand}}.\nПодтвердите email: {{link}}\nЕсли вы не запрашивали это, просто проигнорируйте письмо.', TRUE, E'2026-02-08T00:00:00+00:00', NULL);
INSERT INTO public."EmailTemplates" ("Id", "Key", "Locale", "Subject", "HtmlBody", "TextBody", "IsActive", "CreatedOnUtc", "ModifiedOnUtc") VALUES (E'bdfd6e52-9b7b-4e8a-9f64-6c0c78a0d4fa', E'password_reset', E'en', E'Reset your password', E'<!doctype html>\n<html lang="en">\n  <head>\n    <meta charset="UTF-8">\n    <meta name="viewport" content="width=device-width, initial-scale=1.0">\n    <title>Reset your password</title>\n  </head>\n  <body style="margin:0;padding:0;background-color:#f4f6fb;">\n    <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="background-color:#f4f6fb;padding:32px 16px;">\n      <tr>\n        <td align="center">\n          <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="max-width:600px;background:#ffffff;border-radius:16px;box-shadow:0 12px 30px rgba(15,23,42,0.12);overflow:hidden;">\n            <tr>\n              <td style="padding:24px 28px;background:#101827;color:#ffffff;font-family:Segoe UI,Arial,sans-serif;font-size:18px;font-weight:600;">\n                {{brand}}\n              </td>\n            </tr>\n            <tr>\n              <td style="padding:28px;font-family:Segoe UI,Arial,sans-serif;color:#0f172a;">\n                <h1 style="margin:0 0 12px;font-size:22px;line-height:1.3;">Reset your password</h1>\n                <p style="margin:0 0 20px;font-size:15px;line-height:1.6;color:#475569;">We received a request to reset your {{brand}} password.</p>\n                <table role="presentation" cellspacing="0" cellpadding="0">\n                  <tr>\n                    <td style="border-radius:10px;background:#2563eb;">\n                      <a href="{{link}}" style="display:inline-block;padding:12px 20px;font-size:15px;color:#ffffff;text-decoration:none;font-weight:600;">\n                        Reset password\n                      </a>\n                    </td>\n                  </tr>\n                </table>\n                <p style="margin:20px 0 0;font-size:13px;line-height:1.6;color:#64748b;">If you did not request this, you can ignore this email.</p>\n              </td>\n            </tr>\n            <tr>\n              <td style="padding:16px 28px;background:#f8fafc;color:#64748b;font-family:Segoe UI,Arial,sans-serif;font-size:12px;">\n                If the button doesn''t work, copy and paste this link into your browser:<br>\n                <span style="word-break:break-all;color:#64748b;">{{link}}</span>\n              </td>\n            </tr>\n          </table>\n        </td>\n      </tr>\n    </table>\n  </body>\n</html>', E'We received a request to reset your {{brand}} password.\nReset your password: {{link}}\nIf you did not request this, you can ignore this email.', TRUE, E'2026-02-08T00:00:00+00:00', NULL);
INSERT INTO public."EmailTemplates" ("Id", "Key", "Locale", "Subject", "HtmlBody", "TextBody", "IsActive", "CreatedOnUtc", "ModifiedOnUtc") VALUES (E'c3b2a7b1-6c91-4d6b-82b3-4f9aaf0c0b3f', E'password_reset', E'ru', E'Сброс пароля', E'<!doctype html>\n<html lang="ru">\n  <head>\n    <meta charset="UTF-8">\n    <meta name="viewport" content="width=device-width, initial-scale=1.0">\n    <title>Сброс пароля</title>\n  </head>\n  <body style="margin:0;padding:0;background-color:#f4f6fb;">\n    <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="background-color:#f4f6fb;padding:32px 16px;">\n      <tr>\n        <td align="center">\n          <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="max-width:600px;background:#ffffff;border-radius:16px;box-shadow:0 12px 30px rgba(15,23,42,0.12);overflow:hidden;">\n            <tr>\n              <td style="padding:24px 28px;background:#101827;color:#ffffff;font-family:Segoe UI,Arial,sans-serif;font-size:18px;font-weight:600;">\n                {{brand}}\n              </td>\n            </tr>\n            <tr>\n              <td style="padding:28px;font-family:Segoe UI,Arial,sans-serif;color:#0f172a;">\n                <h1 style="margin:0 0 12px;font-size:22px;line-height:1.3;">Сброс пароля</h1>\n                <p style="margin:0 0 20px;font-size:15px;line-height:1.6;color:#475569;">Мы получили запрос на смену пароля {{brand}}.</p>\n                <table role="presentation" cellspacing="0" cellpadding="0">\n                  <tr>\n                    <td style="border-radius:10px;background:#2563eb;">\n                      <a href="{{link}}" style="display:inline-block;padding:12px 20px;font-size:15px;color:#ffffff;text-decoration:none;font-weight:600;">\n                        Сбросить пароль\n                      </a>\n                    </td>\n                  </tr>\n                </table>\n                <p style="margin:20px 0 0;font-size:13px;line-height:1.6;color:#64748b;">Если вы не запрашивали это, просто проигнорируйте письмо.</p>\n              </td>\n            </tr>\n            <tr>\n              <td style="padding:16px 28px;background:#f8fafc;color:#64748b;font-family:Segoe UI,Arial,sans-serif;font-size:12px;">\n                Если кнопка не работает, скопируйте ссылку в браузер:<br>\n                <span style="word-break:break-all;color:#64748b;">{{link}}</span>\n              </td>\n            </tr>\n          </table>\n        </td>\n      </tr>\n    </table>\n  </body>\n</html>', E'Мы получили запрос на смену пароля {{brand}}.\nСбросить пароль: {{link}}\nЕсли вы не запрашивали это, просто проигнорируйте письмо.', TRUE, E'2026-02-08T00:00:00+00:00', NULL);
INSERT INTO public."EmailTemplates" ("Id", "Key", "Locale", "Subject", "HtmlBody", "TextBody", "IsActive", "CreatedOnUtc", "ModifiedOnUtc") VALUES (E'70d06c63-b046-4bc0-89be-5636d5d030ef', E'dietologist_invitation', E'en', E'Invitation to become a dietologist', E'<!doctype html>\n<html lang="en">\n  <head>\n    <meta charset="UTF-8">\n    <meta name="viewport" content="width=device-width, initial-scale=1.0">\n    <title>Invitation to become a dietologist</title>\n  </head>\n  <body style="margin:0;padding:0;background-color:#f4f6fb;">\n    <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="background-color:#f4f6fb;padding:32px 16px;">\n      <tr>\n        <td align="center">\n          <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="max-width:600px;background:#ffffff;border-radius:16px;box-shadow:0 12px 30px rgba(15,23,42,0.12);overflow:hidden;">\n            <tr>\n              <td style="padding:24px 28px;background:#101827;color:#ffffff;font-family:Segoe UI,Arial,sans-serif;font-size:18px;font-weight:600;">\n                {{brand}}\n              </td>\n            </tr>\n            <tr>\n              <td style="padding:28px;font-family:Segoe UI,Arial,sans-serif;color:#0f172a;">\n                <h1 style="margin:0 0 12px;font-size:22px;line-height:1.3;">Invitation to become a dietologist</h1>\n                <p style="margin:0 0 20px;font-size:15px;line-height:1.6;color:#475569;">{{clientName}} has invited you to become their dietologist on {{brand}}. Click the button below to accept the invitation.</p>\n                <table role="presentation" cellspacing="0" cellpadding="0">\n                  <tr>\n                    <td style="border-radius:10px;background:#2563eb;">\n                      <a href="{{link}}" style="display:inline-block;padding:12px 20px;font-size:15px;color:#ffffff;text-decoration:none;font-weight:600;">\n                        Accept Invitation\n                      </a>\n                    </td>\n                  </tr>\n                </table>\n                <p style="margin:20px 0 0;font-size:13px;line-height:1.6;color:#64748b;">If you did not expect this invitation, you can ignore this email.</p>\n              </td>\n            </tr>\n            <tr>\n              <td style="padding:16px 28px;background:#f8fafc;color:#64748b;font-family:Segoe UI,Arial,sans-serif;font-size:12px;">\n                If the button doesn''t work, copy and paste this link into your browser:<br>\n                <span style="word-break:break-all;color:#64748b;">{{link}}</span>\n              </td>\n            </tr>\n          </table>\n        </td>\n      </tr>\n    </table>\n  </body>\n</html>', E'{{clientName}} has invited you to become their dietologist on {{brand}}.\nAccept Invitation: {{link}}\nIf you did not expect this invitation, you can ignore this email.', TRUE, E'2026-04-26T00:00:00+00:00', NULL);
INSERT INTO public."EmailTemplates" ("Id", "Key", "Locale", "Subject", "HtmlBody", "TextBody", "IsActive", "CreatedOnUtc", "ModifiedOnUtc") VALUES (E'2ebcb2a1-368f-4191-970d-18353f959696', E'dietologist_invitation', E'ru', E'Приглашение стать диетологом', E'<!doctype html>\n<html lang="ru">\n  <head>\n    <meta charset="UTF-8">\n    <meta name="viewport" content="width=device-width, initial-scale=1.0">\n    <title>Приглашение стать диетологом</title>\n  </head>\n  <body style="margin:0;padding:0;background-color:#f4f6fb;">\n    <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="background-color:#f4f6fb;padding:32px 16px;">\n      <tr>\n        <td align="center">\n          <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="max-width:600px;background:#ffffff;border-radius:16px;box-shadow:0 12px 30px rgba(15,23,42,0.12);overflow:hidden;">\n            <tr>\n              <td style="padding:24px 28px;background:#101827;color:#ffffff;font-family:Segoe UI,Arial,sans-serif;font-size:18px;font-weight:600;">\n                {{brand}}\n              </td>\n            </tr>\n            <tr>\n              <td style="padding:28px;font-family:Segoe UI,Arial,sans-serif;color:#0f172a;">\n                <h1 style="margin:0 0 12px;font-size:22px;line-height:1.3;">Приглашение стать диетологом</h1>\n                <p style="margin:0 0 20px;font-size:15px;line-height:1.6;color:#475569;">{{clientName}} приглашает вас стать их диетологом в {{brand}}. Нажмите кнопку ниже, чтобы принять приглашение.</p>\n                <table role="presentation" cellspacing="0" cellpadding="0">\n                  <tr>\n                    <td style="border-radius:10px;background:#2563eb;">\n                      <a href="{{link}}" style="display:inline-block;padding:12px 20px;font-size:15px;color:#ffffff;text-decoration:none;font-weight:600;">\n                        Принять приглашение\n                      </a>\n                    </td>\n                  </tr>\n                </table>\n                <p style="margin:20px 0 0;font-size:13px;line-height:1.6;color:#64748b;">Если вы не ожидали это приглашение, просто проигнорируйте письмо.</p>\n              </td>\n            </tr>\n            <tr>\n              <td style="padding:16px 28px;background:#f8fafc;color:#64748b;font-family:Segoe UI,Arial,sans-serif;font-size:12px;">\n                Если кнопка не работает, скопируйте ссылку в браузер:<br>\n                <span style="word-break:break-all;color:#64748b;">{{link}}</span>\n              </td>\n            </tr>\n          </table>\n        </td>\n      </tr>\n    </table>\n  </body>\n</html>', E'{{clientName}} приглашает вас стать их диетологом в {{brand}}.\nПринять приглашение: {{link}}\nЕсли вы не ожидали это приглашение, просто проигнорируйте письмо.', TRUE, E'2026-04-26T00:00:00+00:00', NULL);
INSERT INTO public."Roles" ("Id", "Name", "CreatedOnUtc", "ModifiedOnUtc") VALUES (E'7bb7a1f7-9dfc-4e2a-9497-3b5b29d299a1', E'Admin', E'2026-10-17T13:01:30.532894+00:00', NULL);
INSERT INTO public."Roles" ("Id", "Name", "CreatedOnUtc", "ModifiedOnUtc") VALUES (E'e25d9e58-1c78-42f1-9f7c-9c3f6c4e6b2c', E'Premium', E'2026-10-17T13:01:30.532894+00:00', NULL);
INSERT INTO public."Roles" ("Id", "Name", "CreatedOnUtc", "ModifiedOnUtc") VALUES (E'b2fb6dfc-2e86-4a4b-9a20-2e8a5b8a2d9c', E'Support', E'2026-10-17T13:01:30.532894+00:00', NULL);
INSERT INTO public."Roles" ("Id", "Name", "CreatedOnUtc", "ModifiedOnUtc") VALUES (E'ca941103-6ade-4f6d-9747-34353cdb3a3d', E'Dietologist', E'2026-10-17T13:01:32.631936+00:00', NULL);
INSERT INTO public."Roles" ("Id", "Name", "CreatedOnUtc", "ModifiedOnUtc") VALUES (E'f8c47d75-cb3f-4267-9ec5-761dc6934211', E'Owner', E'2026-10-17T13:01:38.737958+00:00', NULL);
INSERT INTO public."TelegramAssertionPolicies" ("Id", "NotBeforeUtc") VALUES (1, E'2026-10-17T13:06:59+00:00');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20251108210736_InitialCreate', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20251109204427_MoveIngredientsToSteps', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20251109214619_UseStronglyTypedRecipeIds', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20251109223107_AddRecipeVisibility', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20251111000000_AddRecipeCustomNutrition', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20251112000000_AddMealNutritionTotals', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20251115022231_AddProductComment', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20251115114413_AddMealImageUrl', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20251115114728_AddMealImageUrlColumn', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20251115125446_AddProductType', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20251115161118_AddMealManualNutritionOverrides', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20251115204341_AddWeightEntries', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20251115211410_AddDesiredWeightToUsers', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20251116210133_AddMealSatietyLevels', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20251116213102_AddUserActivityLevel', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20251116214332_AddUserNutritionGoals', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20251117172742_AddDesiredWaistToUsers', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20251118220000_AddWaistEntries', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20251121193811_AddCycles', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20251123024500_UseStronglyTypedMealIds', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20251123040000_AddAdminUser', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20251201120000_AddFiberTargetToUsers', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20251201120100_AddProductDefaultPortionAmount', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20251201162433_AddImageAssets', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20251211040000_AddHydrationEntries', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20251212153151_AddDailyAdvices', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20251215230159_AddProfileImageAssetIdToUsers', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20251221180000_AddDashboardLayoutToUsers', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20251222190000_AddDeletedAtToUsers', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20251224123000_AddLanguageToUsers', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20251229140000_SetDefaultLanguageForUsers', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260131220000_AddTelegramUserIdToUsers', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260201130000_AddAlcoholNutrition', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260202120000_AddRecipeComment', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260202193000_AddRecipeStepTitle', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260202194500_AddRecipeStepImageAsset', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260204120000_AddUserRoles', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260205071704_AddAiUsageLog', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260205201519_AddMealAiSessions', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260206103506_AddMealAiItemNameFields', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260206224341_AddUserAiTokenLimits', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260206230536_AddMealAiSessionImageAssetRelation', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260207192305_AddEmailVerificationAndPasswordResetTokens', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260208012000_AddEmailConfirmationSentAtUtc', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260208160000_AddPasswordResetSentAtUtc', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260208173000_AddEmailTemplates', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260208180000_SeedEmailTemplatesAfterAddEmailTemplates', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260209005246_AddShoppingLists', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260210133700_AddUserLastLoginAtUtc', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260216055226_AddRecentItems', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260326172220_AddMissingUserForeignKeys', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260327222444_AddCompositeIndexOnAiUsage', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260328133626_AddPerformanceIndexes', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260329142412_AddXminConcurrencyTokens', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260402201954_AddTrigramSearchIndexes', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260404175028_AddDietologistFeature', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260404182816_AddFastingSessions', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260404235246_AddFavoriteMeals', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260405004851_AddCalorieCycling', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260405140402_AddMealPlans', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260405175126_AddExerciseEntries', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260405180825_AddNutritionLessons', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260405182910_AddAiPromptTemplates', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260405190811_AddUsdaReferenceData', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260405191536_AddUsdaFdcIdToProduct', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260405192406_AddDailyReferenceValues', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260405203147_AddWearableIntegrations', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260405212429_AddRecipeComments', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260405213916_AddRecipeLikesAndContentReports', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260407162127_AddAiConsentAcceptedAt', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260407183824_IncreaseNutritionLessonContentMaxLength', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260407223141_AddAiRecognitionSourceToMealAiSession', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260408221206_SplitFastingInitialAndAddedDuration', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260409081844_AddFastingPlansAndOccurrences', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260409173447_UseNotificationPayloadJson', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260410001235_AddWebPushSubscriptions', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260410133259_AddPushNotificationsPreferences', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260410212336_AddFastingCheckInFields', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260410215756_AddFastingReminderThresholds', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260410222425_AddFastingTelemetryEvents', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260412074648_AddFastingCheckIns', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260413161754_AddFavoriteProductsAndRecipes', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260415102230_AddDietologistProfileAndFastingPermissions', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260417123301_AddThemeToUsers', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260418130735_AddUiStyleToUsers', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260421105332_AddBillingSubscriptions', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260423215532_AddHasPasswordToUsers', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260426194739_SeedDietologistInvitationEmailTemplates', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260427223534_AddBillingProviderAuditTables', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260428002045_RemoveUserActivityLevelDefault', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260428045008_AddAdminImpersonationSessions', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260429220900_AddOwnerRole', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260501135727_AddOpenFoodFactsProductCache', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260502205541_ConvertMealSatietyToFivePointScale', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260505104851_AddUserLoginEvents', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260522223941_AddUserRoleAuditEvents', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260523153541_AddUserRoleAuditEventUserForeignKeys', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260524141931_AddUserPremiumTrial', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260524153000_ConfirmSeededAdminEmail', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260524213515_AddBillingPremiumRoleManagement', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260524223140_EnforceUniqueBillingPayments', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260606024116_AddUserRefreshTokenSessions', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260611052514_ReplaceCyclesWithCycleProfiles', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260611221750_RebuildShoppingListsForSourceAwareItems', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260611230530_AddFavoriteProductPreferredPortionAmount', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260612093115_AddMealAiReviewMetadata', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260612100641_AddMealItemSnapshots', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260612195402_AddPreviousRefreshTokenGrace', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260702134159_AddImageObjectDeletionOutbox', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260702135615_AddNotificationWebPushOutbox', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260704210408_AddEmailOutbox', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260704211656_AddOutboxMessageLeases', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260707100425_AddOutboxDeadLettering', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260709095339_AddMarketingAttributionEvents', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260718223616_AddGoogleExternalIdentity', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260718224628_AddTelegramAssertionReplayProtection', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260718224946_AddBillingWebhookOrdering', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260718230655_ExpandProtectedWearableTokenStorage', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260723105925_AddAdminCreatedUserCredentials', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260724073547_AddRecommendationComments', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260724080234_AddClientTasks', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260724082113_AddClientTaskDueReminders', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260724082934_AddRecommendationTemplatesAndBulkDispatches', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260724084439_AddCollaborationAudit', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260726042050_AddOutboxReplayAudit', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260805211801_AddWeightGoalLifecycle', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260806215516_AddWaistGoalLifecycle', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260808080859_AddBillingFinancialBreakdown', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260808094923_AddBillingWebhookInboxAndPaymentOccurrence', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260809193803_AddAchievementPersistence', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260809195626_AddManagedAchievementDefinitions', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260809205652_AddAchievementEvaluationOutbox', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260809223424_AddAcademyAchievementDefinitions', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260810221016_AddWeeklyGoals', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260811091452_RenameLegacyFastingProtocolValues', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260814220114_RenameBodyMeasurementColumnsForExplicitUnits', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260816013354_AddCycleMenstrualEpisodes', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260816222202_AddAiQuotaReservations', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260817023034_AddCyclePrivacyAndPredictionV2', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260817044216_AddMarketingAttributionUserTimelineIndex', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260819075214_AddWearableConnectIdempotencyFingerprint', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260822113751_HardenHydrationEntries', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260822145958_AllowMultipleHydrationEntriesAtSameTimestamp', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260823195705_AddUserSecurityVersion', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260823225043_HardenMarketingAttributionLifecycle', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260824000613_HardenContentReportModeration', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260824012021_CoalesceAchievementEvaluationOutbox', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260825004720_RemoveLegacyUserRefreshToken', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260825090908_ScrubDeadLetteredEmailPayloads', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260825142835_ConfirmImageUploads', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260829021931_AddAiAndFastingQueryIndexes', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260906005823_ProtectReferencedImageAssets', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260908131615_AddEmailPurposeAndBugAcknowledgement', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260908131755_AddBugAcknowledgementReceipts', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260908201844_AddLessonPublication', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260908203220_AddTemplateRevisionHistory', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260910220112_AddUserSurfaceStyle', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260911193014_AddFoodRecognitionJobs', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260912000929_AddTelegramOnlyAccounts', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260912002959_AddTelegramLoginTickets', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260912011417_AddTelegramOidcIdentity', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260912013245_AddTelegramOperations', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260912020828_AddMealRecognitionReceipts', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260912024547_AddHydrationOperationReceipts', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260914032832_ProtectAiPromptConcurrentUpdates', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260914190216_PreserveBillingPaymentPrecision', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260917010000_ConvertOtherMealsToSnack', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260918123537_GroupDailyAdviceTranslations', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260922155631_PreserveRemovedMealFavorites', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260924193550_AddProductLabelRecognitionImages', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260925063206_AddProductGallery', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260926090052_AddRecipeGallery', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260926113443_AddRecipeStepGallery', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260926141133_AddRecipeTextIngredients', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260926163506_PreserveRecipeIngredientOrder', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260927150611_AddRecipeLanguage', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260927200359_AddFixedRecipeCategories', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260928205829_AddRecipePublicIngredientDescriptions', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260928220000_BackfillPublicRecipeProductDescriptions', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20260930223014_AddCycleDayNotes', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20261003001656_AddDurableAtomicCommandReceiptsAndPushDeliveryProgress', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20261003003938_SplitUserPreferencesAndNutritionProfile', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20261006060809_ImproveDefaultEmailTemplateContrast', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20261006124036_AllowLiveUsdaFoodReferences', E'10.0.11');
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (E'20261017003851_HardenTelegramAssertionRecovery', E'10.0.11');
SET search_path = public;
