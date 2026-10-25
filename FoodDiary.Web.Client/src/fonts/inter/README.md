# Inter web font

Unmodified Google Fonts v20 WOFF2 subsets, licensed under the included SIL OFL.
The variable weight range is 400–600, matching the previously requested weights.
Unicode subsets preserve the existing language coverage without loading unused
alphabets. `manifest.json` records immutable font URLs, SHA-256 hashes, byte sizes,
Unicode ranges and the pinned license source. Application CSS imports these files
from `src/styles/fonts.scss`; the bundler emits versioned media URLs for both apps.

`Inter Fallback` uses local Arial or its metric-compatible Liberation Sans face
before a requested subset arrives. It downloads no extra font. Regular/medium and
bold fallbacks have separate width adjustments approximated from public English
and Russian copy. The bundled Inter has UPM 2048, ascent 1984, descent 494 and zero
line gap in both hhea/typo tables. The override percentages divide those metrics
by UPM and the width adjustment, following
[Chrome's font fallback method](https://developer.chrome.com/blog/font-fallbacks).
Recalculate these values when the font or representative typography changes.
Fallback matching reduces differences; it does not guarantee identical glyph
widths or zero CLS. The loaded Inter face and existing system fallback chain
remain available.

To update, retrieve the CSS at `cssSource` using a modern browser user agent,
review the new Google Fonts version and its license, and replace each matching
unmodified subset. Update the manifest hashes/ranges and CSS together. Do not
rename modified fonts as an upstream font or omit its license. Run the font asset
test, client/admin and Storybook builds, browser font-failure scenarios and repeated
Lighthouse measurements before accepting the update.
