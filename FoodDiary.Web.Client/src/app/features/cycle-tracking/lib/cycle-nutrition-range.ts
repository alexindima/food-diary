import { formatDateInputValue, normalizeStartOfUtcDay } from '../../../shared/lib/local-date.utils';

const MAX_SUMMARY_DAYS = 366;
const DATE_KEY_LENGTH = 10;

export function cycleNutritionRange(trackingStartDate: string, now: Date): { dateFrom: string; dateTo: string } {
    const dateTo = formatDateInputValue(now);
    const earliest = normalizeStartOfUtcDay(now);
    earliest.setUTCDate(earliest.getUTCDate() - (MAX_SUMMARY_DAYS - 1));
    const earliestDateKey = earliest.toISOString().slice(0, DATE_KEY_LENGTH);
    return { dateFrom: trackingStartDate < earliestDateKey ? earliestDateKey : trackingStartDate, dateTo };
}
