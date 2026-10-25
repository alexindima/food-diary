import { DOCUMENT } from '@angular/common';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { makeStateKey, PLATFORM_ID, type StateKey, TransferState } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { describe, expect, it } from 'vitest';

import { environment } from '../../../environments/environment';
import { BrowserWindowService } from '../platform/browser-window.service';
import { FoodDiaryTranslationLoader } from './food-diary-translation.loader';
import { SERVER_TRANSLATIONS } from './server-translations.token';

const LANGUAGE = 'en';
const BUILD_VERSION = environment.buildVersion ?? 'dev';

describe('FoodDiaryTranslationLoader routes', () => {
    it('selects server bundles from the actual document route without transferring unrelated landing copy', async () => {
        TestBed.configureTestingModule({
            providers: [
                provideHttpClient(),
                provideHttpClientTesting(),
                { provide: PLATFORM_ID, useValue: 'server' },
                { provide: DOCUMENT, useValue: { location: { pathname: '/calorie-counter' } } },
                { provide: TransferState, useFactory: (): TransferState => new TransferState() },
                { provide: BrowserWindowService, useValue: { getPathname: (): null => null } },
                { provide: SERVER_TRANSLATIONS, useValue: { en: { core: {}, seo: { SEO: true }, landing: { UNUSED: true } } } },
            ],
        });
        await expect(firstValueFrom(TestBed.inject(FoodDiaryTranslationLoader).getTranslation('en'))).resolves.toEqual({ SEO: true });
        expect(TestBed.inject(TransferState).hasKey(makeStateKey('fd-i18n:en:landing'))).toBe(false);
        TestBed.inject(HttpTestingController).verify();
    });
    it('uses packaged translations on the server without fetching the public hostname', async () => {
        TestBed.configureTestingModule({
            providers: [
                provideHttpClient(),
                provideHttpClientTesting(),
                { provide: PLATFORM_ID, useValue: 'server' },
                {
                    provide: SERVER_TRANSLATIONS,
                    useValue: { ru: { core: {}, app: { PUBLIC_RECIPES: { CATALOG: 'Каталог рецептов' } }, landing: { UNUSED: true } } },
                },
            ],
        });
        const loader = TestBed.inject(FoodDiaryTranslationLoader);
        await expect(firstValueFrom(loader.loadRouteTranslations('ru', '/explore'))).resolves.toEqual({
            PUBLIC_RECIPES: { CATALOG: 'Каталог рецептов' },
        });
        const transferred = TestBed.inject(TransferState);
        expect(
            transferred.get(makeStateKey<Record<string, unknown>>('fd-i18n:ru:app'), {}),
            'Packaged server translations must reach browser transfer state',
        ).toEqual({
            PUBLIC_RECIPES: { CATALOG: 'Каталог рецептов' },
        });
        expect(transferred.hasKey(makeStateKey('fd-i18n:ru:landing'))).toBe(false);
        TestBed.inject(HttpTestingController).verify();
    });
    it('loads landing bundles for initial public route', async () => {
        const { loader, httpMock } = setup('/');
        const resultPromise = firstValueFrom(loader.getTranslation(LANGUAGE));

        flushBundle(httpMock, 'core', { APP: { TITLE: 'FoodDiary' } });
        flushBundle(httpMock, 'landing', { HERO: { TITLE: 'Track meals' } });

        await expect(resultPromise).resolves.toEqual({
            APP: { TITLE: 'FoodDiary' },
            HERO: { TITLE: 'Track meals' },
        });
        httpMock.verify();
    });

    it('loads privacy and seo bundles by route and detects public routes', async () => {
        const { loader, httpMock } = setup('/dashboard');
        const privacyPromise = firstValueFrom(loader.loadRouteTranslations(LANGUAGE, '/privacy-policy?ref=footer'));
        const seoPromise = firstValueFrom(loader.loadRouteTranslations(LANGUAGE, '/food-diary#top'));

        flushBundle(httpMock, 'core', {});
        flushBundle(httpMock, 'privacy', { PRIVACY: true });
        flushBundle(httpMock, 'seo', { SEO: true });

        await expect(privacyPromise).resolves.toEqual({ PRIVACY: true });
        await expect(seoPromise).resolves.toEqual({ SEO: true });
        expect(loader.isPublicRoute('/food-diary?utm=1')).toBe(true);
        expect(loader.isPublicRoute('/dashboard')).toBe(false);
        httpMock.verify();
    });
});

describe('FoodDiaryTranslationLoader merging and caching', () => {
    it('deep merges bundle translations and lets route bundle override core values', async () => {
        const { loader, httpMock } = setup('/dashboard');
        const resultPromise = firstValueFrom(loader.loadRouteTranslations(LANGUAGE, '/dashboard'));

        flushBundle(httpMock, 'core', { COMMON: { SAVE: 'Save', CANCEL: 'Cancel' } });
        flushBundle(httpMock, 'app', { COMMON: { SAVE: 'Save changes' } });

        await expect(resultPromise).resolves.toEqual({ COMMON: { SAVE: 'Save changes', CANCEL: 'Cancel' } });
        httpMock.verify();
    });

    it('caches loaded bundles by language and bundle name', async () => {
        const { loader, httpMock } = setup('/dashboard');

        const firstLoad = firstValueFrom(loader.loadApplicationTranslations(LANGUAGE));
        flushBundle(httpMock, 'core', {});
        flushBundle(httpMock, 'app', { APP: true });
        await expect(firstLoad).resolves.toEqual({ APP: true });
        httpMock.verify();

        await expect(firstValueFrom(loader.loadApplicationTranslations(LANGUAGE))).resolves.toEqual({ APP: true });
        httpMock.verify();
    });

    it('retries a failed bundle and retains successfully loaded core translations', async () => {
        const { loader, httpMock } = setup('/dashboard');
        const resultPromise = firstValueFrom(loader.loadApplicationTranslations(LANGUAGE));
        const failed = expect(resultPromise).rejects.toMatchObject({ status: 503 });
        flushBundle(httpMock, 'core', { COMMON: { SAVE: 'Save' } });

        const req = httpMock.expectOne(bundleUrl('app'));
        req.flush('unavailable', { status: 503, statusText: 'Unavailable' });

        await failed;
        const retry = firstValueFrom(loader.loadApplicationTranslations(LANGUAGE));
        flushBundle(httpMock, 'app', { DASHBOARD: { TITLE: 'Dashboard' } });
        await expect(retry).resolves.toEqual({ COMMON: { SAVE: 'Save' }, DASHBOARD: { TITLE: 'Dashboard' } });
        httpMock.verify();
    });

    it('uses transferred browser translations without a duplicate request', async () => {
        const { loader, httpMock, transferState } = setup('/dashboard', 'browser');
        transferState.set(translationStateKey('app'), { APP: true });
        transferState.set(translationStateKey('core'), {});

        await expect(firstValueFrom(loader.loadApplicationTranslations(LANGUAGE))).resolves.toEqual({ APP: true });
        expect(transferState.hasKey(translationStateKey('app'))).toBe(false);
        httpMock.verify();
    });

    it('stores server translations in transfer state', async () => {
        const { loader, httpMock, transferState } = setup('/dashboard', 'server');
        const resultPromise = firstValueFrom(loader.loadApplicationTranslations(LANGUAGE));

        flushBundle(httpMock, 'core', {});
        flushBundle(httpMock, 'app', { APP: true });

        await expect(resultPromise).resolves.toEqual({ APP: true });
        expect(transferState.get(translationStateKey('app'), {})).toEqual({ APP: true });
        httpMock.verify();
    });
});

describe('FoodDiaryTranslationLoader initial failure recovery', () => {
    it('recovers a failed initial core bundle during application loading without caching the empty fallback', async () => {
        const { loader, httpMock } = setup('/dashboard');
        const initial = firstValueFrom(loader.getTranslation(LANGUAGE));
        httpMock.expectOne(bundleUrl('core')).flush('offline', { status: 503, statusText: 'Unavailable' });
        flushBundle(httpMock, 'app', { DASHBOARD: { TITLE: 'Dashboard' } });
        await expect(initial).resolves.toEqual({});

        const retry = firstValueFrom(loader.loadApplicationTranslations(LANGUAGE));
        flushBundle(httpMock, 'core', { COMMON: { SAVE: 'Save' } });
        await expect(retry).resolves.toEqual({ COMMON: { SAVE: 'Save' }, DASHBOARD: { TITLE: 'Dashboard' } });
        httpMock.verify();
    });
});

function setup(
    pathname: string,
    platformId: 'browser' | 'server' = 'browser',
): { loader: FoodDiaryTranslationLoader; httpMock: HttpTestingController; transferState: TransferState } {
    TestBed.configureTestingModule({
        providers: [
            FoodDiaryTranslationLoader,
            provideHttpClient(),
            provideHttpClientTesting(),
            { provide: PLATFORM_ID, useValue: platformId },
            { provide: TransferState, useFactory: (): TransferState => new TransferState() },
            {
                provide: BrowserWindowService,
                useValue: {
                    getPathname: (): string => pathname,
                },
            },
        ],
    });

    return {
        loader: TestBed.inject(FoodDiaryTranslationLoader),
        httpMock: TestBed.inject(HttpTestingController),
        transferState: TestBed.inject(TransferState),
    };
}

function flushBundle(httpMock: HttpTestingController, bundle: string, body: Record<string, unknown>): void {
    const req = httpMock.expectOne(bundleUrl(bundle));
    expect(req.request.method).toBe('GET');
    req.flush(body);
}

function bundleUrl(bundle: string): string {
    return `./assets/i18n/${LANGUAGE}/${bundle}.json?v=${BUILD_VERSION}`;
}

function translationStateKey(bundle: string): StateKey<Record<string, unknown>> {
    return makeStateKey<Record<string, unknown>>(`fd-i18n:${LANGUAGE}:${bundle}`);
}
