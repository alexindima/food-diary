import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';

import { provideTranslateTesting } from '../../../../../src/testing/translate-testing.module';
import { FdUiDatePickerButtonComponent } from './fd-ui-date-picker-button';

const TEST_YEAR = 2026;
const MARCH_INDEX = 2;
const SELECTED_DAY = 8;

describe('FdUiDatePickerButtonComponent focus', () => {
    let fixture: ComponentFixture<FdUiDatePickerButtonComponent>;
    let trigger: HTMLButtonElement;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [FdUiDatePickerButtonComponent],
            providers: [provideTranslateTesting()],
        }).compileComponents();
        fixture = TestBed.createComponent(FdUiDatePickerButtonComponent);
        fixture.componentInstance.value.set(new Date(TEST_YEAR, MARCH_INDEX, SELECTED_DAY));
        fixture.componentRef.setInput('ariaLabel', 'Selected date');
        fixture.detectChanges();
        const button = (fixture.nativeElement as HTMLElement).querySelector('button');
        if (button === null) {
            throw new Error('Missing calendar trigger');
        }
        trigger = button;
    });

    async function openAsync(): Promise<HTMLButtonElement> {
        trigger.focus();
        trigger.click();
        fixture.detectChanges();
        await fixture.whenStable();
        const day = document.querySelector<HTMLButtonElement>('.fd-ui-date-picker-button__panel [data-date="2026-03-08"]');
        if (day === null) {
            throw new Error('Missing selected day');
        }
        return day;
    }

    it('moves focus to the selected day when the overlay opens', async () => {
        const day = await openAsync();
        expect(document.activeElement).toBe(day);
        expect(trigger.getAttribute('aria-expanded')).toBe('true');
    });

    it.each(['escape', 'selection'])('returns focus to its trigger after %s', async action => {
        const day = await openAsync();
        day.focus();
        if (action === 'escape') {
            fixture.componentInstance['onOverlayKeydown'](new KeyboardEvent('keydown', { key: 'Escape' }));
        } else {
            day.click();
        }
        fixture.detectChanges();
        expect(document.activeElement).toBe(trigger);
        expect(trigger.getAttribute('aria-expanded')).toBe('false');
    });

    it('preserves focus on an outside control when the overlay closes', async () => {
        await openAsync();
        const outside = document.createElement('button');
        document.body.append(outside);
        try {
            outside.focus();
            fixture.componentInstance['close']();
            fixture.detectChanges();
            expect(document.activeElement).toBe(outside);
        } finally {
            outside.remove();
        }
    });

    it('returns focus to the visible launcher that opened an indirect calendar action', async () => {
        const launcher = document.createElement('button');
        document.body.append(launcher);
        try {
            launcher.focus();
            fixture.componentInstance['open']();
            fixture.detectChanges();
            await fixture.whenStable();
            const day = document.querySelector<HTMLButtonElement>('.fd-ui-date-picker-button__panel [data-date="2026-03-08"]');
            if (day === null) {
                throw new Error('Missing calendar day');
            }
            day.focus();
            fixture.componentInstance['onOverlayKeydown'](new KeyboardEvent('keydown', { key: 'Escape' }));
            fixture.detectChanges();
            expect(document.activeElement).toBe(launcher);
        } finally {
            launcher.remove();
        }
    });
});
