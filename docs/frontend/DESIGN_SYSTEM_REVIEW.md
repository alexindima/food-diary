# Frontend Design System Review

Last reviewed: 2026-07-10.

This document is the living inventory for visual consistency across the public client, authenticated application, dialogs, and admin client. Update the relevant row whenever a page or shared visual primitive changes.

## Review Contract

Every reviewed page must be checked at `1440x900` and `390x844` in Russian and English. Shared UI primitives must additionally be checked in all supported combinations:

- themes: `ocean`, `leaf`, `dark`;
- UI styles: `classic`, `modern`;
- loaded, empty, loading, validation, and error states where applicable;
- keyboard focus, visible labels, horizontal overflow, console warnings, and dialog scrolling.

The minimum page contract is:

| Concern                | Required source                                                                         |
| ---------------------- | --------------------------------------------------------------------------------------- |
| Page background        | `--fd-bg-body`, `--fd-bg-page`, or a documented public landing token                    |
| Raised surface         | `fd-ui-card`, `fd-ui-surface`, or a documented feature shell built entirely from tokens |
| Card radius            | `--fd-radius-card`; do not copy its computed pixel value                                |
| Panel/dialog radius    | `--fd-radius-panel` / `--fd-radius-dialog`                                              |
| Card padding           | UI-kit density API or `--fd-space-card-*` tokens                                        |
| Nested surface padding | `--fd-space-surface-padding` or an explicit UI-kit density                              |
| Text and borders       | semantic `--fd-color-*`, `--fd-bg-*`, and `--fd-border-*` tokens                        |
| Effects                | `--fd-shadow-*`, `--fd-transition-*`, and `--fd-transform-*` tokens                     |

Computed values vary by theme and UI style. For example, `dark + modern` currently resolves the application shell to `#14171c`, raised surfaces to `#2b2f36`, card/dialog radii to `8px`, profile card padding to `16px 18px 20px`, and nested surface padding to `12px 14px`. These values are evidence, not constants to copy into component styles.

## Enforcement

- `npm run stylelint` rejects raw component colors, token fallbacks, hardcoded non-zero spacing/radii, repeated control sizes, shadows, typography, and transition timings.
- `npm run check:i18n` verifies locale parity, placeholders, legacy bundles, mojibake, replacement characters, and corrupted `???` sequences.
- Storybook `Foundation/Design Tokens` renders the active CSS custom properties rather than a static palette.
- UI-kit components own shared card, surface, dialog, input, segmented-control, pagination, and chart behavior. Page-level overrides should express feature structure only.

When two screens show the same semantic object, they must use the same primitive and density. Current examples:

| Object                          | Canonical implementation                                                          |
| ------------------------------- | --------------------------------------------------------------------------------- |
| Product, recipe, or meal result | shared media/entity card                                                          |
| Profile and settings section    | `fd-ui-card` with `profile` density                                               |
| Compact nested setting          | `fd-ui-surface` with shared surface padding                                       |
| Dashboard metric                | dashboard card shell with token-driven feature content                            |
| Modal                           | `fd-ui-dialog` and its size/body-scroll APIs                                      |
| Time period or mode choice      | tabs or `fd-ui-segmented-toggle`, depending on whether content or a value changes |

## Public Pages

`D` means desktop checked, `M` means mobile checked, and `-` means the viewport remains in the queue.

| Route                              | Shared pattern      | D   | M   | Notes                                                                                        |
| ---------------------------------- | ------------------- | --- | --- | -------------------------------------------------------------------------------------------- |
| `/`                                | main public landing | yes | yes | No overflow; stored sessions hide prerendered content until the root-route redirect resolves |
| `/food-diary`                      | SEO landing         | -   | yes | Shared SEO composition                                                                       |
| `/calorie-counter`                 | SEO landing         | -   | yes | Shared SEO composition                                                                       |
| `/meal-planner`                    | SEO landing         | -   | yes | Shared SEO composition                                                                       |
| `/macro-tracker`                   | SEO landing         | -   | yes | Shared SEO composition                                                                       |
| `/intermittent-fasting`            | SEO landing         | -   | yes | Shared SEO composition                                                                       |
| `/meal-tracker`                    | SEO landing         | -   | yes | Shared SEO composition                                                                       |
| `/weight-loss-app`                 | SEO landing         | -   | yes | Shared SEO composition                                                                       |
| `/dietologist-collaboration`       | SEO landing         | -   | yes | Shared SEO composition                                                                       |
| `/nutrition-planner`               | SEO landing         | -   | yes | Shared SEO composition                                                                       |
| `/weight-tracker`                  | SEO landing         | -   | yes | Shared SEO composition                                                                       |
| `/body-progress-tracker`           | SEO landing         | -   | yes | Shared SEO composition                                                                       |
| `/shopping-list-for-meal-planning` | SEO landing         | -   | yes | Shared SEO composition                                                                       |
| `/nutrition-tracker`               | SEO landing         | -   | yes | Shared SEO composition                                                                       |
| `/food-log`                        | SEO landing         | -   | yes | Shared SEO composition                                                                       |
| `/protein-tracker`                 | SEO landing         | -   | yes | Shared SEO composition                                                                       |
| `/meal-prep-planner`               | SEO landing         | -   | yes | Shared SEO composition                                                                       |
| `/privacy-policy`                  | legal page          | -   | yes | No overflow                                                                                  |
| not-found route                    | public state page   | -   | -   | Pending copy and responsive review                                                           |

## Authentication

| Surface               | D   | M   | Notes                                                                                                          |
| --------------------- | --- | --- | -------------------------------------------------------------------------------------------------------------- |
| Login/register dialog | yes | yes | Uses shared dialog; Russian labels, policy validation, and Google unavailable state render correctly           |
| `/mobile/login`       | yes | yes | Shared auth forms; no nested main landmark, duplicate title, or overflow                                       |
| `/verify-pending`     | yes | yes | Real unverified account plus deterministic smoke fixture; refresh, resend, cooldown, and shell spacing checked |
| `/verify-email`       | yes | yes | Missing-token and API-error states use distinct recovery actions                                               |
| `/reset-password`     | yes | yes | Invalid token, validation, API error, and retryable form states checked                                        |

## Authenticated Client

| Route                                    | Primary surface pattern           | D   | M       | Notes                                                                                 |
| ---------------------------------------- | --------------------------------- | --- | ------- | ------------------------------------------------------------------------------------- |
| `/dashboard`                             | dashboard card shell              | yes | yes     | Token-driven custom cards; candidate for gradual convergence on UI-kit card contracts |
| `/meals`                                 | shared media/entity cards         | yes | yes     | Matches products and recipes                                                          |
| `/meals/add`, `/meals/:id/edit`          | manage form cards                 | yes | yes     | Filled edit state, nested dialogs, unit copy, and discard protection checked          |
| `/products`                              | shared media/entity cards         | yes | yes     | Matches meals and recipes                                                             |
| `/products/add`, `/products/:id/edit`    | manage form cards                 | yes | yes     | Filled edit state, delete confirmation, and discard protection checked                |
| `/recipes`                               | shared media/entity cards         | yes | yes     | Matches meals and products                                                            |
| `/recipes/add`, `/recipes/:id/edit`      | manage form cards                 | yes | yes     | Add and discard flows checked; owned edit/delete state still needs a stable fixture   |
| `/explore`                               | shared recipe results             | -   | yes     | No overflow                                                                           |
| `/shopping-lists`                        | card/list workspace               | -   | yes     | No overflow                                                                           |
| `/goals`                                 | metric cards and controls         | -   | yes     | No overflow                                                                           |
| `/statistics`                            | `fd-ui-card` and line chart       | -   | yes     | Mobile chart overflow fixed in this review                                            |
| `/weight-history`                        | `fd-ui-card` and line chart       | -   | yes     | End-axis label containment fixed                                                      |
| `/waist-history`                         | `fd-ui-card` and line chart       | -   | yes     | End-axis label containment fixed                                                      |
| `/cycle-tracking`                        | metric and log cards              | -   | yes     | No overflow                                                                           |
| `/meal-plans`                            | plan cards                        | -   | yes     | No overflow                                                                           |
| `/meal-plans/:id`                        | plan detail                       | -   | -       | Needs seeded plan data                                                                |
| `/weekly-check-in`                       | summary cards                     | -   | yes     | No overflow                                                                           |
| `/lessons`                               | lesson cards                      | -   | yes     | No overflow                                                                           |
| `/lessons/:id`                           | lesson detail                     | -   | -       | Needs seeded lesson data                                                              |
| `/gamification`                          | metric and badge cards            | -   | yes     | No overflow                                                                           |
| `/fasting`                               | timer card and segmented controls | -   | yes     | Mobile mode selector now stacks without overlap                                       |
| `/premium`                               | access and plan cards             | -   | yes     | No overflow                                                                           |
| `/profile`                               | `fd-ui-card` profile density      | yes | yes     | Compact phones now use one-column account fields                                      |
| `/dietologist`                           | client cards                      | -   | blocked | Admin test account redirects to dashboard; requires a dietologist account             |
| `/dietologist/clients/:clientId`         | client dashboard                  | -   | -       | Requires shared client fixture                                                        |
| `/dietologist-invitations/:invitationId` | invitation state                  | -   | -       | Requires invitation fixture                                                           |
| `/recommendations`                       | recommendation list               | -   | yes     | No overflow                                                                           |

## Admin Client

The admin client remains a separate review batch because it runs on its own host and has different information density. Inventory:

- `/`, `/users`, `/users/login-activity`, `/users/impersonation-sessions`;
- `/ai-usage`, `/acquisition`, `/billing`, `/email-templates`, `/mail-inbox`;
- `/lessons`, `/moderation`, and `/unauthorized`.

Review desktop at `1440x900` and a compact laptop width before mobile. Admin workflows may intentionally remain desktop-first, but they must not overflow or hide required actions at supported widths.

## Findings Ledger

| Priority | Finding                                                                                                   | Status                                                                       |
| -------- | --------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------- |
| P1       | 170 Russian tour strings were stored as question marks                                                    | Fixed; CI now rejects the corruption pattern                                 |
| P1       | Mobile horizontal overflow in statistics, weight history, waist history, and fasting                      | Fixed and browser-verified at `375x844`                                      |
| P1       | Storybook token pages used a stale static TypeScript palette                                              | Fixed; stories now render live CSS variables                                 |
| P2       | Profile fields used two columns on compact phones and clipped long Russian labels                         | Fixed with a one-column compact layout                                       |
| P2       | Authenticated navigation to `/` briefly rendered the public landing before redirecting                    | Fixed with a pre-hydration stored-session gate released on `NavigationEnd`   |
| P2       | Secondary authenticated pages have mobile coverage but incomplete desktop visual coverage                 | Open; next review batch                                                      |
| P2       | Detail/manage pages need deterministic fixtures for complete visual states                                | Open; add fixtures before baseline screenshots                               |
| P2       | Manage forms expanded every nutrient into a single mobile column                                          | Fixed; normal phone widths retain the compact shared nutrient grid           |
| P2       | Meal and recipe image uploads consumed a full square card width on mobile                                 | Fixed; all three manage forms now share a compact 240px upload treatment     |
| P2       | Waist dashboard card reused the weight-specific empty-state copy                                          | Fixed; trend blocks now accept a semantic empty-state key                    |
| P2       | Shared dialog close control had a hardcoded English accessible name                                       | Fixed API; audited manage-flow dialogs now provide the localized label       |
| P2       | Russian manage-flow copy mixed `AI`, `Push`, and `прием` with localized terminology                       | Fixed in the audited flows and adjacent settings copy                        |
| P1       | Meal and recipe manage forms could discard dirty values without confirmation                              | Fixed; both forms now share explicit stay/discard confirmation               |
| P2       | Product discard confirmation reused the delete icon                                                       | Fixed; destructive navigation now uses a logout icon                         |
| P2       | Detail dialogs and form clear controls exposed English-only accessible names                              | Fixed in product, recipe, and meal manage/detail flows                       |
| P2       | Meal edit summary rendered measurement names as `180 граммы`                                              | Fixed; compact amount summaries use localized unit symbols                   |
| P3       | Dashboard uses feature-specific card shells instead of `fd-ui-card`                                       | Review gradually; current values are token-driven and visually consistent    |
| P1       | Password reset replaced the form after a recoverable API error, preventing retry                          | Fixed; the form and entered values remain available for another attempt      |
| P2       | Standalone auth pages used inconsistent backgrounds, alignment, headings, and card density                | Fixed with a shared token-driven auth page shell                             |
| P2       | Invalid email-verification links offered a retry action that could never succeed                          | Fixed; invalid links return to login while request failures remain retryable |
| P1       | Verification pending used a full viewport inside the authenticated mobile shell, adding 126px of overflow | Fixed with the shell mobile-navigation spacing token                         |
| P2       | Notification SignalR logged a warning during expected logout-driven disconnects                           | Fixed; only unexpected closes during an active session warn                  |

## Per-Page Checklist

For each row moved to reviewed:

1. Confirm the page title, subtitle, actions, and first viewport hierarchy.
2. Record the page background token and every distinct surface primitive/density.
3. Compare repeated cards with the canonical implementation listed above.
4. Check desktop and mobile for horizontal overflow, clipping, overlap, sticky-element occlusion, and readable Russian labels.
5. Exercise at least one primary interaction and one modal or menu when present.
6. Check loading, empty, populated, validation, and error states.
7. Check console warnings/errors and keyboard focus.
8. Run `npm run stylelint`, focused unit tests, `npm run check:i18n`, and the relevant smoke suite.
