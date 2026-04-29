import { HttpClient } from '@angular/common/http';
import { DOCUMENT } from '@angular/common';
import { inject, Injectable } from '@angular/core';
import { TranslateLoader, type TranslationObject } from '@ngx-translate/core';
import { catchError, forkJoin, map, Observable, of, shareReplay } from 'rxjs';
import { environment } from '../../environments/environment';

type TranslationDictionary = TranslationObject;
type TranslationBundle = 'core' | 'public' | 'app';

@Injectable()
export class FoodDiaryTranslationLoader extends TranslateLoader {
    private readonly http = inject(HttpClient);
    private readonly document = inject(DOCUMENT);
    private readonly cache = new Map<string, Observable<TranslationDictionary>>();

    public getTranslation(lang: string): Observable<TranslationDictionary> {
        const bundles: readonly TranslationBundle[] = this.isPublicPath(this.document.location.pathname)
            ? ['core', 'public']
            : ['core', 'app'];
        return this.loadBundles(lang, bundles);
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

    private isPublicPath(pathname: string): boolean {
        const normalizedPath = pathname.split(/[?#]/u, 1)[0].toLowerCase();
        return (
            normalizedPath === '/' ||
            normalizedPath.startsWith('/auth') ||
            normalizedPath === '/privacy-policy' ||
            normalizedPath === '/food-diary' ||
            normalizedPath === '/calorie-counter' ||
            normalizedPath === '/meal-planner' ||
            normalizedPath === '/macro-tracker' ||
            normalizedPath === '/intermittent-fasting' ||
            normalizedPath === '/meal-tracker' ||
            normalizedPath === '/weight-loss-app' ||
            normalizedPath === '/dietologist-collaboration' ||
            normalizedPath === '/nutrition-planner' ||
            normalizedPath === '/weight-tracker' ||
            normalizedPath === '/body-progress-tracker' ||
            normalizedPath === '/shopping-list-for-meal-planning'
        );
    }
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
