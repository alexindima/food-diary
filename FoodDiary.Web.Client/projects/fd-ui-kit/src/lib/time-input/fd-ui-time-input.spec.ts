import { OverlayContainer } from '@angular/cdk/overlay';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it, onTestFinished, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../src/testing/translate-testing.module';
import { FD_VALIDATION_ERRORS } from '../form-error/fd-ui-form-error';
import { FdUiTimeInputComponent } from './fd-ui-time-input';

const HOURS_PER_DAY = 24;
const LAID_OUT_OPTION_WIDTH = 100;

type TimeInputTestContext = {
    component: FdUiTimeInputComponent;
    fixture: ComponentFixture<FdUiTimeInputComponent>;
    host: () => HTMLElement;
    requireElement: (selector: string) => HTMLElement;
};

async function setupTimeInputAsync(): Promise<TimeInputTestContext> {
    await TestBed.configureTestingModule({
        imports: [FdUiTimeInputComponent],
        providers: [],
    }).compileComponents();

    const fixture = TestBed.createComponent(FdUiTimeInputComponent);
    const component = fixture.componentInstance;
    const host = (): HTMLElement => fixture.nativeElement as HTMLElement;
    const requireElement = (selector: string): HTMLElement => {
        const element = host().querySelector<HTMLElement>(selector);
        if (element === null) {
            throw new Error(`Expected element ${selector} to exist.`);
        }

        return element;
    };

    fixture.detectChanges();

    return { component, fixture, host, requireElement };
}

describe('FdUiTimeInputComponent', () => {
    it.each(['Enter', ' ', 'ArrowDown'])('opens with %j, focuses the selected hour, and closes on Escape', async key => {
        // CDK checks layout before focusing an option. jsdom supplies no layout metrics.
        const geometry = vi.spyOn(HTMLElement.prototype, 'offsetWidth', 'get').mockReturnValue(LAID_OUT_OPTION_WIDTH);
        onTestFinished(() => {
            geometry.mockRestore();
        });
        const { fixture, requireElement, component } = await setupTimeInputAsync();
        fixture.componentRef.setInput('value', '21:35');
        fixture.detectChanges();
        const input = requireElement('[role="combobox"]');
        const overlay = TestBed.inject(OverlayContainer).getContainerElement();
        input.focus();
        const open = new KeyboardEvent('keydown', { key, bubbles: true, cancelable: true });
        input.dispatchEvent(open);
        fixture.detectChanges();
        await fixture.whenStable();

        const active = overlay.querySelector('[role="group"] button[aria-pressed="true"]');
        expect(open.defaultPrevented).toBe(true);
        expect(input.getAttribute('aria-expanded')).toBe('true');
        expect(active?.textContent.trim()).toBe('21');
        expect(document.activeElement).toBe(active);

        const escape = new KeyboardEvent('keydown', { key: 'Escape', bubbles: true, cancelable: true });
        active?.dispatchEvent(escape);
        fixture.detectChanges();
        await fixture.whenStable();

        expect(escape.defaultPrevented).toBe(true);
        expect(overlay.querySelector('[role="dialog"]')).toBeNull();
        expect(input.getAttribute('aria-expanded')).toBe('false');
        expect(document.activeElement).toBe(input);
        expect(component.value()).toBe('21:35');
    });

    it('closes an open picker when Escape is sent directly to the input', async () => {
        const { fixture, component, requireElement } = await setupTimeInputAsync();
        const input = requireElement('[role="combobox"]');
        input.click();
        fixture.detectChanges();
        expect(input.getAttribute('aria-expanded')).toBe('true');

        input.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
        fixture.detectChanges();
        expect(input.getAttribute('aria-expanded')).toBe('false');
        expect(component.touched()).toBe(true);
    });

    it('should create', async () => {
        const { component } = await setupTimeInputAsync();

        expect(component).toBeTruthy();
    });
});

describe('FdUiTimeInputComponent rendering', () => {
    it('should render label', async () => {
        const { fixture, requireElement } = await setupTimeInputAsync();
        fixture.componentRef.setInput('label', 'Meal Time');
        fixture.detectChanges();

        const label = requireElement('.fd-ui-time-input__label-text');
        expect(label.textContent).toContain('Meal Time');
    });

    it('should not render label when not provided', async () => {
        const { host } = await setupTimeInputAsync();
        const label = host().querySelector('.fd-ui-time-input__label');
        expect(label).toBeNull();
    });

    it('should use the field label as the picker button accessible name', async () => {
        const { fixture, requireElement } = await setupTimeInputAsync();
        fixture.componentRef.setInput('label', 'Meal time');
        fixture.detectChanges();

        expect(requireElement('.fd-ui-time-input__suffix').getAttribute('aria-label')).toBe('Meal time');
    });

    it('should support a dedicated picker button accessible name', async () => {
        const { fixture, requireElement } = await setupTimeInputAsync();
        fixture.componentRef.setInput('label', 'Time');
        fixture.componentRef.setInput('pickerAriaLabel', 'Open time picker');
        fixture.detectChanges();

        expect(requireElement('.fd-ui-time-input__suffix').getAttribute('aria-label')).toBe('Open time picker');
    });

    it('should show required asterisk', async () => {
        const { fixture, requireElement } = await setupTimeInputAsync();
        fixture.componentRef.setInput('label', 'Time');
        fixture.componentRef.setInput('required', true);
        fixture.detectChanges();

        const asterisk = requireElement('.fd-ui-time-input__required');
        expect(asterisk.textContent).toContain('*');
    });

    it('should display error', async () => {
        const { fixture, requireElement } = await setupTimeInputAsync();
        fixture.componentRef.setInput('error', 'Invalid time');
        fixture.detectChanges();

        const errorEl = requireElement('.fd-ui-time-input__error');
        expect(errorEl.textContent).toContain('Invalid time');
    });

    it('should not display error when null', async () => {
        const { fixture, host } = await setupTimeInputAsync();
        fixture.componentRef.setInput('error', null);
        fixture.detectChanges();

        const errorEl = host().querySelector('.fd-ui-time-input__error');
        expect(errorEl).toBeNull();
    });
});

describe('FdUiTimeInputComponent signal form control', () => {
    it('should write value from model', async () => {
        const { component, fixture } = await setupTimeInputAsync();
        component.value.set('14:30');
        fixture.detectChanges();
        expect(component['internalValue']()).toBe('14:30');
    });

    it('should write null value as empty string', async () => {
        const { component, fixture } = await setupTimeInputAsync();
        component.value.set('10:00');
        fixture.detectChanges();
        expect(component['internalValue']()).toBe('10:00');

        component.value.set(null);
        fixture.detectChanges();
        expect(component['internalValue']()).toBe('');
    });

    it('should set disabled state', async () => {
        const { component, fixture } = await setupTimeInputAsync();
        fixture.componentRef.setInput('disabled', true);
        fixture.detectChanges();

        expect(component['disabled']()).toBe(true);
    });

    it('should re-enable after being disabled', async () => {
        const { component, fixture } = await setupTimeInputAsync();
        fixture.componentRef.setInput('disabled', true);
        fixture.detectChanges();
        fixture.componentRef.setInput('disabled', false);
        fixture.detectChanges();

        expect(component['disabled']()).toBe(false);
    });
});

describe('FdUiTimeInputComponent picker accessibility', () => {
    it('opens a named dialog with labelled groups of selectable buttons', async () => {
        const { component, fixture, requireElement } = await setupTimeInputAsync();
        fixture.componentRef.setInput('label', 'Время');
        fixture.componentRef.setInput('hoursLabel', 'Часы');
        fixture.componentRef.setInput('minutesLabel', 'Минуты');
        component.value.set('21:35');
        fixture.detectChanges();
        requireElement('.fd-ui-time-input__suffix').click();
        fixture.detectChanges();
        await fixture.whenStable();

        const overlay = TestBed.inject(OverlayContainer).getContainerElement();
        const dialog = overlay.querySelector('[role="dialog"]');
        expect(dialog?.getAttribute('aria-label')).toBe('Время');
        expect(dialog?.getAttribute('aria-modal')).toBe('true');
        const groups = overlay.querySelectorAll('[role="group"]');
        expect(Array.from(groups, group => group.getAttribute('aria-label'))).toEqual(['Часы', 'Минуты']);
        expect(overlay.querySelector('[role="listbox"]')).toBeNull();
        expect(Array.from(overlay.querySelectorAll('button[aria-pressed="true"]'), button => button.textContent.trim())).toEqual([
            '21',
            '35',
        ]);
    });

    it('uses the dedicated picker name for the dialog', async () => {
        const { fixture, requireElement } = await setupTimeInputAsync();
        fixture.componentRef.setInput('label', 'Time');
        fixture.componentRef.setInput('pickerAriaLabel', 'Choose meal time');
        fixture.detectChanges();
        requireElement('.fd-ui-time-input__suffix').click();
        fixture.detectChanges();
        await fixture.whenStable();

        const overlay = TestBed.inject(OverlayContainer).getContainerElement();
        expect(overlay.querySelector('[role="dialog"]')?.getAttribute('aria-label')).toBe('Choose meal time');
    });

    it('keeps a meaningful name when only column labels are supplied', async () => {
        const { fixture, requireElement } = await setupTimeInputAsync();
        fixture.componentRef.setInput('hoursLabel', 'Часы');
        fixture.componentRef.setInput('minutesLabel', 'Минуты');
        fixture.detectChanges();

        expect(requireElement('.fd-ui-time-input__control').getAttribute('aria-label')).toBe('Часы, Минуты');
        expect(requireElement('.fd-ui-time-input__suffix').getAttribute('aria-label')).toBe('Часы, Минуты');
    });
});

describe('FdUiTimeInputComponent classes', () => {
    it('should apply size class', async () => {
        const { fixture, requireElement } = await setupTimeInputAsync();
        fixture.componentRef.setInput('size', 'lg');
        fixture.detectChanges();

        const container = requireElement('.fd-ui-time-input');
        expect(container.classList).toContain('fd-ui-time-input--size-lg');
    });

    it('should default to md size class', async () => {
        const { requireElement } = await setupTimeInputAsync();
        const container = requireElement('.fd-ui-time-input');
        expect(container.classList).toContain('fd-ui-time-input--size-md');
    });
});

describe('FdUiTimeInputComponent clock values', () => {
    it('shows all 24 hours and commits a padded time through the readonly field picker', async () => {
        const { fixture, component, requireElement } = await setupTimeInputAsync();
        fixture.componentRef.setInput('value', '9:05');
        fixture.detectChanges();
        const input = requireElement('[role="combobox"]');
        expect(input.matches('[readonly]')).toBe(true);
        input.click();
        fixture.detectChanges();
        const overlay = TestBed.inject(OverlayContainer).getContainerElement();
        const hours = [...overlay.querySelectorAll('[role="group"][aria-label="Hours"] button')];
        expect(hours).toHaveLength(HOURS_PER_DAY);
        expect(hours[0]?.textContent.trim()).toBe('00');
        expect(hours.at(-1)?.textContent.trim()).toBe('23');
        expect(pickerOption(overlay, 'Hours', '09').getAttribute('aria-pressed')).toBe('true');
        expect(pickerOption(overlay, 'Minutes', '05').getAttribute('aria-pressed')).toBe('true');

        pickerOption(overlay, 'Minutes', '05').click();
        fixture.detectChanges();
        expect(component.value()).toBe('09:05');
        expect(input.getAttribute('aria-expanded')).toBe('false');
    });

    it.each(['abc', '25:00', '12:60'])('does not select impossible external time %s and accepts a valid replacement', async invalid => {
        const { fixture, component, requireElement } = await setupTimeInputAsync();
        fixture.componentRef.setInput('value', invalid);
        fixture.detectChanges();
        requireElement('[role="combobox"]').click();
        fixture.detectChanges();
        const overlay = TestBed.inject(OverlayContainer).getContainerElement();

        expect(pickerOption(overlay, 'Hours', '00').getAttribute('aria-pressed')).toBe('true');
        expect(pickerOption(overlay, 'Minutes', '00').getAttribute('aria-pressed')).toBe('true');
        expect(component.value()).toBe(invalid);
        pickerOption(overlay, 'Minutes', '15').click();
        fixture.detectChanges();
        expect(component.value()).toBe('00:15');
    });
});

describe('FdUiTimeInputComponent rendered interactions', () => {
    it('preserves a precise minute from the model while selecting an hour and then commits a picked minute', async () => {
        const { component, fixture, requireElement } = await setupTimeInputAsync();
        fixture.componentRef.setInput('value', '21:59');
        fixture.detectChanges();
        const changed = vi.fn();
        component.value.subscribe(changed);
        const input = requireElement('[role="combobox"]');
        input.click();
        fixture.detectChanges();
        const overlay = TestBed.inject(OverlayContainer).getContainerElement();
        expect(overlay.querySelector('[aria-label="Minutes"] [aria-pressed="true"]')).toBeNull();

        const hour = pickerOption(overlay, 'Hours', '23');
        hour.click();
        fixture.detectChanges();
        expect(hour.getAttribute('aria-pressed')).toBe('true');
        expect(component.value()).toBe('23:59');
        expect(input.getAttribute('aria-expanded')).toBe('true');

        const arrow = new KeyboardEvent('keydown', { key: 'ArrowRight', bubbles: true, cancelable: true });
        hour.dispatchEvent(arrow);
        expect(arrow.defaultPrevented).toBe(false);
        pickerOption(overlay, 'Minutes', '35').click();
        fixture.detectChanges();
        expect(input.getAttribute('aria-expanded')).toBe('false');
        expect(changed.mock.calls).toEqual([['23:59'], ['23:35']]);
        expect(component.touched()).toBe(true);
    });

    it('closes the picker when the consumer disables it and rejects subsequent keyboard activation', async () => {
        const { component, fixture, requireElement } = await setupTimeInputAsync();
        const input = requireElement('[role="combobox"]');
        input.click();
        fixture.detectChanges();
        expect(input.getAttribute('aria-expanded')).toBe('true');

        fixture.componentRef.setInput('disabled', true);
        fixture.detectChanges();
        input.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true, cancelable: true }));
        fixture.detectChanges();

        expect(input.matches(':disabled')).toBe(true);
        expect(input.getAttribute('aria-expanded')).toBe('false');
        expect(component.value()).toBeNull();
    });

    it('focuses the actual control for Signal Forms and marks the field touched when focus leaves', async () => {
        const { component, fixture, requireElement } = await setupTimeInputAsync();
        const input = requireElement('[role="combobox"]');
        const touch = vi.fn();
        component.touch.subscribe(touch);
        const outside = document.createElement('button');
        document.body.append(outside);
        onTestFinished(() => {
            outside.remove();
        });

        component.focus();
        fixture.detectChanges();
        expect(document.activeElement).toBe(input);
        expect(input.getAttribute('placeholder')).toBe('HH:mm');
        expect(component.touched()).toBe(false);

        const tab = new KeyboardEvent('keydown', { key: 'Tab', bubbles: true, cancelable: true });
        input.dispatchEvent(tab);
        expect(tab.defaultPrevented).toBe(false);

        requireElement('.fd-ui-time-input__suffix').focus();
        fixture.detectChanges();
        expect(component.touched()).toBe(false);

        outside.focus();
        fixture.detectChanges();
        expect(component.touched()).toBe(true);
        expect(touch).toHaveBeenCalledOnce();
        expect(input.getAttribute('placeholder')).toBeNull();
    });
});

function pickerOption(overlay: HTMLElement, groupLabel: string, value: string): HTMLButtonElement {
    const option = [...overlay.querySelectorAll<HTMLButtonElement>(`[role="group"][aria-label="${groupLabel}"] button`)].find(
        element => element.textContent.trim() === value,
    );
    if (option === undefined) {
        throw new Error(`Expected option ${value} in the ${groupLabel} group.`);
    }
    return option;
}

describe('FdUiTimeInputComponent scoped validation', () => {
    it('announces the local required-time message after leaving the control', async () => {
        await TestBed.configureTestingModule({
            imports: [FdUiTimeInputComponent],
            providers: [provideTranslateTesting()],
        })
            .overrideComponent(FdUiTimeInputComponent, {
                add: { providers: [{ provide: FD_VALIDATION_ERRORS, useValue: { required: (): string => 'Select a meal time' } }] },
            })
            .compileComponents();

        const fixture = TestBed.createComponent(FdUiTimeInputComponent);
        fixture.componentRef.setInput('errors', [{ kind: 'required' }]);
        fixture.detectChanges();
        const element = fixture.nativeElement as HTMLElement;
        const control = element.querySelector('input');
        expect(element.querySelector('[role="alert"]')).toBeNull();

        control?.focus();
        control?.blur();
        fixture.detectChanges();
        const error = element.querySelector('[role="alert"]');
        expect(error?.textContent.trim()).toBe('Select a meal time');
        expect(control?.getAttribute('aria-describedby')).toBe(error?.id);
    });
});
