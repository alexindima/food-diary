# RecipeCommunity Infrastructure

Own repository adapters and full AddRecipeCommunityModule registration. Preserve scoped repository aliases and request transaction behavior. Consume the scoped IModuleContextFactory contract without referencing central Infrastructure; never add repository SaveChanges or provider calls.

Read comment authors through Users.Contracts IUserCommentAuthorReadService after
selecting the ordered page. Batch distinct page user IDs; preserve total count,
null names and missing-author omission without refilling the page. Do not read
Users sets or reference Users.Domain.

Runtime writes use the owned RecipeCommunityDbContext created by the central factory and
shared unit of work (ADR 0040). Central migration mappings remain.

Current module convention: all projects use `FoodDiary.Modules.RecipeCommunity.<Project>` assembly identities and namespaces matching their folders, including tests. Preserve historical migration metadata and database/HTTP contracts during namespace moves.
