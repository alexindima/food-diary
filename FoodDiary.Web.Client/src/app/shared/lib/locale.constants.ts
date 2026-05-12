export const APP_LANGUAGE_RU = 'ru';
export const APP_LOCALE_RU = 'ru-RU';
export const APP_LOCALE_EN = 'en-US';

export function resolveAppLocale(language: string | null | undefined): string {
    return language === APP_LANGUAGE_RU ? APP_LOCALE_RU : APP_LOCALE_EN;
}
