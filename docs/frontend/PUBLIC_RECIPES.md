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
The deployment workflow publishes static assets after API readiness, then starts
SSR from the same image and checks `/health/ssr`.

Roll out the API, client image, Compose configuration and nginx configuration together.
Apply pending database migrations before starting the matching API. Verify a public URL returns populated HTML with HTTP
200, a private URL returns 404 without private content, and static assets still load.
Rollback must restore the matching client/nginx configuration before stopping SSR.
Local SSR uses `npm run build:prod` then `npm run serve:ssr:food-diary-web-client`;
the default API origin is `http://localhost:5300`.

### Atomic static publication

ADR [0055](../adr/0055-atomic-frontend-publication-and-asset-retention.md) defines the
publication contract for both the user and admin sites. The stable host bind root
contains `releases/<id>`, `current`, `previous` and `retained-assets`. Nginx serves
`current`; only Angular content-hashed JS/CSS/media can fall back to retained bytes.
The publisher leaves the original HTML, worker and `ngsw.json` bytes together in
one release, validating the manifest's index and hash table before switching.
Requests spanning a switch can still observe adjacent versions; manifests are
never rewritten to conceal that boundary.

Deployment copies the immutable image's static output into an owned temporary
directory and calls `scripts/deploy/publish_frontend.py publish` for each site.
The workflow uses `sha256-<client-image-digest>` as the release ID, so rebuilding
the same commit can publish a distinct immutable image without reusing an ID.
It validates paths and build bytes, stages a complete release on the site's own
filesystem, then uses `os.replace` for the internal `current` symlink. It never
replaces the directory mounted into Nginx. Reusing an ID with different bytes or a
hashed URL with different content fails before switching. A failed copy or
pre-switch validation leaves the previous active release available.

On the first migration, existing flat files become the previous digest-identified
release. Old and new hashed files remain available in the stable flat root while
the old Nginx configuration is still running. New hashes are published before SSR
starts returning the corresponding HTML. Nginx is then recreated with the new
`current` roots. Flat HTML and `ngsw.json` remain an intact legacy pair for an old
configuration rollback. ACME HTTP/HTTPS challenges continue using the stable root's
`.well-known` directory.

For an authorized rollback, restore the matching SSR image and Nginx configuration
as one coordinated operation. Under `/var/lock/fooddiary-deploy.lock`, use:

```bash
python3 /opt/fooddiary/scripts/deploy/publish_frontend.py rollback \
  --root /var/www/fooddiary.club --release-id <retained-release-id> --require-ngsw
python3 /opt/fooddiary/scripts/deploy/publish_frontend.py rollback \
  --root /var/www/admin.fooddiary.club --release-id <retained-release-id>
```

Omitting `--release-id` selects that site's `previous`. Record both current IDs
before rollback: each site is atomic independently, and migration's legacy IDs can
differ. Assets for the rollback target are mirrored before its pointer changes.
With the new Nginx configuration, the pointer selects the exact retained release;
an older configuration continues serving its preserved flat legacy shell. Restore
the corresponding SSR image before accepting the rollback as complete. This
procedure does not roll back backend schema or API compatibility.

There is no automatic release or asset pruning in this implementation. Full
releases, retained hashes and the migration compatibility mirror consume disk
space; inspect free space before publication and monitor its growth. Failed
staging directories are kept for diagnosis. Garbage collection requires a
separately agreed retention horizon that accounts for open tabs and old service
workers; do not add an arbitrary age-based deletion to the deploy workflow.

Focused checks are `python3 -B -m unittest discover -s scripts/deploy -p
'test_publish_frontend.py'` on Linux and `pwsh -File
scripts/Test-PwaShellIntegrity.ps1` with the existing Nginx fixture image. The latter
uses owned temporary directories and loopback ports, without starting FoodDiary.

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
