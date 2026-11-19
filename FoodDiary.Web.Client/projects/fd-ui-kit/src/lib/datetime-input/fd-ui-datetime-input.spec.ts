import { OverlayContainer } from '@angular/cdk/overlay';
import { Component, signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { disabled, form, FormField } from '@angular/forms/signals';
import { By } from '@angular/platform-browser';
import { describe, expect, it, onTestFinished, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../src/testing/translate-testing.module';
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
    await TestBed.configureTestingModule({
        imports: [FdUiDatetimeInputComponent],
        providers: [provideTranslateTesting()],
    }).compileComponents();
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

describe('FdUiDatetimeInputComponent native time interactions', () => {
    it('keeps the committed value when blurring the time, then commits midnight for a cleared field', async () => {
        const fixture = await setupAsync();
        const component = fixture.componentInstance;
        const changed = vi.fn();
        component.value.subscribe(changed);
        const input = timeInput(fixture);

        input.dispatchEvent(new FocusEvent('blur'));
        fixture.detectChanges();
        expect(component.value()).toBe('2024-02-29T23:45');
        expect(changed).not.toHaveBeenCalled();

        input.value = '';
        input.dispatchEvent(new Event('input', { bubbles: true }));
        fixture.detectChanges();
        expect(component.value()).toBe('2024-02-29T23:45');
        input.dispatchEvent(new FocusEvent('blur'));
        fixture.detectChanges();

        expect(input.value).toBe('00:00');
        expect(changed).toHaveBeenCalledExactlyOnceWith('2024-02-29T00:00');
        expect(component.touched()).toBe(true);
    });

    it('opens the native time picker from its button and preserves the current value', async () => {
        const fixture = await setupAsync();
        const input = timeInput(fixture);
        const showPicker = vi.fn();
        Object.defineProperty(input, 'showPicker', { configurable: true, value: showPicker });
        const button = (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>('.fd-ui-datetime-input__suffix');

        button?.click();
        fixture.detectChanges();
        expect(document.activeElement).toBe(input);
        expect(showPicker).toHaveBeenCalledOnce();
        expect(fixture.componentInstance.value()).toBe('2024-02-29T23:45');

        fixture.componentRef.setInput('disabled', true);
        fixture.detectChanges();
        expect(button?.disabled).toBe(true);
        button?.click();
        expect(showPicker).toHaveBeenCalledOnce();
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

describe('FdUiDatetimeInputComponent calendar and native focus', () => {
    it('recovers an invalid external date via the calendar using midnight or the last confirmed time', async () => {
        vi.useFakeTimers({ toFake: ['Date'] });
        vi.setSystemTime(new Date('2024-02-20T12:00:00'));
        onTestFinished(() => {
            vi.useRealTimers();
        });
        const fixture = await setupAsync('invalid imported datetime');
        const changed = vi.fn();
        fixture.componentInstance.value.subscribe(changed);
        expect(timeInput(fixture).value).toBe('');

        clickCalendarDate(fixture, '2024-02-21');
        expect(changed).toHaveBeenCalledExactlyOnceWith('2024-02-21T00:00');

        const time = timeInput(fixture);
        time.value = '09:15';
        time.dispatchEvent(new Event('input', { bubbles: true }));
        fixture.detectChanges();
        fixture.componentRef.setInput('value', 'another invalid datetime');
        fixture.detectChanges();
        expect(time.value).toBe('');

        clickCalendarDate(fixture, '2024-02-22');
        expect(changed.mock.calls).toEqual([['2024-02-21T00:00'], ['2024-02-21T09:15'], ['2024-02-22T09:15']]);
        expect(time.value).toBe('09:15');
    });

    it('keeps a disabled field unchanged if a pending child update and time-picker click arrive', async () => {
        const fixture = await setupAsync();
        const changed = vi.fn();
        fixture.componentInstance.value.subscribe(changed);
        const trigger = timeTrigger(fixture);
        const showPicker = vi.fn();
        Object.defineProperty(timeInput(fixture), 'showPicker', { value: showPicker });

        fixture.componentRef.setInput('disabled', true);
        fixture.detectChanges();
        dateInput(fixture).value.set('2024-03-01');
        trigger.dispatchEvent(new MouseEvent('click', { bubbles: true }));
        fixture.detectChanges();

        expect(trigger.disabled).toBe(true);
        expect(showPicker).not.toHaveBeenCalled();
        expect(changed).not.toHaveBeenCalled();
        expect(fixture.componentInstance.value()).toBe('2024-02-29T23:45');
    });

    it('focuses the time input when native showPicker is missing and marks the composite field touched when leaving', async () => {
        const fixture = await setupAsync(null);
        const input = timeInput(fixture);
        Object.defineProperty(input, 'showPicker', { value: undefined });
        const region = fixture.nativeElement as HTMLElement;
        const touch = vi.fn();
        fixture.componentInstance.touch.subscribe(touch);
        const outside = document.createElement('button');
        document.body.append(outside);
        onTestFinished(() => {
            outside.remove();
        });

        expect(input.getAttribute('placeholder')).toBe('HH:mm');
        const trigger = timeTrigger(fixture);
        trigger.focus();
        trigger.click();
        fixture.detectChanges();
        expect(document.activeElement).toBe(input);
        expect(input.getAttribute('placeholder')).toBeNull();
        expect(fixture.componentInstance.touched()).toBe(false);

        outside.focus();
        fixture.detectChanges();
        expect(fixture.componentInstance.touched()).toBe(true);
        expect(touch).toHaveBeenCalled();
        expect(region.querySelector('.fd-ui-datetime-input--floating')).not.toBeNull();
    });
});

describe('FdUiDatetimeInputComponent validation and reset', () => {
    it('preserves the committed value if native time reports a draft with seconds', async () => {
        const fixture = await setupAsync();
        const input = timeInput(fixture);
        const changed = vi.fn();
        fixture.componentInstance.value.subscribe(changed);
        input.focus();
        input.value = '09:15:45';
        input.dispatchEvent(new Event('input', { bubbles: true }));
        input.blur();
        fixture.detectChanges();

        expect(input.value).toBe('09:15:45');
        expect(fixture.componentInstance.value()).toBe('2024-02-29T23:45');
        expect(fixture.componentInstance.touched()).toBe(true);
        expect(changed).not.toHaveBeenCalled();
    });

    it('shows a domain error and clears an uncommitted time draft when reset on an empty model', async () => {
        const fixture = await setupAsync(null);
        const host = fixture.nativeElement as HTMLElement;
        const input = timeInput(fixture);
        const changed = vi.fn();
        fixture.componentInstance.value.subscribe(changed);
        fixture.componentRef.setInput('error', 'Choose an appointment date');
        fixture.detectChanges();
        expect(host.querySelector('.fd-ui-datetime-input--has-error')?.textContent).toContain('Choose an appointment date');

        input.value = '09:30';
        input.dispatchEvent(new Event('input', { bubbles: true }));
        fixture.detectChanges();
        expect(input.value).toBe('09:30');
        expect(changed).not.toHaveBeenCalled();

        fixture.componentInstance.reset();
        fixture.componentRef.setInput('error', null);
        fixture.detectChanges();
        expect(input.value).toBe('');
        expect(host.querySelector('.fd-ui-datetime-input--has-error')).toBeNull();
        expect(fixture.componentInstance.value()).toBeNull();
        expect(fixture.componentInstance.touched()).toBe(false);
        expect(changed).not.toHaveBeenCalled();
    });
});

function timeTrigger(fixture: ComponentFixture<FdUiDatetimeInputComponent>): HTMLButtonElement {
    const trigger = (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>('.fd-ui-datetime-input__suffix');
    if (trigger === null) {
        throw new Error('Expected the time picker trigger.');
    }
    return trigger;
}

function clickCalendarDate(fixture: ComponentFixture<FdUiDatetimeInputComponent>, date: string): void {
    const trigger = (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>('fd-ui-date-input button');
    const overlay = TestBed.inject(OverlayContainer).getContainerElement();
    trigger?.click();
    fixture.detectChanges();
    const day = overlay.querySelector<HTMLButtonElement>(`[data-date="${date}"]`);
    if (day === null) {
        throw new Error(`Expected ${date} in the date picker.`);
    }
    day.click();
    fixture.detectChanges();
}
