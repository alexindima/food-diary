import { formatDateValue } from '../../../../../shared/lib/local-date.utils';

export function formatPremiumMediumDate(value: string | null | undefined, locale: string): string | null {
    return formatDateValue(value, locale, {
        dateStyle: 'medium',
    });
}
