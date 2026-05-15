export function formatPremiumMediumDate(value: string | null | undefined, locale: string): string | null {
    if (value === null || value === undefined || value.length === 0) {
        return null;
    }

    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
        return null;
    }

    return new Intl.DateTimeFormat(locale, {
        dateStyle: 'medium',
    }).format(date);
}
