import { DOCUMENT } from '@angular/common';
import { TestBed } from '@angular/core/testing';
import { NavigationEnd, Router } from '@angular/router';
import { type LangChangeEvent, TranslateService } from '@ngx-translate/core';
import { type Observable, of, Subject } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { FoodDiaryTranslationLoader } from './food-diary-translation.loader';
import { LocalizationService } from './localization.service';

type TranslateServiceMock = {
    addLangs: ReturnType<typeof vi.fn>;
    setDefaultLang: ReturnType<typeof vi.fn>;
    use: ReturnType<typeof vi.fn>;
    getBrowserLang: ReturnType<typeof vi.fn>;
    getDefaultLang: ReturnType<typeof vi.fn>;
    instant: ReturnType<typeof vi.fn>;
    setTranslation: ReturnType<typeof vi.fn>;
    currentLang: string;
    onLangChange: Observable<LangChangeEvent>;
};

type TranslationLoaderMock = {
    isPublicRoute: ReturnType<typeof vi.fn>;
    loadRouteTranslations: ReturnType<typeof vi.fn>;
    loadApplicationTranslations: ReturnType<typeof vi.fn>;
};

describe('LocalizationService', () => {
    let service: LocalizationService;
    let translateSpy: TranslateServiceMock;
    let langChangeSubject: Subject<LangChangeEvent>;
    let mockDocument: Document;
    let translationLoaderSpy: TranslationLoaderMock;
    let routerEventsSubject: Subject<unknown>;
    let documentLang: string | null;
    let currentLangValue: string;

    beforeEach(() => {
        langChangeSubject = new Subject<LangChangeEvent>();
        currentLangValue = 'en';

        translateSpy = {
            addLangs: vi.fn(),
            setDefaultLang: vi.fn(),
            use: vi.fn(),
            getBrowserLang: vi.fn(),
            getDefaultLang: vi.fn(),
            instant: vi.fn(),
            setTranslation: vi.fn(),
            currentLang: currentLangValue,
            onLangChange: langChangeSubject.asObservable(),
        };

        Object.defineProperty(translateSpy, 'onLangChange', {
            get: () => langChangeSubject.asObservable(),
            configurable: true,
        });

        Object.defineProperty(translateSpy, 'currentLang', {
            get: () => currentLangValue,
            set: (v: string) => {
                currentLangValue = v;
            },
            configurable: true,
        });

        translateSpy.use.mockReturnValue(of({}));
        translateSpy.getDefaultLang.mockReturnValue('en');
        translateSpy.getBrowserLang.mockReturnValue('en');
        translationLoaderSpy = {
            isPublicRoute: vi.fn().mockReturnValue(true),
            loadRouteTranslations: vi.fn().mockReturnValue(of({ SEO_PAGE: { TRUST_TITLE: 'Trust' } })),
            loadApplicationTranslations: vi.fn().mockReturnValue(of({ DASHBOARD: { TITLE: 'Dashboard' } })),
        };
        routerEventsSubject = new Subject<unknown>();

        documentLang = null;
        mockDocument = {
            location: { hostname: 'fooddiary.club' },
            documentElement: {
                setAttribute: vi.fn((name: string, value: string) => {
                    if (name === 'lang') {
                        documentLang = value;
                    }
                }),
                getAttribute: vi.fn((name: string) => (name === 'lang' ? documentLang : null)),
            },
        } as unknown as Document;

        TestBed.configureTestingModule({
            providers: [
                LocalizationService,
                { provide: TranslateService, useValue: translateSpy },
                { provide: DOCUMENT, useValue: mockDocument },
                { provide: FoodDiaryTranslationLoader, useValue: translationLoaderSpy },
                { provide: Router, useValue: { events: routerEventsSubject.asObservable() } },
            ],
        });

        service = TestBed.inject(LocalizationService);
    });

    afterEach(() => {
        localStorage.removeItem('fd_language');
    });

    it("should initialize with default language 'en'", async () => {
        await service.initializeLocalization();

        expect(translateSpy.addLangs).toHaveBeenCalledWith(['en', 'ru']);
        expect(translateSpy.use).toHaveBeenCalledWith('en');
    });

    it('should use stored language preference', async () => {
        localStorage.setItem('fd_language', 'ru');

        await service.initializeLocalization();

        expect(translateSpy.use).toHaveBeenCalledWith('ru');
    });

    it('should default to russian for the russian domain when no stored preference exists', async () => {
        (mockDocument.location as Location).hostname = 'xn--b1adbcbrouc8l.xn--p1ai';

        await service.initializeLocalization();

        expect(translateSpy.use).toHaveBeenCalledWith('ru');
    });

    it('should prefer stored language over domain default', async () => {
        localStorage.setItem('fd_language', 'en');
        (mockDocument.location as Location).hostname = 'xn--b1adbcbrouc8l.xn--p1ai';

        await service.initializeLocalization();

        expect(translateSpy.use).toHaveBeenCalledWith('en');
    });

    it("should normalize unknown language to 'en'", async () => {
        translateSpy.getBrowserLang.mockReturnValue('fr');

        await service.initializeLocalization();

        expect(translateSpy.use).toHaveBeenCalledWith('en');
    });

    it("should normalize 'ru' correctly", async () => {
        translateSpy.getBrowserLang.mockReturnValue('ru');

        await service.initializeLocalization();

        expect(translateSpy.use).toHaveBeenCalledWith('ru');
    });

    it('should apply language preference', async () => {
        currentLangValue = 'en';
        translateSpy.getDefaultLang.mockReturnValue('en');

        await service.applyLanguagePreference('ru');

        expect(translateSpy.use).toHaveBeenCalledWith('ru');
    });

    it('should not call use() if language is already set', async () => {
        currentLangValue = 'en';
        translateSpy.getDefaultLang.mockReturnValue('en');
        translateSpy.use.mockClear();

        await service.applyLanguagePreference('en');

        expect(translateSpy.use).not.toHaveBeenCalled();
    });

    it('should clear stored language', () => {
        localStorage.setItem('fd_language', 'ru');

        service.clearStoredLanguage();

        expect(localStorage.getItem('fd_language')).toBeNull();
    });

    it('should set document lang attribute on language change', () => {
        langChangeSubject.next({ lang: 'ru', translations: {} });

        expect(mockDocument.documentElement.getAttribute('lang')).toBe('ru');
    });

    it('should extend current language with application translations', async () => {
        await service.loadApplicationTranslations();

        expect(translationLoaderSpy.loadApplicationTranslations).toHaveBeenCalledWith('en');
        expect(translateSpy.setTranslation).toHaveBeenCalledWith('en', { DASHBOARD: { TITLE: 'Dashboard' } }, true);
    });

    it('should extend current language with public route translations', async () => {
        await service.loadRouteTranslations('/calorie-counter');

        expect(translationLoaderSpy.loadRouteTranslations).toHaveBeenCalledWith('en', '/calorie-counter');
        expect(translateSpy.setTranslation).toHaveBeenCalledWith('en', { SEO_PAGE: { TRUST_TITLE: 'Trust' } }, true);
    });

    it('should load public route translations on public navigation', async () => {
        routerEventsSubject.next(new NavigationEnd(1, '/calorie-counter', '/calorie-counter'));
        await Promise.resolve();

        expect(translationLoaderSpy.loadRouteTranslations).toHaveBeenCalledWith('en', '/calorie-counter');
    });

    it('should load application translations on private navigation', async () => {
        translationLoaderSpy.isPublicRoute.mockReturnValue(false);

        routerEventsSubject.next(new NavigationEnd(1, '/dashboard', '/dashboard'));
        await Promise.resolve();

        expect(translationLoaderSpy.loadApplicationTranslations).toHaveBeenCalledWith('en');
    });
});
