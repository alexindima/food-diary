import { describe, expect, it } from 'vitest';

import {
    buildDefaultHistoryCustomRange,
    buildHistoryFiltersForRange,
    calculateHistoryRangeDates,
    formatHistoryDateInput,
    isHistoryRange,
    normalizeEndOfHistoryDay,
    normalizeStartOfHistoryDay,
} from './history-range.utils';

const NOW = new Date('2026-05-15T12:00:00Z');
const CONFIG = {
    entriesLimitMax: 500,
    entriesLimitPerDay: 5,
    monthQuantizationDays: 3,
    yearQuantizationDays: 14,
    customQuantizationDivisor: 12,
};

describe('history-range.utils', () => {
    it('validates supported range values', () => {
        expect(isHistoryRange('week')).toBe(true);
        expect(isHistoryRange('month')).toBe(true);
        expect(isHistoryRange('year')).toBe(true);
        expect(isHistoryRange('custom')).toBe(true);
        expect(isHistoryRange('quarter')).toBe(false);
    });

    it('calculates preset ranges from the current date', () => {
        expect(calculateHistoryRangeDates('week', null, NOW).start.toISOString()).toBe('2026-05-08T12:00:00.000Z');
        expect(calculateHistoryRangeDates('month', null, NOW).start.toISOString()).toBe('2026-04-15T12:00:00.000Z');
        expect(calculateHistoryRangeDates('year', null, NOW).start.toISOString()).toBe('2025-05-15T12:00:00.000Z');
    });

    it('uses custom dates when provided', () => {
        const range = calculateHistoryRangeDates(
            'custom',
            { start: new Date('2026-01-01T00:00:00Z'), end: new Date('2026-02-01T00:00:00Z') },
            NOW,
        );

        expect(range.start.toISOString()).toBe('2026-01-01T00:00:00.000Z');
        expect(range.end.toISOString()).toBe('2026-02-01T00:00:00.000Z');
    });

    it('builds normalized filters and summary quantization', () => {
        const filters = buildHistoryFiltersForRange(
            'custom',
            {
                start: new Date('2026-05-01T15:00:00Z'),
                end: new Date('2026-05-15T10:00:00Z'),
            },
            CONFIG,
        );

        expect(filters.entriesParams).toEqual({
            dateFrom: '2026-05-01T00:00:00.000Z',
            dateTo: '2026-05-15T23:59:59.999Z',
            sort: 'desc',
            limit: 75,
        });
        expect(filters.summaryParams.quantizationDays).toBe(1);
        expect(filters.rangeKey).toBe('2026-05-01T00:00:00.000Z_2026-05-15T23:59:59.999Z');
    });

    it('normalizes day boundaries and date input format', () => {
        const date = new Date('2026-05-15T12:34:56Z');

        expect(normalizeStartOfHistoryDay(date).toISOString()).toBe('2026-05-15T00:00:00.000Z');
        expect(normalizeEndOfHistoryDay(date).toISOString()).toBe('2026-05-15T23:59:59.999Z');
        expect(formatHistoryDateInput(date)).toBe('2026-05-15');
    });

    it('builds default custom range around now', () => {
        const range = buildDefaultHistoryCustomRange(NOW);

        expect(range.start.toISOString()).toBe('2026-04-15T12:00:00.000Z');
        expect(range.end.toISOString()).toBe('2026-05-15T12:00:00.000Z');
    });
});
