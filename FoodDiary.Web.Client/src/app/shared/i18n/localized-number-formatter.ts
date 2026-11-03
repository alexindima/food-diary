import { resolveAppLocale } from '../lib/locale.constants';

const MAX_FORMATTERS = 32;
const formatters = new Map<string, Intl.NumberFormat>();

export function getLocalizedNumberFormatter(
    language: string,
    minimumFractionDigits = 0,
    maximumFractionDigits = minimumFractionDigits,
): Intl.NumberFormat {
    const locale = resolveAppLocale(language);
    const key = `${locale}:${minimumFractionDigits}:${maximumFractionDigits}`;
    const cached = formatters.get(key);
    if (cached !== undefined) {
        return cached;
    }

    const formatter = new Intl.NumberFormat(locale, { minimumFractionDigits, maximumFractionDigits });
    if (formatters.size >= MAX_FORMATTERS) {
        const oldestKey = formatters.keys().next().value;
        if (oldestKey !== undefined) {
            formatters.delete(oldestKey);
        }
    }
    formatters.set(key, formatter);
    return formatter;
}
