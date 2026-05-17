import { DOCUMENT } from '@angular/common';
import { DestroyRef, inject, Injectable } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { filter, firstValueFrom } from 'rxjs';

import { environment } from '../../environments/environment';
import { BrowserStorageService } from './browser-storage.service';
import { FoodDiaryTranslationLoader } from './food-diary-translation.loader';

@Injectable({ providedIn: 'root' })
export class LocalizationService {
    private readonly translateService = inject(TranslateService);
    private readonly document = inject(DOCUMENT);
    private readonly destroyRef = inject(DestroyRef);
    private readonly router = inject(Router);
    private readonly storage = inject(BrowserStorageService);
    private readonly translationLoader = inject(FoodDiaryTranslationLoader);
    private readonly russianDefaultHosts = new Set(environment.russianDefaultHosts ?? []);
    private readonly storageKey = 'fd_language';
    private readonly applicationTranslationLanguages = new Set<string>();
    private readonly routeTranslationKeys = new Set<string>();

    public constructor() {
        this.translateService.onLangChange.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(event => {
            const normalized = this.normalizeLanguage(event.lang);
            this.persistLanguage(normalized);
            this.setDocumentLang(normalized);
        });

        this.router.events
            .pipe(
                filter((event): event is NavigationEnd => event instanceof NavigationEnd),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe(event => {
                void this.loadTranslationsForRouteAsync(event.urlAfterRedirects);
            });
    }

    public async initializeLocalizationAsync(): Promise<void> {
        this.translateService.addLangs(['en', 'ru']);

        const browserLang = this.translateService.getBrowserLang();
        const storedLang = this.getStoredLanguage();
        const domainLang = this.getDomainDefaultLanguage();
        const normalizedLang = this.normalizeLanguage(storedLang ?? domainLang ?? browserLang);

        return firstValueFrom(this.translateService.use(normalizedLang)).then(() => void 0);
    }

    public async applyLanguagePreferenceAsync(language: string | null | undefined): Promise<void> {
        const normalized = this.normalizeLanguage(language);
        if (normalized.length === 0) {
            return Promise.resolve();
        }

        const currentLang = this.translateService.getCurrentLang();
        const fallbackLang = this.translateService.getFallbackLang();
        const current = currentLang.length > 0 ? currentLang : fallbackLang;
        if (current === normalized) {
            this.persistLanguage(normalized);
            this.setDocumentLang(normalized);
            return Promise.resolve();
        }

        return firstValueFrom(this.translateService.use(normalized)).then(() => void 0);
    }

    public getCurrentLanguage(): string {
        const currentLang = this.translateService.getCurrentLang();
        const fallbackLang = this.translateService.getFallbackLang();
        const current = currentLang.length > 0 ? currentLang : fallbackLang;
        return this.normalizeLanguage(current);
    }

    public async loadApplicationTranslationsAsync(): Promise<void> {
        const currentLang = this.getCurrentLanguage();
        if (this.applicationTranslationLanguages.has(currentLang)) {
            return Promise.resolve();
        }

        return firstValueFrom(this.translationLoader.loadApplicationTranslations(currentLang)).then(translations => {
            this.translateService.setTranslation(currentLang, translations, true);
            this.applicationTranslationLanguages.add(currentLang);
        });
    }

    public async loadTranslationsForRouteAsync(pathname: string): Promise<void> {
        return this.translationLoader.isPublicRoute(pathname)
            ? this.loadRouteTranslationsAsync(pathname)
            : this.loadApplicationTranslationsAsync();
    }

    public async loadRouteTranslationsAsync(pathname: string): Promise<void> {
        const currentLang = this.getCurrentLanguage();
        const normalizedPath = this.normalizeRouteKey(pathname);
        const cacheKey = `${currentLang}:${normalizedPath}`;
        if (this.routeTranslationKeys.has(cacheKey)) {
            return Promise.resolve();
        }

        return firstValueFrom(this.translationLoader.loadRouteTranslations(currentLang, pathname)).then(translations => {
            this.translateService.setTranslation(currentLang, translations, true);
            this.routeTranslationKeys.add(cacheKey);
        });
    }

    public clearStoredLanguage(): void {
        this.storage.removeItem('local', this.storageKey);
    }

    private normalizeLanguage(lang?: string | null): string {
        if (lang === null || lang === undefined || lang.length === 0) {
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

    private normalizeRouteKey(pathname: string): string {
        const withoutHash = pathname.split('#', 1)[0] ?? '/';
        const withoutQuery = withoutHash.split('?', 1)[0] ?? '/';

        return withoutQuery.length > 0 ? withoutQuery : '/';
    }

    private persistLanguage(lang: string): void {
        this.storage.setItem('local', this.storageKey, lang);
    }

    private getStoredLanguage(): string | null {
        const value = this.storage.getItem('local', this.storageKey);
        if (value === null || value.length === 0 || value === 'undefined' || value === 'null') {
            return null;
        }
        return value;
    }

    private getDomainDefaultLanguage(): string | null {
        const hostname = this.document.location.hostname.toLowerCase();
        if (hostname.length === 0) {
            return null;
        }

        return this.russianDefaultHosts.has(hostname) ? 'ru' : null;
    }

    private setDocumentLang(lang: string): void {
        this.document.documentElement.setAttribute('lang', lang);
    }
}
