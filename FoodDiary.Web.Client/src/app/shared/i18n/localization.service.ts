import { DOCUMENT } from '@angular/common';
import { DestroyRef, inject, Service } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router } from '@angular/router';
import { TranslateService, type TranslationObject } from '@ngx-translate/core';
import { filter, firstValueFrom, type Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { isPublicMarketingPath, publicSiteLocale } from '../../config/public-site-paths';
import { BrowserStorageService } from '../platform/browser-storage.service';
import { BrowserWindowService } from '../platform/browser-window.service';
import { FoodDiaryTranslationLoader } from './food-diary-translation.loader';
import { resolveTranslateLanguage } from './translate-language.utils';

@Service()
export class LocalizationService {
    private readonly translateService = inject(TranslateService);
    private readonly document = inject(DOCUMENT);
    private readonly destroyRef = inject(DestroyRef);
    private readonly router = inject(Router);
    private readonly storage = inject(BrowserStorageService);
    private readonly browserWindow = inject(BrowserWindowService);
    private readonly translationLoader = inject(FoodDiaryTranslationLoader);
    private readonly russianDefaultHosts = new Set(environment.russianDefaultHosts);
    private readonly storageKey = 'fd_language';
    private readonly loadedTranslationKeys = new Set<string>();
    private readonly translationRequests = new Map<string, Promise<void>>();

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
        const initialPath = this.browserWindow.getPathname() ?? this.document.location.pathname;
        const routeLang = typeof initialPath === 'string' && isPublicMarketingPath(initialPath) ? publicSiteLocale(initialPath) : null;
        const normalizedLang = this.normalizeLanguage(routeLang ?? storedLang ?? domainLang ?? browserLang);

        return firstValueFrom(this.translateService.use(normalizedLang)).then(translations => {
            if (typeof initialPath === 'string' && Object.keys(translations).length > 0) {
                const publicPath = this.translationLoader.isPublicRoute(initialPath) ? initialPath : undefined;
                this.loadedTranslationKeys.add(this.translationKey(normalizedLang, publicPath));
            }
            this.document.documentElement.setAttribute('data-i18n-ready', normalizedLang);
        });
    }

    public async applyLanguagePreferenceAsync(language: string | null | undefined): Promise<void> {
        const normalized = this.normalizeLanguage(language);
        if (normalized.length === 0) {
            return;
        }

        const current = resolveTranslateLanguage(this.translateService);
        if (current === normalized) {
            this.persistLanguage(normalized);
            this.setDocumentLang(normalized);
            return;
        }

        return firstValueFrom(this.translateService.use(normalized)).then(() => void 0);
    }

    public getCurrentLanguage(): string {
        return this.normalizeLanguage(resolveTranslateLanguage(this.translateService));
    }

    public async loadApplicationTranslationsAsync(): Promise<void> {
        const currentLang = this.getCurrentLanguage();
        return this.loadTranslationsOnceAsync(currentLang, this.translationKey(currentLang), () =>
            this.translationLoader.loadApplicationTranslations(currentLang),
        );
    }

    public async loadTranslationsForRouteAsync(pathname: string): Promise<void> {
        return this.translationLoader.isPublicRoute(pathname)
            ? this.loadRouteTranslationsAsync(pathname)
            : this.loadApplicationTranslationsAsync();
    }

    public async loadRouteTranslationsAsync(pathname: string): Promise<void> {
        const currentLang = this.getCurrentLanguage();
        return this.loadTranslationsOnceAsync(currentLang, this.translationKey(currentLang, pathname), () =>
            this.translationLoader.loadRouteTranslations(currentLang, pathname),
        );
    }

    public clearStoredLanguage(): void {
        this.storage.removeItem('local', this.storageKey);
    }

    private async loadTranslationsOnceAsync(language: string, key: string, load: () => Observable<TranslationObject>): Promise<void> {
        if (this.loadedTranslationKeys.has(key)) {
            return;
        }
        const pending = this.translationRequests.get(key);
        if (pending !== undefined) {
            return pending;
        }
        const request = firstValueFrom(load())
            .then(translations => {
                this.translateService.setTranslation(language, translations, true);
                this.loadedTranslationKeys.add(key);
            })
            .catch(() => {})
            .finally(() => {
                this.translationRequests.delete(key);
            });
        this.translationRequests.set(key, request);
        return request;
    }

    private translationKey(language: string, pathname?: string): string {
        return pathname === undefined ? `application:${language}` : `route:${language}:${this.normalizeRouteKey(pathname)}`;
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
        if (value === null || value === 'undefined' || value === 'null' || value.length === 0) {
            return null;
        }
        return value;
    }

    private getDomainDefaultLanguage(): string | null {
        const hostname = (this.browserWindow.getHostname() ?? this.document.location.hostname).toLowerCase();
        if (hostname.length === 0) {
            return null;
        }

        return this.russianDefaultHosts.has(hostname) ? 'ru' : null;
    }

    private setDocumentLang(lang: string): void {
        this.document.documentElement.setAttribute('lang', lang);
    }
}
