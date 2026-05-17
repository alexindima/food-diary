import { formatDateInputValue, formatTimeInputValue } from '../../../shared/lib/local-date.utils';

export function getDateInputValue(date: Date): string {
    return formatDateInputValue(date);
}

export function getTimeInputValue(date: Date): string {
    return formatTimeInputValue(date);
}
