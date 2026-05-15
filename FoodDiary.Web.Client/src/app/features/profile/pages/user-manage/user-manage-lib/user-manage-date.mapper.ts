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
    if (value === null || value === undefined || value.length === 0) {
        return null;
    }

    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
        return null;
    }

    return new Intl.DateTimeFormat(resolveAppLocale(language), options).format(date);
}
