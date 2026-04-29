import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { DOCUMENT } from '@angular/common';
import { Router } from '@angular/router';
import { Subject, of } from 'rxjs';
import { TranslateService, LangChangeEvent } from '@ngx-translate/core';
import { LocalizationService } from './localization.service';
import { FoodDiaryTranslationLoader } from './food-diary-translation.loader';

describe('LocalizationService', () => {
    let service: LocalizationService;
    let translateSpy: any;
    let langChangeSubject: Subject<LangChangeEvent>;
    let mockDocument: Document;
    let translationLoaderSpy: any;
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
        } as any;

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

        translateSpy.use.mockReturnValue(of({} as any));
        translateSpy.getDefaultLang.mockReturnValue('en');
        translateSpy.getBrowserLang.mockReturnValue('en');
        translationLoaderSpy = {
            isPublicRoute: vi.fn().mockReturnValue(true),
            loadApplicationTranslations: vi.fn().mockReturnValue(of({ DASHBOARD: { TITLE: 'Dashboard' } })),
        } as any;
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
});
