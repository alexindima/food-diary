import { describe, expect, it } from 'vitest';

import { APP_THEMES, APP_UI_STYLES, DEFAULT_APP_THEME, DEFAULT_APP_UI_STYLE, isAppThemeName, isAppUiStyleName } from './app-theme.config';

describe('app theme config', () => {
    it('should expose valid default theme and UI style', () => {
        expect(isAppThemeName(DEFAULT_APP_THEME)).toBe(true);
        expect(isAppUiStyleName(DEFAULT_APP_UI_STYLE)).toBe(true);
    });

    it('should validate theme names', () => {
        for (const theme of APP_THEMES) {
            expect(isAppThemeName(theme.name)).toBe(true);
        }

        expect(isAppThemeName('unknown')).toBe(false);
        expect(isAppThemeName(null)).toBe(false);
        expect(isAppThemeName(undefined)).toBe(false);
    });

    it('should validate UI style names', () => {
        for (const uiStyle of APP_UI_STYLES) {
            expect(isAppUiStyleName(uiStyle.name)).toBe(true);
        }

        expect(isAppUiStyleName('unknown')).toBe(false);
        expect(isAppUiStyleName(null)).toBe(false);
        expect(isAppUiStyleName(undefined)).toBe(false);
    });
});
