# Favorites contract ownership

Favorites Contracts owns public favorite read services/projections and the three
consumer-owned source readers and immutable source models supplied by Meals,
Products and Recipes. Meals also consumes IMealFavoriteReadService from this same
project. All IDs come from the relevant scalar Domain.Contracts projects; Results
preserves the source readers' existing error contract.

Favorites Application/Abstractions owns repository interfaces, persistence read
models and error factories. Foreign business modules cannot reference it. The
central Errors facades are retired. No aggregate or repository type is exported
through the consumer contracts.

Relocation preserves namespaces, signatures, nullability, cancellation defaults,
user-scoped access and error behavior. Source implementations, EF tracking,
transactions, DI lifetimes and HTTP payloads stay unchanged. Rebuild and deploy
hosts and their consumers together after assembly relocation; rollback requires
a coordinated code rebuild, with no database migration.

FavoritesContractOwnershipTests protects physical ownership and rejects aggregate
or repository types in Contracts. NarrowConsumerContractTests protects the full
contract dependency closure and rejects foreign internal-port references.
