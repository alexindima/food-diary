import { afterEveryRender, getDebugNode } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it, onTestFinished, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../src/testing/translate-testing.module';
import { FdUiCalendarComponent } from '../calendar/fd-ui-calendar';
import { FdUiDatePickerButtonComponent } from './fd-ui-date-picker-button';

const TEST_YEAR = 2026;
const MARCH_INDEX = 2;
const SELECTED_DAY = 8;

async function createDatePickerAsync(): Promise<{ fixture: ComponentFixture<FdUiDatePickerButtonComponent>; trigger: HTMLButtonElement }> {
    await TestBed.configureTestingModule({
        imports: [FdUiDatePickerButtonComponent],
        providers: [provideTranslateTesting()],
    }).compileComponents();
    const fixture = TestBed.createComponent(FdUiDatePickerButtonComponent);
    fixture.componentInstance.value.set(new Date(TEST_YEAR, MARCH_INDEX, SELECTED_DAY));
    fixture.componentRef.setInput('ariaLabel', 'Selected date');
    fixture.detectChanges();
    const trigger = (fixture.nativeElement as HTMLElement).querySelector('button');
    if (trigger === null) {
        throw new Error('Missing calendar trigger');
    }
    return { fixture, trigger };
}

describe('FdUiDatePickerButtonComponent focus', () => {
    let fixture: ComponentFixture<FdUiDatePickerButtonComponent>;
    let trigger: HTMLButtonElement;

    beforeEach(async () => {
        ({ fixture, trigger } = await createDatePickerAsync());
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

function pickerCalendar(): FdUiCalendarComponent {
    const element = document.querySelector('.fd-ui-date-picker-button__panel fd-ui-calendar');
    if (element === null) {
        throw new Error('Expected an open date picker');
    }
    const node = getDebugNode(element);
    if (node === null) {
        throw new Error('Expected the calendar component in the overlay');
    }
    return node.injector.get(FdUiCalendarComponent);
}

describe('FdUiDatePickerButtonComponent overlay behavior', () => {
    it('opens the current month for an empty value and restores focus to the trigger from Escape without an opener', async () => {
        vi.useFakeTimers({ toFake: ['Date'] });
        vi.setSystemTime(new Date('2026-05-20T12:00:00'));
        onTestFinished(() => {
            vi.useRealTimers();
        });
        const { fixture, trigger } = await createDatePickerAsync();
        fixture.componentRef.setInput('value', null);
        fixture.detectChanges();
        expect(document.activeElement).toBe(document.body);

        trigger.click();
        fixture.detectChanges();
        await fixture.whenStable();
        expect(document.activeElement?.getAttribute('data-date')).toBe('2026-05-20');

        const otherKey = new KeyboardEvent('keydown', { key: 'a', bubbles: true, cancelable: true });
        document.activeElement?.dispatchEvent(otherKey);
        fixture.detectChanges();
        expect(otherKey.defaultPrevented).toBe(false);
        expect(trigger.getAttribute('aria-expanded')).toBe('true');

        const escape = new KeyboardEvent('keydown', { key: 'Escape', bubbles: true, cancelable: true });
        document.activeElement?.dispatchEvent(escape);
        fixture.detectChanges();
        expect(escape.defaultPrevented).toBe(true);
        expect(trigger.getAttribute('aria-expanded')).toBe('false');
        expect(document.activeElement).toBe(trigger);
        expect(fixture.componentInstance.value()).toBeNull();
    });

    it('retains the navigated month on reopening and selects a date through the rendered grid', async () => {
        const { fixture, trigger } = await createDatePickerAsync();
        const selection = vi.fn();
        fixture.componentInstance.value.subscribe(selection);
        trigger.click();
        fixture.detectChanges();
        const next = document.querySelector<HTMLButtonElement>('.fd-ui-date-picker-button__panel button[aria-label="CALENDAR.NEXT_MONTH"]');
        next?.click();
        fixture.detectChanges();
        expect(document.querySelector('.fd-ui-date-picker-button__panel .fd-ui-calendar__title')?.textContent.trim()).toBe('April 2026');

        trigger.click();
        fixture.detectChanges();
        expect(document.querySelector('.fd-ui-date-picker-button__panel .fd-ui-calendar__title')?.textContent.trim()).toBe('April 2026');
        document.querySelector<HTMLButtonElement>('.fd-ui-date-picker-button__panel [data-date="2026-04-05"]')?.click();
        fixture.detectChanges();
        expect(selection).toHaveBeenCalledExactlyOnceWith(new Date('2026-04-05T00:00:00'));
        expect(trigger.getAttribute('aria-expanded')).toBe('false');
    });

    it('keeps the stored date and remains open when the embedded calendar is reset', async () => {
        const { fixture, trigger } = await createDatePickerAsync();
        const selection = vi.fn();
        fixture.componentInstance.value.subscribe(selection);
        trigger.click();
        fixture.detectChanges();

        const calendar = pickerCalendar();
        calendar.displayMonth.set(null);
        calendar.value.set(null);
        fixture.detectChanges();

        expect(fixture.componentInstance.value()).toEqual(new Date(TEST_YEAR, MARCH_INDEX, SELECTED_DAY));
        expect(selection).not.toHaveBeenCalled();
        expect(trigger.getAttribute('aria-expanded')).toBe('true');
    });
});

describe('FdUiDatePickerButtonComponent canceled attachment', () => {
    it('keeps focus on the original launcher if its overlay is dismissed as it attaches', async () => {
        const { fixture, trigger } = await createDatePickerAsync();
        const dismissed = vi.fn();
        trigger.focus();
        const dismissFirstRender = TestBed.runInInjectionContext(() =>
            afterEveryRender({
                write: () => {
                    const backdrop = document.querySelector<HTMLElement>('.cdk-overlay-backdrop');
                    if (backdrop !== null && document.querySelector('.fd-ui-date-picker-button__panel') !== null) {
                        backdrop.click();
                        dismissed();
                    }
                },
            }),
        );
        onTestFinished(() => {
            dismissFirstRender.destroy();
        });

        trigger.click();
        fixture.detectChanges();
        await fixture.whenStable();
        fixture.detectChanges();

        expect(dismissed).toHaveBeenCalled();
        expect(trigger.getAttribute('aria-expanded')).toBe('false');
        expect(document.activeElement).toBe(trigger);
        expect(fixture.componentInstance.value()).toEqual(new Date(TEST_YEAR, MARCH_INDEX, SELECTED_DAY));
    });
});
