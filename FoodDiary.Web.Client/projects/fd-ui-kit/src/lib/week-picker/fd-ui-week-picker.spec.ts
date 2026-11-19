import { OverlayContainer } from '@angular/cdk/overlay';
import { getDebugNode } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../src/testing/translate-testing.module';
import { FdUiCalendarComponent } from '../calendar/fd-ui-calendar';
import { FdUiWeekPickerComponent } from './fd-ui-week-picker';

const TEST_YEAR = 2026;
const AUGUST_INDEX = 7;
const CURRENT_WEEK_DAY = 10;
const PREVIOUS_WEEK_DAY = 3;
const NEXT_WEEK_DAY = 17;

describe('FdUiWeekPickerComponent', () => {
    let fixture: ComponentFixture<FdUiWeekPickerComponent>;
    let component: FdUiWeekPickerComponent;
    let overlayContainer: OverlayContainer;
    const host = (): HTMLElement => fixture.nativeElement as HTMLElement;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [FdUiWeekPickerComponent],
            providers: [provideTranslateTesting()],
        }).compileComponents();
        fixture = TestBed.createComponent(FdUiWeekPickerComponent);
        component = fixture.componentInstance;
        overlayContainer = TestBed.inject(OverlayContainer);
        component.value.set(new Date(TEST_YEAR, AUGUST_INDEX, CURRENT_WEEK_DAY));
        fixture.componentRef.setInput('min', new Date(TEST_YEAR, AUGUST_INDEX, PREVIOUS_WEEK_DAY));
        fixture.componentRef.setInput('max', new Date(TEST_YEAR, AUGUST_INDEX, CURRENT_WEEK_DAY));
        fixture.detectChanges();
    });

    it('normalizes values and navigates between weeks inside min and max', () => {
        component['selectPreviousWeek']();
        expect(component.value().getDate()).toBe(PREVIOUS_WEEK_DAY);

        component['selectNextWeek']();
        expect(component.value().getDate()).toBe(CURRENT_WEEK_DAY);

        component['selectNextWeek']();
        expect(component.value().getDate()).not.toBe(NEXT_WEEK_DAY);
    });

    it('disables navigation controls at range boundaries', () => {
        const buttons = host().querySelectorAll<HTMLButtonElement>('button');
        expect(buttons[0].disabled).toBe(false);
        expect(buttons[2].disabled).toBe(true);
    });

    it.each([
        ['en-US', /^August 10\s*[–-]\s*16$/],
        ['ru-RU', /^10\s*[–-]\s*16 августа$/],
    ])('formats a same-month range in %s date order', (locale, expected) => {
        fixture.componentRef.setInput('locale', locale);
        fixture.detectChanges();
        const label = host().querySelector('.fd-ui-week-picker__trigger-copy strong');
        expect(label?.textContent.trim()).toMatch(expected);
    });

    it('opens the reusable week calendar and closes after selection', () => {
        component['open']();
        fixture.detectChanges();

        expect(overlayContainer.getContainerElement().querySelector('.fd-ui-week-picker__panel')).not.toBeNull();

        component['onCalendarSelect'](new Date(TEST_YEAR, AUGUST_INDEX, PREVIOUS_WEEK_DAY));
        fixture.detectChanges();

        expect(component.value().getDate()).toBe(PREVIOUS_WEEK_DAY);
        expect(overlayContainer.getContainerElement().querySelector('.fd-ui-week-picker__panel')).toBeNull();
    });
});

describe('FdUiWeekPickerComponent overlay month and current week', () => {
    let fixture: ComponentFixture<FdUiWeekPickerComponent>;
    const pastWeek = new Date('2026-07-06T00:00:00');

    beforeEach(async () => {
        await TestBed.configureTestingModule({ providers: [provideTranslateTesting()] }).compileComponents();
        fixture = TestBed.createComponent(FdUiWeekPickerComponent);
        fixture.componentRef.setInput('value', pastWeek);
        fixture.componentRef.setInput('max', new Date('2026-08-10T00:00:00'));
        fixture.detectChanges();
    });

    function open(): HTMLElement {
        const trigger = (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>('button[aria-label="Choose a week"]');
        trigger?.click();
        fixture.detectChanges();
        const overlay = TestBed.inject(OverlayContainer).getContainerElement();
        const panel = overlay.querySelector<HTMLElement>('.fd-ui-week-picker__panel');
        if (panel === null) {
            throw new Error('Expected a rendered week picker panel');
        }
        return panel;
    }

    it('moves across months without selecting a week and closes with Escape after an unrelated key', () => {
        const panel = open();
        panel.querySelector<HTMLButtonElement>('button[aria-label="CALENDAR.NEXT_MONTH"]')?.click();
        fixture.detectChanges();
        expect(panel.querySelector('.fd-ui-calendar__title')?.textContent.trim()).toBe('August 2026');
        const cell = panel.querySelector<HTMLButtonElement>('[data-date="2026-08-10"]');
        cell?.focus();
        cell?.dispatchEvent(new KeyboardEvent('keydown', { key: 'a', bubbles: true }));
        fixture.detectChanges();
        expect(panel.isConnected).toBe(true);

        const escape = new KeyboardEvent('keydown', { key: 'Escape', bubbles: true, cancelable: true });
        cell?.dispatchEvent(escape);
        fixture.detectChanges();

        expect(panel.isConnected).toBe(false);
        expect(escape.defaultPrevented).toBe(true);
        expect(fixture.componentInstance.value()).toEqual(pastWeek);
    });

    it('returns to the current week from inside the calendar and emits a Monday', () => {
        const additional = TestBed.createComponent(FdUiWeekPickerComponent);
        additional.componentRef.setInput('value', pastWeek);
        additional.detectChanges();
        const host = additional.nativeElement as HTMLElement;
        const changed = vi.fn();
        additional.componentInstance.value.subscribe(changed);
        host.querySelector<HTMLButtonElement>('button[aria-label="Choose a week"]')?.click();
        additional.detectChanges();
        const overlay = TestBed.inject(OverlayContainer).getContainerElement();
        const returnButton = [...overlay.querySelectorAll('button')].find(button => button.textContent.includes('Return to current week'));
        expect(returnButton).toBeDefined();
        returnButton?.click();
        additional.detectChanges();

        expect(host.querySelector('small')?.textContent.trim()).toBe('Current week');
        expect(changed).toHaveBeenCalledExactlyOnceWith(additional.componentInstance.max());
        expect(additional.componentInstance.value().getDay()).toBe(1);
        expect(overlay.querySelector('.fd-ui-week-picker__panel')).toBeNull();
    });

    it('keeps the week selection and calendar open when the embedded model is reset', () => {
        const panel = open();
        const renderedCalendar = panel.querySelector('fd-ui-calendar');
        if (renderedCalendar === null) {
            throw new Error('Expected an embedded calendar');
        }
        const calendar = getDebugNode(renderedCalendar)?.injector.get(FdUiCalendarComponent);
        expect(calendar).toBeDefined();
        calendar?.displayMonth.set(null);
        calendar?.value.set(null);
        fixture.detectChanges();

        expect(panel.isConnected).toBe(true);
        expect(fixture.componentInstance.value()).toEqual(pastWeek);
    });
});
