# Consumer aggregate boundaries

Notifications, Lessons and Identity expose immutable data across the reviewed
consumer seams. Zero direct references to a foreign Domain project alone cannot
establish this boundary: aggregate types can remain available transitively.

## Notifications

`INotificationWriter.AddAsync` accepts `NotificationRequest` with UserId, Type,
PayloadJson and optional ReferenceId. The existing sendWebPush parameter and
cancellation token retain their meaning. NotificationWriter constructs and
validates the aggregate, adds it through the owner repository, then enqueues its
ID when web push is requested. The caller still owns SaveChanges/commit.

Identity, Dietologist, Fasting, WeeklyGoals and RecipeCommunity construct request
data instead of Notification. Their payload factories, recipient selection,
deduplication, post-commit refresh and notification delivery policy are unchanged.
Validation now occurs when the owner writer processes the request, before its
repository/outbox operations. No network transport or new asynchronous queue is
introduced by this contract change.

## Administrative results

Lessons administration returns LessonAdminReadModel snapshots for create, update
and import. Domain mutation, validation, import deduplication and repository access
stay in Lessons. The mutation response retains the previous CompletedCount default
of zero; this operation does not become a reporting query.

Identity email-template administration returns EmailTemplateReadModel. Admin maps
these existing immutable models and no longer has aggregate mapping overloads.
Owner persistence interfaces continue to use owner entities.

This is an in-process source/binary contract change requiring a coordinated build
of the application. HTTP routes, schemas, status codes, authorization, stored data,
provider payloads and migrations are unchanged. Existing Abstractions/Contracts
projects retain their ownership; no project or DI graph change is required.

## Guardrails and verification

ConsumerAggregateContractTests checks the reviewed contract/model sources against
domain entity declarations and the migrated consumers against the foreign entity
types. This is a scoped syntax guard, not a general semantic proof for all contracts.
It complements ProjectDependencyMatrixTests and ApplicationDomainBoundaryTests.

Notification writer tests cover owner validation, recipient/payload preservation,
optional push, cancellation-token forwarding and add-before-outbox identity.
The existing Admin application suite covers lesson create/update/import/retry and
email-template responses. Consumer application suites cover notification behavior.
The common DbContext and remaining broad Abstractions dependencies are separate
architectural concerns and are not removed by this change.
