import { registerLocaleData } from '@angular/common';
import localeRu from '@angular/common/locales/ru';
import { ChangeDetectionStrategy, Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../testing/translate-testing.module';
import { injectCurrentLanguage } from './inject-current-language';
import { LocalizedDatePipe } from './localized-date.pipe';

type TranslateServiceStub = {
    getCurrentLang: ReturnType<typeof vi.fn<() => string>>;
};

const TEST_YEAR = 2026;
const JANUARY_INDEX = 0;
const TEST_DAY = 15;
const EXPECTED_CUSTOM_DATE = '2026-01-15';

describe('LocalizedDatePipe', () => {
    let pipe: LocalizedDatePipe;

    beforeEach(() => {
        const translateSpy: TranslateServiceStub = {
            getCurrentLang: vi.fn().mockReturnValue('en'),
        };

        TestBed.configureTestingModule({
            providers: [LocalizedDatePipe, { provide: TranslateService, useValue: translateSpy }],
        });

        pipe = TestBed.inject(LocalizedDatePipe);
    });

    it('should transform Date to formatted string', () => {
        const date = new Date(TEST_YEAR, JANUARY_INDEX, TEST_DAY);
        const result = pipe.transform(date);
        expect(result).toBeDefined();
        expect(typeof result).toBe('string');
        expect(result?.length).toBeGreaterThan(0);
    });

    it('formats stored calendar dates in UTC when requested', () => {
        expect(pipe.transform('2026-01-01T00:00:00Z', 'yyyy-MM-dd', 'UTC')).toBe('2026-01-01');
        expect(pipe.transform('2024-02-29T00:00:00Z', 'yyyy-MM-dd', 'UTC')).toBe('2024-02-29');
    });

    it('should return undefined for null input', () => {
        const result = pipe.transform(null);
        expect(result).toBeUndefined();
    });

    it('should return undefined for undefined input', () => {
        const result = pipe.transform(void 0);
        expect(result).toBeUndefined();
    });

    it("should use default pattern 'mediumDate'", () => {
        const date = new Date(TEST_YEAR, JANUARY_INDEX, TEST_DAY);
        const defaultResult = pipe.transform(date);
        const explicitResult = pipe.transform(date, 'mediumDate');
        expect(defaultResult).toBe(explicitResult);
    });

    it('should use custom pattern when provided', () => {
        const date = new Date(TEST_YEAR, JANUARY_INDEX, TEST_DAY);
        const result = pipe.transform(date, 'yyyy-MM-dd');
        expect(result).toBe(EXPECTED_CUSTOM_DATE);
    });
});

@Component({
    template: '<time [attr.datetime]="date">{{ date | localizedDate: "d MMMM y" : "UTC" : locale() }}</time>',
    imports: [LocalizedDatePipe],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
class LocalizedDateHostComponent {
    protected readonly date = '2026-08-09T00:00:00Z';
    protected readonly locale = injectCurrentLanguage();
}

describe('Mounted localized date', () => {
    it('updates an OnPush calendar date using its requested UTC day after changing language', async () => {
        registerLocaleData(localeRu);
        TestBed.configureTestingModule({
            imports: [LocalizedDateHostComponent],
            providers: [provideTranslateTesting()],
        });
        const translate = TestBed.inject(TranslateService);
        translate.use('en');
        const fixture = TestBed.createComponent(LocalizedDateHostComponent);
        fixture.autoDetectChanges();
        await fixture.whenStable();

        const date = (fixture.nativeElement as HTMLElement).querySelector('time');
        expect(date?.textContent).toBe('9 August 2026');

        translate.use('ru');
        await fixture.whenStable();

        expect(date?.textContent).toBe('9 августа 2026');
        expect(date?.getAttribute('datetime')).toBe('2026-08-09T00:00:00Z');
    });
});
