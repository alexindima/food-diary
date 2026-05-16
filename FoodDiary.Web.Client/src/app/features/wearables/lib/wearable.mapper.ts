import { MINUTES_PER_HOUR } from '../../../shared/lib/time.constants';
import type { WearableConnection, WearableDailySummary } from '../models/wearable.data';
import { WEARABLE_PROVIDER_CONFIGS } from './wearable.config';
import type { WearableMetric, WearableProviderConfig, WearableProviderRow } from './wearable.types';

const HOURS_PRECISION_FACTOR = 10;
const INTEGER_NUMBER_FORMAT = '1.0-0';
const HOURS_NUMBER_FORMAT = '1.0-1';

type WearableMetricConfig = {
    key: string;
    icon: string;
    value: number | null;
    unit: string;
    numberFormat: string;
};

export function buildWearableProviderRows(
    connections: WearableConnection[],
    providers: WearableProviderConfig[] = WEARABLE_PROVIDER_CONFIGS,
): WearableProviderRow[] {
    const activeConnections = new Map(
        connections.filter(connection => connection.isActive).map(connection => [connection.provider, connection]),
    );

    return providers.map(provider => ({
        ...provider,
        connection: activeConnections.get(provider.id),
    }));
}

export function buildWearableMetrics(summary: WearableDailySummary | null): WearableMetric[] {
    if (summary === null) {
        return [];
    }

    const metrics: WearableMetric[] = [];
    addMetric(metrics, { key: 'STEPS', icon: '\uD83D\uDEB6', value: summary.steps, unit: '', numberFormat: INTEGER_NUMBER_FORMAT });
    addMetric(metrics, {
        key: 'HEART_RATE',
        icon: '\u2764\uFE0F',
        value: summary.heartRate,
        unit: 'bpm',
        numberFormat: INTEGER_NUMBER_FORMAT,
    });
    addMetric(metrics, {
        key: 'CALORIES_BURNED',
        icon: '\uD83D\uDD25',
        value: summary.caloriesBurned,
        unit: 'kcal',
        numberFormat: INTEGER_NUMBER_FORMAT,
    });
    addMetric(metrics, {
        key: 'ACTIVE_MINUTES',
        icon: '\u26A1',
        value: summary.activeMinutes,
        unit: 'min',
        numberFormat: INTEGER_NUMBER_FORMAT,
    });

    if (summary.sleepMinutes !== null) {
        addMetric(metrics, {
            key: 'SLEEP',
            icon: '\uD83D\uDE34',
            value: Math.round((summary.sleepMinutes / MINUTES_PER_HOUR) * HOURS_PRECISION_FACTOR) / HOURS_PRECISION_FACTOR,
            unit: 'h',
            numberFormat: HOURS_NUMBER_FORMAT,
        });
    }

    return metrics;
}

function addMetric(metrics: WearableMetric[], config: WearableMetricConfig): void {
    const { key, icon, value, unit, numberFormat } = config;

    if (value === null) {
        return;
    }

    metrics.push({
        key,
        labelKey: `WEARABLES.${key}`,
        icon,
        value,
        unit,
        numberFormat,
    });
}
