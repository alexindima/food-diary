import type { AggregatedStatisticsHttpResponse } from '../../../shared/api/sdk/generated/model/aggregated-statistics-http-response';
import { requireSdkFields, sdkInstant } from '../../../shared/api/sdk/sdk-response';
import type { AggregatedStatistics } from '../models/statistics.data';

export function aggregatedStatisticsFromSdk(response: AggregatedStatisticsHttpResponse): AggregatedStatistics {
    const value = requireSdkFields(response, [
        'dateFrom',
        'dateTo',
        'totalCalories',
        'averageProteins',
        'averageFats',
        'averageCarbs',
        'averageFiber',
        'totalProteins',
        'totalFats',
        'totalCarbs',
        'totalFiber',
    ]);
    return { ...value, dateFrom: sdkInstant(value.dateFrom), dateTo: sdkInstant(value.dateTo) };
}
