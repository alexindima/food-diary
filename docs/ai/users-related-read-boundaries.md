# Users related-data read boundaries

Fasting and RecipeCommunity Infrastructure no longer reference Users.Domain or
query the Users set. Users.Contracts supplies two narrow batch readers:
IUserFastingReminderReadService and IUserCommentAuthorReadService. Users owns the
scoped UserRelatedDataReadService implementation and its no-tracking projections.

Fasting loads active occurrences and their Plan, preserving start-time order,
then reads distinct users' reminder settings once. Comments count and select the
ordered page first, then read distinct page authors once. Missing users are omitted
without page refill; total count is unchanged. Null author names and inactive or
soft-deleted accounts retain former behavior. These capabilities are not access
checks. Empty batches perform no SQL; no credentials or entire User is loaded.

This trades each cross-module join for one additional batch query, never a query
per row. Under the existing read-committed behavior, a concurrent user change can
be observed between selecting rows and loading related values. A user deleted
between reads is omitted. No stronger snapshot guarantee is introduced; no new
transaction, lock, migration, HTTP shape or notification policy is added.

Provider tests cover narrow SQL, account states, duplicate/missing IDs, cancellation,
no tracking, comment page boundaries and reminder composition. Architecture guards
protect both removed project edges and direct foreign set reads. Deploy by rebuilding
hosts with the Users registration changes; revert contracts/adapters/DI together.
Shared DbContext and other Infrastructure cross-module reads remain.
