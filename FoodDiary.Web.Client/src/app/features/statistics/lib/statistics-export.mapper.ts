import type { ExportDiaryRequest, ExportFormat } from '../../meals/api/export.service';
import type { DateRange } from './statistics-data-mapper';
import { normalizeEndOfDay, normalizeStartOfDay } from './statistics-data-mapper';

export type StatisticsExportRequestConfig = {
    range: DateRange;
    format: ExportFormat;
    currentLang: string;
    fallbackLang: string | null;
    timeZoneOffsetMinutes: number;
};

export function buildStatisticsExportRequest(config: StatisticsExportRequestConfig): ExportDiaryRequest {
    const { range, format, currentLang, fallbackLang, timeZoneOffsetMinutes } = config;

    return {
        dateFrom: normalizeStartOfDay(range.start).toISOString(),
        dateTo: normalizeEndOfDay(range.end).toISOString(),
        format,
        locale: resolveExportLocale(currentLang, fallbackLang),
        timeZoneOffsetMinutes,
    };
}

function resolveExportLocale(currentLang: string, fallbackLang: string | null): string | undefined {
    if (currentLang.length > 0) {
        return currentLang;
    }

    if (fallbackLang !== null && fallbackLang.length > 0) {
        return fallbackLang;
    }

    return undefined;
}
