import { parseDateValue } from './local-date.utils';

export function formatGoalHistoryDates(
    start: string,
    end: string | null,
    locale: string,
): { startDate: string; endDate: string | null; dateRange: string } {
    const startValue = parseDateValue(start);
    const endValue = parseDateValue(end);
    const full = new Intl.DateTimeFormat(locale, { day: 'numeric', month: 'short', year: 'numeric' });
    const startDate = startValue === null ? '' : cleanDate(full.format(startValue));
    const endDate = endValue === null ? null : cleanDate(full.format(endValue));
    if (startValue === null || endValue === null || startDate === endDate) {
        return { startDate, endDate, dateRange: startDate };
    }
    const first =
        startValue.getFullYear() === endValue.getFullYear()
            ? cleanDate(new Intl.DateTimeFormat(locale, { day: 'numeric', month: 'short' }).format(startValue))
            : startDate;
    return { startDate, endDate, dateRange: `${first} — ${endDate}` };
}

function cleanDate(value: string): string {
    return value.replace(/\s+г\.$/u, '').trim();
}
