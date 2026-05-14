import { describe, expect, it } from 'vitest';

import {
    buildDefaultWeightHistoryCustomRange,
    buildWeightHistoryFiltersForRange,
    calculateWeightHistoryRangeDates,
    formatWeightHistoryDateInput,
    isWeightHistoryRange,
    normalizeEndOfDay,
    normalizeStartOfDay,
} from './weight-history-range.utils';

const NOW = new Date('2026-05-15T12:00:00Z');

describe('weight history range utils', () => {
    it('validates supported range values', () => {
        expect(isWeightHistoryRange('week')).toBe(true);
        expect(isWeightHistoryRange('month')).toBe(true);
        expect(isWeightHistoryRange('year')).toBe(true);
        expect(isWeightHistoryRange('custom')).toBe(true);
        expect(isWeightHistoryRange('quarter')).toBe(false);
    });

    it('calculates preset ranges from the current date', () => {
        expect(calculateWeightHistoryRangeDates('week', null, NOW).start.toISOString()).toBe('2026-05-08T12:00:00.000Z');
        expect(calculateWeightHistoryRangeDates('month', null, NOW).start.toISOString()).toBe('2026-04-15T12:00:00.000Z');
        expect(calculateWeightHistoryRangeDates('year', null, NOW).start.toISOString()).toBe('2025-05-15T12:00:00.000Z');
    });

    it('uses custom dates when provided', () => {
        const range = calculateWeightHistoryRangeDates(
            'custom',
            { start: new Date('2026-01-01T00:00:00Z'), end: new Date('2026-02-01T00:00:00Z') },
            NOW,
        );

        expect(range.start.toISOString()).toBe('2026-01-01T00:00:00.000Z');
        expect(range.end.toISOString()).toBe('2026-02-01T00:00:00.000Z');
    });

    it('builds normalized filters and summary quantization', () => {
        const filters = buildWeightHistoryFiltersForRange('custom', {
            start: new Date('2026-05-01T15:00:00Z'),
            end: new Date('2026-05-15T10:00:00Z'),
        });

        expect(filters.entriesParams).toEqual({
            dateFrom: '2026-05-01T00:00:00.000Z',
            dateTo: '2026-05-15T23:59:59.999Z',
            sort: 'desc',
            limit: 75,
        });
        expect(filters.summaryParams.quantizationDays).toBe(1);
    });

    it('normalizes day boundaries and date input format', () => {
        const date = new Date('2026-05-15T12:34:56Z');

        expect(normalizeStartOfDay(date).toISOString()).toBe('2026-05-15T00:00:00.000Z');
        expect(normalizeEndOfDay(date).toISOString()).toBe('2026-05-15T23:59:59.999Z');
        expect(formatWeightHistoryDateInput(date)).toBe('2026-05-15');
    });

    it('builds default custom range around now', () => {
        const range = buildDefaultWeightHistoryCustomRange(NOW);

        expect(range.start.toISOString()).toBe('2026-04-15T12:00:00.000Z');
        expect(range.end.toISOString()).toBe('2026-05-15T12:00:00.000Z');
    });
});
