import { ADMIN_DATE_TEXT_LENGTH } from '../../../shared/period/admin-query';
import type { MarketingAttributionRange } from '../models/admin-acquisition-range';

const DAY_MS = 86_400_000;

export function acquisitionTrend(report: MarketingAttributionRange): MarketingAttributionRange['byDay'] {
    if (report.byDay.length === 0) {
        return [];
    }
    const values = new Map(report.byDay.map(day => [day.date.slice(0, ADMIN_DATE_TEXT_LENGTH), day]));
    const start = report.fromUtc.startsWith('1970-01-01') ? report.byDay[0].date : report.fromUtc;
    const result: MarketingAttributionRange['byDay'] = [];
    for (let day = Date.parse(start); day < Date.parse(report.toUtc); day += DAY_MS) {
        const date = new Date(day).toISOString().slice(0, ADMIN_DATE_TEXT_LENGTH);
        result.push(values.get(date) ?? { date, visits: 0, signups: 0, premiumStarts: 0 });
    }
    return result;
}
