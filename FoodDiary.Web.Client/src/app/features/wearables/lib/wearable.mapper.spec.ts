import { describe, expect, it } from 'vitest';

import type { WearableConnection, WearableDailySummary } from '../models/wearable.data';
import { buildWearableMetrics, buildWearableProviderRows } from './wearable.mapper';

const ACTIVE_CONNECTION: WearableConnection = {
    provider: 'Fitbit',
    externalUserId: 'fitbit-user',
    isActive: true,
    lastSyncedAtUtc: '2026-05-15T00:00:00Z',
    connectedAtUtc: '2026-05-01T00:00:00Z',
};
const INACTIVE_CONNECTION: WearableConnection = {
    provider: 'Garmin',
    externalUserId: 'garmin-user',
    isActive: false,
    lastSyncedAtUtc: null,
    connectedAtUtc: '2026-05-01T00:00:00Z',
};
const SUMMARY: WearableDailySummary = {
    date: '2026-05-15',
    steps: 12_345,
    heartRate: 62,
    caloriesBurned: 450,
    activeMinutes: 54,
    sleepMinutes: 455,
};
const PROVIDER_COUNT = 4;

describe('wearable mapper', () => {
    it('builds provider rows with active connection only', () => {
        const rows = buildWearableProviderRows([ACTIVE_CONNECTION, INACTIVE_CONNECTION]);

        expect(rows).toHaveLength(PROVIDER_COUNT);
        expect(rows.find(row => row.id === 'Fitbit')?.connection).toEqual(ACTIVE_CONNECTION);
        expect(rows.find(row => row.id === 'Garmin')?.connection).toBeUndefined();
    });

    it('builds metrics for available daily summary values', () => {
        const metrics = buildWearableMetrics(SUMMARY);

        expect(metrics.map(metric => metric.key)).toEqual(['STEPS', 'HEART_RATE', 'CALORIES_BURNED', 'ACTIVE_MINUTES', 'SLEEP']);
        expect(metrics.find(metric => metric.key === 'SLEEP')).toMatchObject({
            value: 7.6,
            unit: 'h',
            numberFormat: '1.0-1',
        });
    });

    it('skips missing metrics', () => {
        const metrics = buildWearableMetrics({ ...SUMMARY, steps: null, sleepMinutes: null });

        expect(metrics.map(metric => metric.key)).toEqual(['HEART_RATE', 'CALORIES_BURNED', 'ACTIVE_MINUTES']);
    });

    it('returns empty metrics when summary is missing', () => {
        expect(buildWearableMetrics(null)).toEqual([]);
    });
});
