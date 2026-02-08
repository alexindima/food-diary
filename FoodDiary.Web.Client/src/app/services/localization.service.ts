import { inject, Injectable } from '@angular/core';
import { DOCUMENT } from '@angular/common';
import { TranslateService } from '@ngx-translate/core';
import { firstValueFrom } from 'rxjs';
import { MeasurementUnit } from '../types/product.data';

@Injectable()
export class LocalizationService {
    private readonly translateService = inject(TranslateService);
    private readonly document = inject(DOCUMENT);
    private readonly storageKey = 'fd_language';

    public constructor() {
        this.translateService.onLangChange.subscribe(event => {
            const normalized = this.normalizeLanguage(event.lang);
            this.persistLanguage(normalized);
            this.setDocumentLang(normalized);
        });
    }

    public initializeLocalization(): Promise<void> {
        this.translateService.addLangs(['en', 'ru']);
        this.translateService.setDefaultLang('en');

        const browserLang = this.translateService.getBrowserLang();
        const storedLang = this.getStoredLanguage();
        const normalizedLang = this.normalizeLanguage(storedLang ?? browserLang);

        return firstValueFrom(this.translateService.use(normalizedLang)).then(() => void 0);
    }

    public applyLanguagePreference(language: string | null | undefined): Promise<void> {
        const normalized = this.normalizeLanguage(language);
        if (!normalized) {
            return Promise.resolve();
        }

        const current = this.translateService.currentLang || this.translateService.getDefaultLang();
        if (current === normalized) {
            this.persistLanguage(normalized);
            this.setDocumentLang(normalized);
            return Promise.resolve();
        }

        return firstValueFrom(this.translateService.use(normalized)).then(() => void 0);
    }

    public getCurrentLanguage(): string {
        const current = this.translateService.currentLang || this.translateService.getDefaultLang();
        return this.normalizeLanguage(current);
    }

    public clearStoredLanguage(): void {
        localStorage.removeItem(this.storageKey);
    }

    public getServingUnitName(unit: MeasurementUnit): string {
        return this.translateService.instant(`PRODUCT_MANAGE.DEFAULT_SERVING_UNITS.${unit}`);
    }

    private normalizeLanguage(lang?: string | null): string {
        if (!lang) {
            return 'en';
        }

        const lower = lang.toLowerCase();
        if (lower.startsWith('ru')) {
            return 'ru';
        }

        if (lower.startsWith('en')) {
            return 'en';
        }

        return 'en';
    }

    private persistLanguage(lang: string): void {
        localStorage.setItem(this.storageKey, lang);
    }

    private getStoredLanguage(): string | null {
        const value = localStorage.getItem(this.storageKey);
        if (!value || value === 'undefined' || value === 'null') {
            return null;
        }
        return value;
    }

    private setDocumentLang(lang: string): void {
        this.document.documentElement.setAttribute('lang', lang);
    }
}
