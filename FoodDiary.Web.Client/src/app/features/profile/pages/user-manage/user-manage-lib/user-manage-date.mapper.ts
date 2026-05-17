import { formatDateValue } from '../../../../../shared/lib/local-date.utils';
import { resolveAppLocale } from '../../../../../shared/lib/locale.constants';

export function formatUserManageDate(value: string | null | undefined, language: string): string | null {
    return formatUserManageDateValue(value, language, { dateStyle: 'medium' });
}

export function formatUserManageDateTime(value: string | null | undefined, language: string): string | null {
    return formatUserManageDateValue(value, language, {
        dateStyle: 'medium',
        timeStyle: 'short',
    });
}

function formatUserManageDateValue(value: string | null | undefined, language: string, options: Intl.DateTimeFormatOptions): string | null {
    return formatDateValue(value, resolveAppLocale(language), options);
}
