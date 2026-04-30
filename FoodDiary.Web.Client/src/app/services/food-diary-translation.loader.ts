import { HttpClient } from '@angular/common/http';
import { DOCUMENT } from '@angular/common';
import { inject, Injectable } from '@angular/core';
import { TranslateLoader, type TranslationObject } from '@ngx-translate/core';
import { catchError, forkJoin, map, Observable, of, shareReplay } from 'rxjs';
import { environment } from '../../environments/environment';

type TranslationDictionary = TranslationObject;
type TranslationBundle = 'core' | 'landing' | 'seo' | 'privacy' | 'app';

@Injectable()
export class FoodDiaryTranslationLoader extends TranslateLoader {
    private readonly http = inject(HttpClient);
    private readonly document = inject(DOCUMENT);
    private readonly cache = new Map<string, Observable<TranslationDictionary>>();

    public getTranslation(lang: string): Observable<TranslationDictionary> {
        return this.loadBundles(lang, this.getInitialBundles(this.document.location.pathname));
    }

    public loadApplicationTranslations(lang: string): Observable<TranslationDictionary> {
        return this.loadBundles(lang, ['app']);
    }

    public isPublicRoute(pathname: string): boolean {
        return this.isPublicPath(pathname);
    }

    private loadBundles(lang: string, bundles: readonly TranslationBundle[]): Observable<TranslationDictionary> {
        if (bundles.length === 0) {
            return of({});
        }

        return forkJoin(bundles.map(bundle => this.loadBundle(lang, bundle))).pipe(map(parts => mergeTranslations(parts)));
    }

    private loadBundle(lang: string, bundle: TranslationBundle): Observable<TranslationDictionary> {
        const key = `${lang}:${bundle}`;
        const cached = this.cache.get(key);
        if (cached) {
            return cached;
        }

        const url = `./assets/i18n/${lang}/${bundle}.json?v=${environment.buildVersion ?? 'dev'}`;
        const request = this.http.get<TranslationDictionary>(url).pipe(
            catchError(() => of({})),
            shareReplay({ bufferSize: 1, refCount: false }),
        );
        this.cache.set(key, request);
        return request;
    }

    private getInitialBundles(pathname: string): readonly TranslationBundle[] {
        const normalizedPath = normalizePath(pathname);
        if (this.isLandingPath(normalizedPath)) {
            return ['core', 'landing'];
        }

        if (normalizedPath === '/privacy-policy') {
            return ['core', 'privacy'];
        }

        if (this.isSeoPath(normalizedPath)) {
            return ['core', 'seo'];
        }

        return ['core', 'app'];
    }

    private isPublicPath(pathname: string): boolean {
        const normalizedPath = normalizePath(pathname);
        return this.isLandingPath(normalizedPath) || normalizedPath === '/privacy-policy' || this.isSeoPath(normalizedPath);
    }

    private isLandingPath(normalizedPath: string): boolean {
        return normalizedPath === '/' || normalizedPath.startsWith('/auth');
    }

    private isSeoPath(normalizedPath: string): boolean {
        return SEO_PATHS.has(normalizedPath);
    }
}

const SEO_PATHS = new Set([
    '/food-diary',
    '/calorie-counter',
    '/meal-planner',
    '/macro-tracker',
    '/intermittent-fasting',
    '/meal-tracker',
    '/weight-loss-app',
    '/dietologist-collaboration',
    '/nutrition-planner',
    '/weight-tracker',
    '/body-progress-tracker',
    '/shopping-list-for-meal-planning',
    '/nutrition-tracker',
    '/food-log',
    '/protein-tracker',
    '/meal-prep-planner',
]);

function normalizePath(pathname: string): string {
    return pathname.split(/[?#]/u, 1)[0].toLowerCase();
}

function mergeTranslations(parts: readonly TranslationDictionary[]): TranslationDictionary {
    return parts.reduce<TranslationDictionary>((result, part) => deepMerge(result, part), {});
}

function deepMerge(target: TranslationDictionary, source: TranslationDictionary): TranslationDictionary {
    const output: TranslationDictionary = { ...target };

    for (const [key, value] of Object.entries(source)) {
        const targetValue = output[key];
        output[key] = isDictionary(targetValue) && isDictionary(value) ? deepMerge(targetValue, value) : value;
    }

    return output;
}

function isDictionary(value: unknown): value is TranslationDictionary {
    return typeof value === 'object' && value !== null && !Array.isArray(value);
}
