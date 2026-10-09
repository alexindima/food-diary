# RecipeCommunity scalar domain contracts

Own RecipeCommentId and RecipeLikeId as scalar identifiers with canonical project
and folder namespaces. Reference only shared Domain.Primitives for IEntityId.
Keep aggregates, use cases, persistence and provider concerns outside this project.
Preserve Guid values, Empty/New behavior and explicit Guid-to-ID conversion.
