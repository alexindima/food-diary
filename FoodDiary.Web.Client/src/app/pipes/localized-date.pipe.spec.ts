import { TestBed } from '@angular/core/testing';
import { LangChangeEvent, TranslateService } from '@ngx-translate/core';
import { Observable, Subject } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { LocalizedDatePipe } from './localized-date.pipe';

type TranslateServiceStub = {
    getCurrentLang: ReturnType<typeof vi.fn<() => string>>;
    onLangChange: Observable<LangChangeEvent>;
};

describe('LocalizedDatePipe', () => {
    let pipe: LocalizedDatePipe;
    let onLangChange: Subject<LangChangeEvent>;

    beforeEach(() => {
        onLangChange = new Subject<LangChangeEvent>();

        const translateSpy: TranslateServiceStub = {
            getCurrentLang: vi.fn(),
            onLangChange: onLangChange.asObservable(),
        };
        translateSpy.getCurrentLang.mockReturnValue('en');

        TestBed.configureTestingModule({
            providers: [LocalizedDatePipe, { provide: TranslateService, useValue: translateSpy }],
        });

        pipe = TestBed.inject(LocalizedDatePipe);
    });

    it('should transform Date to formatted string', () => {
        const date = new Date(2026, 0, 15);
        const result = pipe.transform(date);
        expect(result).toBeDefined();
        expect(typeof result).toBe('string');
        expect(result!.length).toBeGreaterThan(0);
    });

    it('should return undefined for null input', () => {
        const result = pipe.transform(null);
        expect(result).toBeUndefined();
    });

    it('should return undefined for undefined input', () => {
        const result = pipe.transform(undefined);
        expect(result).toBeUndefined();
    });

    it("should use default pattern 'mediumDate'", () => {
        const date = new Date(2026, 0, 15);
        const defaultResult = pipe.transform(date);
        const explicitResult = pipe.transform(date, 'mediumDate');
        expect(defaultResult).toBe(explicitResult);
    });

    it('should use custom pattern when provided', () => {
        const date = new Date(2026, 0, 15);
        const result = pipe.transform(date, 'yyyy-MM-dd');
        expect(result).toBe('2026-01-15');
    });
});
