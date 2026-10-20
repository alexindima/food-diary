import { formatDateInputValue, formatTimeInputValue } from '../../../shared/lib/local-date.utils';

export function getDateInputValue(date: Date): string {
    return formatDateInputValue(date);
}

export function getTimeInputValue(date: Date): string {
    return formatTimeInputValue(date);
}

export function isExistingMealLocalDateTime(date: string, time: string): boolean {
    const instant = new Date(`${date}T${time}`);
    return Number.isFinite(instant.getTime()) && getDateInputValue(instant) === date && getTimeInputValue(instant) === time;
}
