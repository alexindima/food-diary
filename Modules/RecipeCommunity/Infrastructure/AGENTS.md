# RecipeCommunity Infrastructure

Own repository adapters and full AddRecipeCommunityModule registration. Preserve scoped repository aliases and request transaction behavior. Reference central DbContext one-way; never add repository SaveChanges or provider calls.

Read comment authors through Users.Contracts IUserCommentAuthorReadService after
selecting the ordered page. Batch distinct page user IDs; preserve total count,
null names and missing-author omission without refilling the page. Do not read
Users sets or reference Users.Domain.
