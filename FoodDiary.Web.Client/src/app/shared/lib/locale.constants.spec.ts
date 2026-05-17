import { describe, expect, it } from 'vitest';

import { APP_LOCALE_EN, APP_LOCALE_RU, resolveAppLocale } from './locale.constants';

describe('locale constants', () => {
    it('resolves Russian app locale for ru language', () => {
        expect(resolveAppLocale('ru')).toBe(APP_LOCALE_RU);
    });

    it('uses English app locale as fallback', () => {
        expect(resolveAppLocale('en')).toBe(APP_LOCALE_EN);
        expect(resolveAppLocale(null)).toBe(APP_LOCALE_EN);
        expect(resolveAppLocale(undefined)).toBe(APP_LOCALE_EN);
    });
});
