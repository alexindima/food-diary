import type { TranslateService } from '@ngx-translate/core';

export function resolveTranslateLanguage(translateService: TranslateService): string {
    const currentLang = translateService.getCurrentLang();
    if (currentLang !== null && currentLang.length > 0) {
        return currentLang;
    }

    const fallbackLang = translateService.getFallbackLang();
    return fallbackLang !== null && fallbackLang.length > 0 ? fallbackLang : 'en';
}
