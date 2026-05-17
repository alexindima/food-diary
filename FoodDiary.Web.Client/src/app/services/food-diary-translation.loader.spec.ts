import { DOCUMENT } from '@angular/common';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { describe, expect, it } from 'vitest';

import { environment } from '../../environments/environment';
import { FoodDiaryTranslationLoader } from './food-diary-translation.loader';

const LANGUAGE = 'en';
const BUILD_VERSION = environment.buildVersion ?? 'dev';

describe('FoodDiaryTranslationLoader routes', () => {
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
        flushBundle(httpMock, 'app', { APP: true });
        await expect(firstLoad).resolves.toEqual({ APP: true });
        httpMock.verify();

        await expect(firstValueFrom(loader.loadApplicationTranslations(LANGUAGE))).resolves.toEqual({ APP: true });
        httpMock.verify();
    });

    it('uses empty dictionary when a bundle request fails', async () => {
        const { loader, httpMock } = setup('/dashboard');
        const resultPromise = firstValueFrom(loader.loadApplicationTranslations(LANGUAGE));

        const req = httpMock.expectOne(bundleUrl('app'));
        req.flush('missing', { status: 404, statusText: 'Not Found' });

        await expect(resultPromise).resolves.toEqual({});
        httpMock.verify();
    });
});

function setup(pathname: string): { loader: FoodDiaryTranslationLoader; httpMock: HttpTestingController } {
    TestBed.configureTestingModule({
        providers: [
            FoodDiaryTranslationLoader,
            provideHttpClient(),
            provideHttpClientTesting(),
            {
                provide: DOCUMENT,
                useValue: {
                    location: { pathname },
                },
            },
        ],
    });

    return {
        loader: TestBed.inject(FoodDiaryTranslationLoader),
        httpMock: TestBed.inject(HttpTestingController),
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
