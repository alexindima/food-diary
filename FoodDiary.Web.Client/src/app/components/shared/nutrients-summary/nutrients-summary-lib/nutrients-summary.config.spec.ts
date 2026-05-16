import { describe, expect, it } from 'vitest';

import { formatNutrientsSummaryTooltip, mergeNutrientsSummaryConfig } from './nutrients-summary.config';

const CUSTOM_COLUMN_LAYOUT = 480;
const DEFAULT_CHART_BLOCK_SIZE = 192;
const CUSTOM_FONT_SIZE = 14;
const DEFAULT_LINE_HEIGHT = 20;
const DEFAULT_COMMON_GAP = 16;
const TOOLTIP_RAW_VALUE = 12.345;

describe('mergeNutrientsSummaryConfig', () => {
    it('keeps nested defaults when only one nested value is overridden', () => {
        const config = mergeNutrientsSummaryConfig({
            styles: {
                charts: {
                    breakpoints: {
                        columnLayout: CUSTOM_COLUMN_LAYOUT,
                    },
                },
                info: {
                    lineStyles: {
                        nutrients: {
                            fontSize: CUSTOM_FONT_SIZE,
                        },
                    },
                },
            },
        });

        expect(config.styles.charts.breakpoints.columnLayout).toBe(CUSTOM_COLUMN_LAYOUT);
        expect(config.styles.charts.breakpoints.chartBlockSize).toBe(DEFAULT_CHART_BLOCK_SIZE);
        expect(config.styles.info.lineStyles.nutrients.fontSize).toBe(CUSTOM_FONT_SIZE);
        expect(config.styles.info.lineStyles.nutrients.lineHeight).toBe(DEFAULT_LINE_HEIGHT);
        expect(config.content.hidePieChart).toBe(false);
    });

    it('merges content config independently from style config', () => {
        const config = mergeNutrientsSummaryConfig({
            content: {
                hidePieChart: true,
            },
        });

        expect(config.content.hidePieChart).toBe(true);
        expect(config.styles.common.gap).toBe(DEFAULT_COMMON_GAP);
    });
});

describe('formatNutrientsSummaryTooltip', () => {
    it('rounds fractional nutrient values and appends grams label', () => {
        const tooltip = formatNutrientsSummaryTooltip(createTooltipItem('Proteins', TOOLTIP_RAW_VALUE), 'g');

        expect(tooltip).toBe('Proteins: 12.35 g');
    });

    it('falls back to zero when raw value is not numeric', () => {
        const tooltip = formatNutrientsSummaryTooltip(createTooltipItem('', 'not-a-number'), 'grams');

        expect(tooltip).toBe(': 0 grams');
    });
});

function createTooltipItem(label: string, raw: unknown): { label: string; raw: unknown } {
    return { label, raw };
}
