import { TestBed } from '@angular/core/testing';
import { DOCUMENT } from '@angular/common';
import { Subject, of } from 'rxjs';
import { TranslateService, LangChangeEvent } from '@ngx-translate/core';
import { LocalizationService } from './localization.service';

describe('LocalizationService', () => {
    let service: LocalizationService;
    let translateSpy: jasmine.SpyObj<TranslateService>;
    let langChangeSubject: Subject<LangChangeEvent>;
    let mockDocument: Document;
    let currentLangValue: string;

    beforeEach(() => {
        langChangeSubject = new Subject<LangChangeEvent>();
        currentLangValue = 'en';

        translateSpy = jasmine.createSpyObj('TranslateService', [
            'addLangs',
            'setDefaultLang',
            'use',
            'getBrowserLang',
            'getDefaultLang',
            'instant',
        ]);

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

        translateSpy.use.and.returnValue(of({} as any));
        translateSpy.getDefaultLang.and.returnValue('en');
        translateSpy.getBrowserLang.and.returnValue('en');

        mockDocument = document;

        TestBed.configureTestingModule({
            providers: [
                LocalizationService,
                { provide: TranslateService, useValue: translateSpy },
                { provide: DOCUMENT, useValue: mockDocument },
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
        expect(translateSpy.setDefaultLang).toHaveBeenCalledWith('en');
        expect(translateSpy.use).toHaveBeenCalledWith('en');
    });

    it('should use stored language preference', async () => {
        localStorage.setItem('fd_language', 'ru');

        await service.initializeLocalization();

        expect(translateSpy.use).toHaveBeenCalledWith('ru');
    });

    it("should normalize unknown language to 'en'", async () => {
        translateSpy.getBrowserLang.and.returnValue('fr');

        await service.initializeLocalization();

        expect(translateSpy.use).toHaveBeenCalledWith('en');
    });

    it("should normalize 'ru' correctly", async () => {
        translateSpy.getBrowserLang.and.returnValue('ru');

        await service.initializeLocalization();

        expect(translateSpy.use).toHaveBeenCalledWith('ru');
    });

    it('should apply language preference', async () => {
        currentLangValue = 'en';
        translateSpy.getDefaultLang.and.returnValue('en');

        await service.applyLanguagePreference('ru');

        expect(translateSpy.use).toHaveBeenCalledWith('ru');
    });

    it('should not call use() if language is already set', async () => {
        currentLangValue = 'en';
        translateSpy.getDefaultLang.and.returnValue('en');
        translateSpy.use.calls.reset();

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
});
