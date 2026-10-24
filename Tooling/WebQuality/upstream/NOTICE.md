# Upstream guidance provenance

The content-addressed Markdown files in `sources/` are unmodified published
GoogleChrome Modern Web Guidance documents. They are reference data, not local
agent instructions. The accepted repository, exact commit, paths and Git blob
hashes are recorded in `upstream.lock.json`.

Copyright Google LLC and the Modern Web Guidance contributors.
The pinned repository is distributed under Apache 2.0; its LICENSE is included
alongside this notice. Its attribution states that portions of the documentation
derive from MDN Web Docs by Mozilla Contributors and W3C, WHATWG and IETF
specifications. Original source links and content are preserved.

Changes to accepted source data require explicit review of dependent FoodDiary
requirements. The update command stages candidates outside this directory.

The scoped Git attributes preserve original Markdown bytes and suppress
whitespace-only diagnostics for these immutable upstream files. Source integrity
is enforced by Git blob verification; local application/tool code retains its
ordinary whitespace checks.
