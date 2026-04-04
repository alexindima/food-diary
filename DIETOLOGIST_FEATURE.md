# Dietologist Feature

## Overview

The dietologist feature enables professional nutritionists to monitor their clients' nutrition data and provide recommendations. Users invite a dietologist by email, control what data is shared via granular permissions, and receive in-app notifications when new recommendations arrive.

## Roles

| Role | Description |
|------|-------------|
| **User** (existing) | Tracks meals, weight, goals, hydration |
| **Dietologist** (new) | Views client data (read-only), sends recommendations |

A user can have **one** active dietologist. A dietologist can have **many** clients. A user who is also a dietologist sees both their personal diary and the "My Clients" section in the sidebar.

## Invitation Flow

1. **User** opens profile settings, enters dietologist's email, configures data sharing permissions (6 toggles)
2. System sends a bilingual (ru/en) invitation email with a secure token link (7-day expiry)
3. **Dietologist** clicks the link:
   - If already registered: accepts the invitation, gains the `Dietologist` role
   - If new: registers first, then accepts
4. Relationship becomes active, dietologist appears in sidebar with "My Clients"
5. Either party can disconnect at any time

## Data Sharing Permissions

Users control what the dietologist can see via 6 independent toggles (all enabled by default):

| Permission | Data category |
|-----------|---------------|
| `ShareMeals` | Food diary, meal entries |
| `ShareStatistics` | Calories, macros, nutrition charts |
| `ShareWeight` | Weight history |
| `ShareWaist` | Waist measurement history |
| `ShareGoals` | Nutrition targets (calories, protein, fat, carbs) |
| `ShareHydration` | Water intake tracking |

Permissions are set during invitation and can be changed at any time in profile settings. Changes take effect immediately -- the dietologist's next API call will be checked against the updated permissions.

## Recommendations

Dietologists can send text recommendations to their clients (max 2000 characters). Recommendations are:
- Created from the client dashboard view
- Visible to the client in their recommendations page
- Tracked with read/unread status
- Trigger an in-app notification

## Notifications

- **In-app badge** in the sidebar shows unread notification count
- **Real-time updates** via SignalR (`/hubs/notifications`) push the unread count when a new notification is created
- Notification types: `NewRecommendation`, `InvitationAccepted`
- Users can mark individual notifications or all as read

## API Endpoints

### Invitation Management (`/api/v1/dietologist/`)

| Method | Path | Description |
|--------|------|-------------|
| POST | `/invite` | Client invites dietologist by email |
| POST | `/accept` | Dietologist accepts invitation |
| POST | `/decline` | Dietologist declines invitation |
| DELETE | `/relationship` | Client disconnects from dietologist |
| PUT | `/permissions` | Client updates sharing permissions |
| GET | `/my-dietologist` | Client gets their dietologist info |
| GET | `/invitation/{id}` | Resolve invitation details |

### Client Data Access (`/api/v1/dietologist/clients/`)

Requires `Dietologist` role. All endpoints verify active relationship and check per-category permissions.

| Method | Path | Description |
|--------|------|-------------|
| GET | `/` | List connected clients |
| DELETE | `/{clientId}` | Disconnect from client |
| GET | `/{clientId}/dashboard` | Client's dashboard snapshot |
| GET | `/{clientId}/goals` | Client's nutrition goals |
| POST | `/{clientId}/recommendations` | Send recommendation |
| GET | `/{clientId}/recommendations` | View sent recommendations |

### Client Recommendations (`/api/v1/recommendations/`)

| Method | Path | Description |
|--------|------|-------------|
| GET | `/` | Client views their recommendations |
| PUT | `/{id}/read` | Mark recommendation as read |

### Notifications (`/api/v1/notifications/`)

| Method | Path | Description |
|--------|------|-------------|
| GET | `/` | Get recent notifications |
| GET | `/unread-count` | Get unread badge count |
| PUT | `/{id}/read` | Mark one as read |
| PUT | `/read-all` | Mark all as read |

## Database Schema

### `DietologistInvitations`

Tracks the full invitation lifecycle (Pending -> Accepted/Declined/Expired/Revoked) and stores sharing permissions as separate boolean columns.

- Foreign keys: `ClientUserId` -> Users, `DietologistUserId` -> Users (nullable)
- Indexes: `(ClientUserId, Status)`, `(DietologistEmail, Status)`, `DietologistUserId`

### `Recommendations`

Dietologist-to-client text recommendations with read tracking.

- Foreign keys: `DietologistUserId` -> Users, `ClientUserId` -> Users
- Indexes: `ClientUserId`, `(DietologistUserId, ClientUserId)`

### `Notifications`

Generic notification entity for in-app notifications.

- Foreign keys: `UserId` -> Users
- Indexes: `(UserId, IsRead)` for efficient badge count queries

## Architecture

### Backend

Follows existing Clean Architecture patterns:

- **Domain**: `DietologistInvitation`, `Recommendation`, `Notification` entities with strongly-typed IDs, `DietologistPermissions` value object, domain events
- **Application**: CQRS commands/queries via MediatR, `DietologistAccessPolicy` for relationship + permission checks, `IDietologistEmailSender` interface
- **Infrastructure**: EF Core repositories, `DietologistEmailSender` (SMTP with HTML template), `NotificationRepository`
- **Presentation**: `DietologistController`, `DietologistClientsController`, `RecommendationsController`, `NotificationsController`, `NotificationHub` (SignalR), `NotificationPusher`

### Frontend

- `AuthService.isDietologist` computed signal from JWT role claim
- `dietologistGuard` for route protection
- Lazy-loaded routes under `/dietologist`
- `DietologistService` API service
- `NotificationService` with `unreadCount` signal
- Sidebar: "My Clients" link (dietologist only) + notification badge
- Client list page and client dashboard with permission-based sections

## Security

- JWT `Dietologist` role required for all client data endpoints
- Active relationship verified on every data access via `DietologistAccessPolicy`
- Per-category permissions checked before returning data (403 if disabled)
- Invitation tokens are BCrypt-hashed, 7-day expiry
- Client can revoke access instantly by disconnecting
