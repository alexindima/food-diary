import { describe, expect, it } from 'vitest';

import {
    buildDefaultWaistHistoryCustomRange,
    buildWaistHistoryFiltersForRange,
    calculateWaistHistoryRangeDates,
    formatWaistHistoryDateInput,
    isWaistHistoryRange,
    normalizeEndOfDay,
    normalizeStartOfDay,
} from './waist-history-range.utils';

const NOW = new Date('2026-05-15T12:00:00Z');

describe('waist history range utils', () => {
    it('validates supported range values', () => {
        expect(isWaistHistoryRange('week')).toBe(true);
        expect(isWaistHistoryRange('month')).toBe(true);
        expect(isWaistHistoryRange('year')).toBe(true);
        expect(isWaistHistoryRange('custom')).toBe(true);
        expect(isWaistHistoryRange('quarter')).toBe(false);
    });

    it('calculates preset ranges from the current date', () => {
        expect(calculateWaistHistoryRangeDates('week', null, NOW).start.toISOString()).toBe('2026-05-08T12:00:00.000Z');
        expect(calculateWaistHistoryRangeDates('month', null, NOW).start.toISOString()).toBe('2026-04-15T12:00:00.000Z');
        expect(calculateWaistHistoryRangeDates('year', null, NOW).start.toISOString()).toBe('2025-05-15T12:00:00.000Z');
    });

    it('uses custom dates when provided', () => {
        const range = calculateWaistHistoryRangeDates(
            'custom',
            { start: new Date('2026-01-01T00:00:00Z'), end: new Date('2026-02-01T00:00:00Z') },
            NOW,
        );

        expect(range.start.toISOString()).toBe('2026-01-01T00:00:00.000Z');
        expect(range.end.toISOString()).toBe('2026-02-01T00:00:00.000Z');
    });

    it('builds normalized filters and summary quantization', () => {
        const filters = buildWaistHistoryFiltersForRange('custom', {
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
        expect(formatWaistHistoryDateInput(date)).toBe('2026-05-15');
    });

    it('builds default custom range around now', () => {
        const range = buildDefaultWaistHistoryCustomRange(NOW);

        expect(range.start.toISOString()).toBe('2026-04-15T12:00:00.000Z');
        expect(range.end.toISOString()).toBe('2026-05-15T12:00:00.000Z');
    });
});
