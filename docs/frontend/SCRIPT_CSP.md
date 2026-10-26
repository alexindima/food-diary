# Script content security policy

Every npm main-site build runs `scripts/protect-script-csp.mjs` after Angular
prerendering and before service-worker finalization. It places a script policy
near the start of each trusted HTML document, before executable scripts. The
policy allows the same origin, the existing provider origins in
`src/app/shared/security/script-csp-sources.json`, and SHA-256 hashes of that
document's build-owned inline scripts. JSON data blocks are excluded. Static
nonces, inline event attributes and unmanaged CSP meta elements fail the build.
The processor preserves script bytes and updates existing HTML hash-table entries
in `ngsw.json`; it does not add API responses to the service worker.

## Static publication and SSR

Production switches the static `current` release atomically and publishes assets
before recreating SSR. Keeping each static document's script policy in its HTML
avoids a hash list loaded by Nginx becoming inconsistent with the selected release
during deployment or rollback. Offline PWA HTML retains the same policy.

The build also writes the union of trusted inline hashes to
`dist/server/script-csp-hashes.txt`, outside public browser assets. The Node SSR
entry point validates and caches this manifest. Its health check fails if the
manifest is missing or invalid. For each `/explore` response it generates 24
cryptographically random bytes, sends a script CSP header containing the nonce
and trusted build hashes, and passes the nonce through `REQUEST_CONTEXT` to
Angular's `CSP_NONCE`. Angular uses it for its generated event-replay script.
Dynamic responses retain `Cache-Control: no-store`. Runtime-rendered user content
is never hashed or stamped with a nonce to authorize it. CLI development imports
do not require the production manifest; direct Node and PM2 entry points enforce
the production policy.

The existing edge Nginx header remains a compatibility policy, including its
`unsafe-inline` source. It combines with the representation's additional policy;
it cannot relax the build meta policy or SSR script header. This follows the
[CSP multiple-policy rules](https://www.w3.org/TR/CSP/#multiple-policies).
Do not remove its inline permission without providing the trusted representation
hashes/nonces at that layer: the policies must all allow the intended script.
Other edge directives, including framing and style restrictions, still apply.

This is stronger inline-script protection, not a claim of complete XSS
prevention, nonce-only strict-dynamic CSP, or Trusted Types enforcement. Same-origin
and listed provider scripts remain trusted. Old cached HTML needs a release update
to receive the new policy. Real Google/Telegram/Paddle flows and actual deployed
headers remain release checks. Angular's built-in `autoCsp` currently excludes SSR;
see [Angular security guidance](https://angular.dev/best-practices/security).

## Verification

- `npm run test:performance-build` checks build policy generation, original script
  preservation, static nonce/event rejection, idempotence and PWA HTML hashes.
- After building, `npm run test:script-csp:browser` starts owned pinned Node/Nginx
  containers and an isolated Chromium/Chrome session. External network requests
  are intercepted. An unapproved script is inserted only into a copied artifact
  after policy generation. The test proves it is blocked while trusted startup
  works, then checks SSR nonce uniqueness, no-store and script-policy violations.
  Containers are removed by their verified owned IDs; sanitized screenshots and
  logs remain under `.artifacts/session-hardening/csp-browser/`.
- The browser check also runs through `test:web-quality:browser`. Prepare Docker
  with `scripts/Ensure-Docker.ps1` locally; CI needs a Linux Docker engine and the
  configured Playwright browser.
