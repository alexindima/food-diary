import { afterEach, describe, expect, it } from 'vitest';

import { CHART_COLORS } from './chart-colors';

describe('CHART_COLORS', () => {
    afterEach(() => {
        document.documentElement.style.removeProperty('--fd-color-chart-proteins');
        document.documentElement.style.removeProperty('--fd-color-primary-600');
    });

    it('should read colors from CSS variables', () => {
        document.documentElement.style.setProperty('--fd-color-chart-proteins', 'rgb(1, 2, 3)');
        document.documentElement.style.setProperty('--fd-color-primary-600', '#123456');

        expect(CHART_COLORS.proteins).toBe('rgb(1, 2, 3)');
        expect(CHART_COLORS.primaryLine).toBe('#123456');
    });

    it('should fall back when CSS variables are not defined', () => {
        expect(CHART_COLORS.proteins).toBe('#2d9cdb');
        expect(CHART_COLORS.primaryLine).toBe('#2563eb');
    });
});
