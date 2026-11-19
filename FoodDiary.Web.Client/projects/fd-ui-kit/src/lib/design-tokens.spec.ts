import { describe, expect, it } from 'vitest';

import { getButtonCssVariable, getColorCssVariable } from './design-tokens';

describe('design token CSS references', () => {
    it('keeps palette shades and semantic colors themeable', () => {
        const emphasizedShade = 500;

        expect(getColorCssVariable('primary', emphasizedShade)).toBe('var(--fd-color-primary-500)');
        expect(getColorCssVariable('semantic', 'danger')).toBe('var(--fd-color-danger)');
    });

    it('resolves the requested button variant, property and state', () => {
        expect(getButtonCssVariable('danger-ghost', 'border', 'hover')).toBe('var(--fd-button-danger-ghost-border-hover)');
        expect(getButtonCssVariable('primary', 'text', 'disabled')).toBe('var(--fd-button-primary-text-disabled)');
    });
});
