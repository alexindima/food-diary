import { Component, signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { disabled, form, FormField } from '@angular/forms/signals';
import { By } from '@angular/platform-browser';
import { describe, expect, it, vi } from 'vitest';

import { FdUiDateInputComponent } from '../date-input/fd-ui-date-input';
import { FdUiDatetimeInputComponent } from './fd-ui-datetime-input';

@Component({
    imports: [FdUiDatetimeInputComponent, FormField],
    template: '<fd-ui-datetime-input [formField]="dateForm.date" />',
})
class DatetimeFormHost {
    public readonly model = signal({ date: '2024-02-29T23:45', disabled: false });
    public readonly dateForm = form(this.model, path => {
        disabled(path.date, { when: context => context.valueOf(path.disabled) });
    });
}

async function setupAsync(value: string | Date | null = '2024-02-29T23:45'): Promise<ComponentFixture<FdUiDatetimeInputComponent>> {
    await TestBed.configureTestingModule({ imports: [FdUiDatetimeInputComponent] }).compileComponents();
    const fixture = TestBed.createComponent(FdUiDatetimeInputComponent);
    fixture.componentRef.setInput('value', value);
    fixture.detectChanges();
    return fixture;
}

function dateInput(fixture: ComponentFixture<FdUiDatetimeInputComponent>): FdUiDateInputComponent {
    return fixture.debugElement.query(By.directive(FdUiDateInputComponent)).componentInstance as FdUiDateInputComponent;
}

function timeInput(fixture: ComponentFixture<FdUiDatetimeInputComponent>): HTMLInputElement {
    const input = (fixture.nativeElement as HTMLElement).querySelector<HTMLInputElement>('input[type="time"]');
    if (input === null) {
        throw new Error('Expected a time input.');
    }
    return input;
}

describe('FdUiDatetimeInputComponent model contract', () => {
    it.each(['2024-02-29T23:45', new Date('2024-02-29T23:45:00')])('displays the local date and time for %s', async value => {
        const fixture = await setupAsync(value);

        expect(dateInput(fixture).value()).toBe('2024-02-29');
        expect(timeInput(fixture).value).toBe('23:45');
    });

    it.each(['2024-03-10T03:15', '2024-11-03T01:30', '2025-01-01T00:05'])('keeps local boundary input %s', async value => {
        const fixture = await setupAsync(value);
        expect(dateInput(fixture).value()).toBe(value.slice(0, 'YYYY-MM-DD'.length));
        expect(timeInput(fixture).value).toBe(value.slice(-'HH:mm'.length));
        expect(fixture.componentInstance.value()).toBe(value);
    });

    it('does not emit user changes while applying an external value or reset', async () => {
        const fixture = await setupAsync();
        const changed = vi.fn();
        fixture.componentInstance.value.subscribe(changed);

        fixture.componentRef.setInput('value', '2025-01-01T00:15');
        fixture.detectChanges();
        expect(dateInput(fixture).value()).toBe('2025-01-01');
        expect(timeInput(fixture).value).toBe('00:15');

        fixture.componentRef.setInput('value', null);
        fixture.detectChanges();
        expect(dateInput(fixture).value()).toBeNull();
        expect(timeInput(fixture).value).toBe('');
        expect(changed).not.toHaveBeenCalled();
    });

    it('changes the date once while preserving the local time', async () => {
        const fixture = await setupAsync();
        const changed = vi.fn();
        fixture.componentInstance.value.subscribe(changed);

        dateInput(fixture)['onDateSelect'](new Date('2024-03-01T00:00:00'));
        fixture.detectChanges();

        expect(fixture.componentInstance.value()).toBe('2024-03-01T23:45');
        expect(changed).toHaveBeenCalledExactlyOnceWith('2024-03-01T23:45');
    });

    it('changes the time once while preserving the date', async () => {
        const fixture = await setupAsync();
        const changed = vi.fn();
        fixture.componentInstance.value.subscribe(changed);
        const input = timeInput(fixture);

        input.value = '00:05';
        input.dispatchEvent(new Event('input'));
        fixture.detectChanges();

        expect(fixture.componentInstance.value()).toBe('2024-02-29T00:05');
        expect(changed).toHaveBeenCalledExactlyOnceWith('2024-02-29T00:05');
    });
});

describe('FdUiDatetimeInputComponent time drafts', () => {
    it('keeps an incomplete time draft and uses the last valid time when the date changes', async () => {
        const fixture = await setupAsync();
        const component = fixture.componentInstance;
        const changed = vi.fn();
        component.value.subscribe(changed);

        component['onTimeInput']('12:');
        fixture.detectChanges();
        expect(component['timeValue']()).toBe('12:');
        expect(component.value()).toBe('2024-02-29T23:45');
        expect(changed).not.toHaveBeenCalled();

        dateInput(fixture)['onDateSelect'](new Date('2024-03-01T00:00:00'));
        fixture.detectChanges();
        expect(component.value()).toBe('2024-03-01T23:45');
        expect(component['timeValue']()).toBe('23:45');
    });

    it('keeps a time entered without a date until a date is selected', async () => {
        const fixture = await setupAsync(null);
        const component = fixture.componentInstance;

        component['onTimeInput']('09:05');
        fixture.detectChanges();
        expect(component.value()).toBeNull();
        expect(timeInput(fixture).value).toBe('09:05');

        dateInput(fixture)['onDateSelect'](new Date('2025-01-01T00:00:00'));
        fixture.detectChanges();
        expect(component.value()).toBe('2025-01-01T09:05');
    });

    it('clears the date and time together and defaults an empty time on blur', async () => {
        const fixture = await setupAsync();
        const component = fixture.componentInstance;

        component['onTimeInput']('');
        fixture.detectChanges();
        expect(timeInput(fixture).value).toBe('');
        expect(component.value()).toBe('2024-02-29T23:45');
        component['onTimeBlur']();
        fixture.detectChanges();
        expect(component.value()).toBe('2024-02-29T00:00');
        expect(component.touched()).toBe(true);

        dateInput(fixture)['clearValue']();
        fixture.detectChanges();
        expect(component.value()).toBeNull();
        expect(timeInput(fixture).value).toBe('');
    });
});

describe('FdUiDatetimeInputComponent signal forms integration', () => {
    it('discards an incomplete draft when the parent resets to the same value', async () => {
        await TestBed.configureTestingModule({ imports: [DatetimeFormHost] }).compileComponents();
        const fixture = TestBed.createComponent(DatetimeFormHost);
        fixture.detectChanges();
        const child = fixture.debugElement.query(By.directive(FdUiDatetimeInputComponent)).componentInstance as FdUiDatetimeInputComponent;
        child['onTimeInput']('');
        fixture.detectChanges();
        expect(child['timeValue']()).toBe('');

        fixture.componentInstance.dateForm().reset();
        fixture.detectChanges();

        expect(child['timeValue']()).toBe('23:45');
        expect(child.value()).toBe('2024-02-29T23:45');
    });

    it('preserves parent edits, disabled state and form resets', async () => {
        await TestBed.configureTestingModule({ imports: [DatetimeFormHost] }).compileComponents();
        const fixture = TestBed.createComponent(DatetimeFormHost);
        fixture.detectChanges();
        const host = fixture.componentInstance;
        const child = fixture.debugElement.query(By.directive(FdUiDatetimeInputComponent)).componentInstance as FdUiDatetimeInputComponent;
        const date = fixture.debugElement.query(By.directive(FdUiDateInputComponent)).componentInstance as FdUiDateInputComponent;

        date['onDateSelect'](new Date('2024-03-01T00:00:00'));
        fixture.detectChanges();
        expect(host.model().date).toBe('2024-03-01T23:45');

        host.model.update(value => ({ ...value, disabled: true }));
        fixture.detectChanges();
        expect(date.disabled()).toBe(true);
        child['onTimeInput']('10:00');
        expect(host.model().date).toBe('2024-03-01T23:45');

        host.dateForm().reset({ date: '2025-12-31T00:15', disabled: false });
        fixture.detectChanges();
        expect(date.value()).toBe('2025-12-31');
        expect(child['timeValue']()).toBe('00:15');
        expect(child.touched()).toBe(false);
    });
});
