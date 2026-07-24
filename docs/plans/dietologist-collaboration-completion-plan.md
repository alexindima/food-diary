# Dietologist Collaboration Completion Plan

## Goal

Turn the existing client–dietologist connection and one-way recommendation flow into a complete,
auditable collaboration workspace without weakening client-controlled data permissions.

## Current verified baseline

- An administrator can create a client and a user with the `Dietologist` role.
- A client can invite a dietologist and choose data-sharing permissions.
- A dietologist can accept the invitation, open the client dashboard, filter a period, and view only
  permitted categories.
- A dietologist can send a recommendation.
- The client receives a notification, opens the recommendation, and its read status is synchronized.
- Loading failures, destructive disconnect confirmation, empty fasting state, keyboard access,
  route titles, and locale-sensitive dates/profile values are covered by the current change set.

## Delivery slices

### 1. Recommendation discussion

Add a chronological thread to every recommendation.

Acceptance criteria:

- The client and the connected dietologist can post plain-text messages of at most 2,000 characters.
- Only the recommendation's client and its authoring dietologist can read or post messages.
- A disconnected dietologist cannot post new messages.
- Messages store author, creation time, and immutable text.
- A message creates a notification for the other participant with a deep link to the recommendation.
- The client and dietologist UIs display the same ordered thread and optimistic submission is not
  treated as success until the API confirms it.
- API authorization, integration, application, domain, repository, and UI tests cover both roles and
  forbidden access.

### 2. Client tasks

Allow a dietologist to turn guidance into trackable work.

Acceptance criteria:

- A task has a title, optional details, optional due date, and status: `Open`, `Completed`, or
  `Cancelled`.
- Only a currently connected dietologist can create or cancel a task for that client.
- Only the client can mark a task complete or reopen it.
- Both roles see current and historical tasks; overdue state is derived from UTC time.
- Creation, status changes, and approaching due dates produce notifications without duplicates.
- Tasks remain in history after disconnect, but neither party can mutate them until an active
  relationship exists again.

### 3. Recommendation templates and bulk actions

Reduce repeated work while keeping every outgoing recommendation explicit.

Acceptance criteria:

- Templates are private to a dietologist and support create, update, archive, list, and search.
- A template may prefill recommendation text but never sends automatically.
- Bulk send requires an explicit client selection and confirmation showing the recipient count.
- Every recipient gets an independent recommendation and notification.
- Partial failures are returned per client and successful sends are not rolled back or duplicated
  when a retry uses the same idempotency key.

### 4. Trends and attention queue

Surface objective signals instead of making the dietologist manually inspect every dashboard.

Acceptance criteria:

- Signals include diary inactivity, sustained calorie-target deviation, and material weight change.
- Thresholds and lookback periods are documented and configurable per dietologist.
- Signals are calculated only from categories the client currently shares.
- The clients page shows an attention queue with signal reason, date, and severity.
- A dietologist can acknowledge or snooze a signal; the action is audited.
- Empty or sparse data is reported as insufficient data, never as a negative health conclusion.

### 5. Audit trail

Record sensitive collaboration actions.

Acceptance criteria:

- Invite, accept/decline, permission change, dashboard access, recommendation/message/task creation,
  read/completion state changes, bulk sends, and disconnects create append-only audit entries.
- Entries store actor, subject client, action, UTC timestamp, and non-sensitive metadata.
- Recommendation or message text and health measurements are not copied into audit metadata.
- Audit records are queryable by authorized administrators and retained according to a documented
  policy.
- Audit writes are part of the same transaction as the action they describe.

Retention policy: collaboration audit entries are append-only and retained for 365 days after
creation. Removal after that period is an administrative maintenance operation; application users
cannot edit or delete individual entries. Metadata must contain identifiers and state names only,
never recommendation/comment text or health measurements.

## Attention signal defaults

- Diary inactivity: 3 days without a shared diary entry.
- Sustained calorie-target deviation: at least 25% away from the shared target on 3 logged days.
- Material weight change: at least 3% across a 14-day shared trend.
- Lookback: 14 days.

Dietologists can adjust these thresholds before recalculating the queue. Signals are descriptive
workflow prompts, not diagnoses. A missing target or fewer than the required data points produces no
negative signal; diary absence is explicitly labelled as insufficient diary data when no entry has
ever been shared.

### 6. Permission matrix E2E suite

Prove that client consent is enforced by the API as well as hidden in the UI.

Acceptance criteria:

- Each permission is tested independently in enabled and disabled states.
- Tests assert the underlying API response, not only DOM visibility.
- Revoking a permission takes effect on the next request without requiring a new login.
- Direct URL and handcrafted API requests cannot bypass a disabled permission.
- Disconnect immediately blocks all dietologist client-data endpoints and mutations.

## Recommended order

1. Recommendation discussion.
2. Client tasks.
3. Audit trail foundations, then instrument existing and new actions.
4. Templates and bulk sends.
5. Trends and attention queue.
6. Full permission matrix and end-to-end regression pass.

Discussion and tasks establish the shared authorization model. Audit logging should then be added
before bulk operations and automated signals expand the amount of sensitive activity.
