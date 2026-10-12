const FORMAT_REFERENCE = new Date('2000-02-01T00:00:00');
const DATE_PART_COUNT = 3;
const FOUR_DIGIT_YEAR = /^\d{4}$/;

export function fdUiLocalizedDateHint(locale: string): string {
    return dateParts(locale)
        .map(part => {
            if (part.type === 'year') {
                return 'YYYY';
            }
            if (part.type === 'month') {
                return 'MM';
            }
            if (part.type === 'day') {
                return 'DD';
            }
            return part.value;
        })
        .join('');
}

export function fdUiParseLocalizedDate(value: string, locale: string): Date | null {
    const parts = dateParts(locale).filter(part => part.type !== 'literal');
    const values = value.trim().split(/[.\/\-\s]+/);
    if (values.length !== DATE_PART_COUNT || values.some(part => !/^\d+$/.test(part))) {
        return null;
    }
    const byType = new Map(parts.map((part, index) => [part.type, values[index]]));
    const yearText = byType.get('year') ?? '';
    if (!FOUR_DIGIT_YEAR.test(yearText)) {
        return null;
    }
    const year = Number(yearText);
    const month = Number(byType.get('month'));
    const day = Number(byType.get('day'));
    const date = new Date(FORMAT_REFERENCE);
    date.setFullYear(year, month - 1, day);
    return date.getFullYear() === year && date.getMonth() === month - 1 && date.getDate() === day ? date : null;
}

function dateParts(locale: string): Intl.DateTimeFormatPart[] {
    return new Intl.DateTimeFormat(locale, { day: '2-digit', month: '2-digit', year: 'numeric' }).formatToParts(FORMAT_REFERENCE);
}
