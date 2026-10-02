import { parseCalendarDateUtc } from '../../../shared/lib/local-date.utils';

const MAX_SUMMARY_DAYS = 366;
const DATE_KEY_LENGTH = 10;

export function cycleNutritionRange(trackingStartDate: string, today: string): { dateFrom: string; dateTo: string } {
    const earliest = parseCalendarDateUtc(today);
    if (earliest === null) {
        return { dateFrom: trackingStartDate, dateTo: today };
    }
    earliest.setUTCDate(earliest.getUTCDate() - (MAX_SUMMARY_DAYS - 1));
    const earliestDateKey = earliest.toISOString().slice(0, DATE_KEY_LENGTH);
    return { dateFrom: trackingStartDate < earliestDateKey ? earliestDateKey : trackingStartDate, dateTo: today };
}
