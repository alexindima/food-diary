# ADR 0021: Extract ContentReports as a logical module

ContentReports-owned application, contracts, domain, EF model, repository adapter, and unit tests live under `Modules/ContentReports`. The legacy `FoodDiary.Application.ContentReports` assembly name and CLR namespaces, EF identity/mapping, HTTP routes, authorization, and central migrations remain stable. Admin depends only on module Contracts; hosts compose module Infrastructure.
