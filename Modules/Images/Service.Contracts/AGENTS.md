# Images service contracts

Expose immutable owner-validated image read projections and consumer capabilities.
Keep this package free of aggregates, repositories, EF and use-case handlers.
Own IImageAssetCleanupService, IImageAssetOwnershipService and DeleteImageAssetResult
as public semantic capabilities. Preserve cleanup, reassignment and transaction
behavior in their existing owner implementations; moving the ports grants no
direct repository access to consumers.
The existing ImageAssetIdParser, ImageAssetResolution and ImageAssetResolver are small
consumer-side input adapters over IImageAssetAccessService; they preserve validation
and optional-image behavior without exposing Images Application to consumers.
Images.Contracts remains the legacy ID-only package. Preserve CLR namespaces, owner
checks, confirmation requirements, null behavior and existing error codes.

IImageAssetContentService supplies transient data URLs for provider requests after
owner and confirmation checks. Consumers must not log, persist or enqueue image
content; persisted recognition jobs continue to contain asset identifiers.
