# Inter web font

Unmodified Google Fonts v20 WOFF2 subsets, licensed under the included SIL OFL.
The variable weight range is 400–600, matching the previously requested weights.
Unicode subsets preserve the existing language coverage without loading unused
alphabets. `manifest.json` records immutable font URLs, SHA-256 hashes, byte sizes,
Unicode ranges and the pinned license source. Application CSS imports these files
from `src/styles/fonts.scss`; the bundler emits versioned media URLs for both apps.

To update, retrieve the CSS at `cssSource` using a modern browser user agent,
review the new Google Fonts version and its license, and replace each matching
unmodified subset. Update the manifest hashes/ranges and CSS together. Do not
rename modified fonts as an upstream font or omit its license. Run the font asset
test, client/admin and Storybook builds, browser font-failure scenarios and repeated
Lighthouse measurements before accepting the update.
