# Public recipes

`/explore` is the anonymous, paginated recipe catalog. `/explore/:id` is a shareable
recipe page with photos, scalable ingredient quantities, instructions and nutrition.
Authenticated navigation exposes the catalog separately from the private `/recipes`
workspace. Guests receive a small header; saving or adding to the diary requests
sign-in and continues the action after successful authentication.

## Access and data

The anonymous API is `GET /api/v1/recipes/public` and `GET /api/v1/recipes/public/{id}`.
Only currently public recipes are returned. Private and missing IDs both return 404.
The list defaults to 20 items (maximum 50), with search, category and total-time filters.
Ordering is newest first with an ID tie-breaker.

Responses use a separate allowlist: no owner identifiers, personal notes, image asset
IDs or user-specific favorite state. Private source products and nested recipes remain
unavailable ingredients; publishing a containing recipe does not publish those sources.
Partial nutrition remains explicitly marked. Text quantities are not numerically scaled.

## Rendering and deployment

Catalog and detail use Angular server rendering. Detail HTML contains recipe-specific
metadata and Recipe JSON-LD; unavailable pages have HTTP 404 and noindex. Other public
marketing pages retain prerendering, and authenticated routes retain client rendering.
Public recipe responses use no-store; the service worker excludes these navigation URLs.

The client image includes Node and `dist/server` alongside static assets. Compose runs
`client-ssr` from the same image, using `SSR_API_ORIGIN=http://api:5000` and port 4000
bound only to host loopback. Both nginx configurations forward `/explore` paths to it.
The deployment workflow starts it after API readiness and checks `/health/ssr`.

Roll out the API, client image, Compose configuration and nginx configuration together.
Apply pending database migrations before starting the matching API. Verify a public URL returns populated HTML with HTTP
200, a private URL returns 404 without private content, and static assets still load.
Rollback must restore the matching client/nginx configuration before stopping SSR.
Local SSR uses `npm run build:prod` then `npm run serve:ssr:food-diary-web-client`;
the default API origin is `http://localhost:5300`.

## Verification

- Public recipe application tests cover projection, visibility and pagination.
- The PostgreSQL API flow covers anonymous access, publication, private sources,
  invalid pagination and unpublishing. OpenAPI snapshots include the public endpoints.
- Frontend tests cover requests, URL filters, pagination, resolver errors, metadata,
  ingredient scaling, galleries, cooking steps and actions following sign-in.
- Manual checks include guest desktop/mobile navigation and production SSR HTML/status.

## Fixed recipe categories

Recipes store a non-null category code. `other` is the default. The editor, private
list and public catalog use the same 15 localized choices. API category filters
accept exact codes; unknown codes return HTTP 400. The public categories endpoint
returns the fixed code list, independent of recipe visibility, language or contents.
RU/EN display names belong to `RECIPE_CATEGORIES` in the frontend locale files.

Codes: `other`, `breakfast`, `soups`, `salads`, `main_courses`, `side_dishes`,
`appetizers`, `sandwiches`, `pasta`, `baking`, `desserts`, `drinks`, `sauces`,
`snacks`, `preserves`.

`AddFixedRecipeCategories` maps known legacy Russian/English labels to codes and
maps blank, unknown or ambiguous values to `other` before enforcing the database
constraint. This normalization is irreversible: the down migration restores the
old column shape, not the original free text. Back up production data before the
migration; deploy the API and frontend together because old clients may send free
text. Migration safety tests cover legacy mapping and a clean migration chain.
