export function measurementDateInputValue(value: string): string {
    const timeSeparator = value.indexOf('T');
    return timeSeparator === -1 ? value : value.slice(0, timeSeparator);
}

/** Measurements are calendar dates. The API encodes them as UTC midnight for compatibility. */
export function toMeasurementDateIso(value: string): string | null {
    const calendarDate = measurementDateInputValue(value);
    if (!/^\d{4}-\d{2}-\d{2}$/.test(calendarDate)) {
        return null;
    }
    const date = new Date(`${calendarDate}T00:00:00.000Z`);
    return Number.isFinite(date.getTime()) && measurementDateInputValue(date.toISOString()) === calendarDate ? date.toISOString() : null;
}

/** Date-picker ranges use local Date objects; stored measurement dates do not. */
export function measurementMonthRange(value: string): { start: Date; end: Date } | null {
    const iso = toMeasurementDateIso(value);
    if (iso === null) {
        return null;
    }
    const date = new Date(iso);
    return {
        start: new Date(date.getUTCFullYear(), date.getUTCMonth(), 1),
        end: new Date(date.getUTCFullYear(), date.getUTCMonth() + 1, 0),
    };
}
