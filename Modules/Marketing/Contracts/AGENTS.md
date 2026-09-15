# Marketing consumer contracts

Own GetMarketingAttributionSummaryQuery and its immutable response models consumed by Admin Presentation, plus RecordPremiumConversionCommand consumed by Billing. The conversion command participates in the caller unit of work and does not commit independently. Use canonical project and folder namespaces and preserve signatures. Handlers, validators, authorization and persistence remain with their existing owners. This project exports no aggregate or repository capability. Rebuild consumers together after assembly relocation.
