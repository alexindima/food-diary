import assert from 'node:assert/strict';
import test from 'node:test';
import { inspectPrerenderTranslationState } from './prerender-translations.mjs';

const html = state => `<script id="ng-state" type="application/json">${JSON.stringify(state)}</script>`;
test('public hydration carries only the needed translated bundles', () => {
    const state = { 'fd-i18n:ru:core': { COMMON: {} }, 'fd-i18n:ru:seo': { SEO: {} } };
    assert.deepEqual(inspectPrerenderTranslationState(html(state), 'ru', 'seo'), []);
    for (const invalid of [
        {},
        { ...state, 'fd-i18n:ru:app': {} },
        { ...state, 'fd-i18n:ru:landing': {} },
        { 'fd-i18n:en:core': {} },
        { ...state, 'fd-i18n:ru:seo': null },
    ]) {
        assert.ok(inspectPrerenderTranslationState(html(invalid), 'ru', 'seo').length > 0);
    }
    assert.ok(inspectPrerenderTranslationState('<html></html>', 'ru', 'seo').length > 0);
    assert.ok(inspectPrerenderTranslationState('<script id="ng-state" type="application/json">broken</script>', 'ru', 'seo').length > 0);
});
