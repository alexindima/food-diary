import { describe, expect, it } from 'vitest';

import type { MarketingAttributionSummary } from '../models/admin-acquisition.data';
import type { MarketingAttributionRange } from '../models/admin-acquisition-range';
import { acquisitionTrend } from './admin-acquisition-trend';

describe('acquisition calendar trend', () => {
    it('uses the same calendar labels for recorded and zero-filled days across leap day', () => {
        const report = createReport('2024-02-28T00:00:00Z', '2024-03-03T00:00:00Z', [
            { date: '2024-02-29T00:00:00Z', visits: 3, signups: 1, premiumStarts: 0 },
            { date: '2024-03-01T00:00:00Z', visits: 5, signups: 2, premiumStarts: 1 },
        ]);

        expect(acquisitionTrend(report)).toEqual([
            { date: '2024-02-28', visits: 0, signups: 0, premiumStarts: 0 },
            { date: '2024-02-29', visits: 3, signups: 1, premiumStarts: 0 },
            { date: '2024-03-01', visits: 5, signups: 2, premiumStarts: 1 },
            { date: '2024-03-02', visits: 0, signups: 0, premiumStarts: 0 },
        ]);
        expect(report.byDay[0].date).toBe('2024-02-29T00:00:00Z');
    });

    it('starts all-time charts at the first retained day while preserving a midnight bucket', () => {
        const report = createReport('1970-01-01T00:00:00Z', '2026-01-02T00:00:00Z', [
            { date: '2025-12-31T00:00:00Z', visits: 2, signups: 0, premiumStarts: 0 },
            { date: '2026-01-01T00:00:00Z', visits: 1, signups: 1, premiumStarts: 0 },
        ]);

        expect(acquisitionTrend(report)).toEqual([
            { date: '2025-12-31', visits: 2, signups: 0, premiumStarts: 0 },
            { date: '2026-01-01', visits: 1, signups: 1, premiumStarts: 0 },
        ]);
    });
});

function createReport(fromUtc: string, toUtc: string, byDay: MarketingAttributionRange['byDay']): MarketingAttributionRange {
    const summary: MarketingAttributionSummary = {
        windowHours: 0,
        generatedAtUtc: toUtc,
        events: 0,
        visits: 0,
        signups: 0,
        premiumStarts: 0,
        anonymousVisitors: 0,
        sessions: 0,
        attributedEvents: 0,
        organicEvents: 0,
        attributedVisits: 0,
        organicVisits: 0,
        signupRatePercent: 0,
        premiumRatePercent: 0,
        lastEventAtUtc: null,
        topCampaigns: [],
        topSources: [],
        recentEvents: [],
    };
    return { fromUtc, toUtc, previousFromUtc: fromUtc, current: summary, previous: summary, byDay, eventTotal: 0 };
}
