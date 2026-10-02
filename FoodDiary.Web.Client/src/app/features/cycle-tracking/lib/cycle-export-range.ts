export type CycleExportRange = { dateFrom: string; dateTo: string };

const DAY_MILLISECONDS = 86_400_000;
const MAX_RANGE_DAYS = 366;
const DATE_KEY_LENGTH = 10;

export function cycleExportRangeError(range: CycleExportRange, today: string): string | null {
    const from = dateNumber(range.dateFrom);
    const to = dateNumber(range.dateTo);
    const last = dateNumber(today);
    if (from === null || to === null || last === null) {
        return 'CYCLE_TRACKING.EXPORT_DATES_REQUIRED';
    }
    if (from > to) {
        return 'CYCLE_TRACKING.EXPORT_DATE_ORDER';
    }
    if (to > last) {
        return 'CYCLE_TRACKING.EXPORT_FUTURE_DATE';
    }
    return to - from > MAX_RANGE_DAYS ? 'CYCLE_TRACKING.EXPORT_RANGE_TOO_LONG' : null;
}

function dateNumber(value: string): number | null {
    if (!/^\d{4}-\d{2}-\d{2}$/.test(value)) {
        return null;
    }
    const date = new Date(`${value}T00:00:00Z`);
    return Number.isNaN(date.getTime()) || date.toISOString().slice(0, DATE_KEY_LENGTH) !== value
        ? null
        : date.getTime() / DAY_MILLISECONDS;
}
