import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it, vi } from 'vitest';

import { FdUiTimeInputComponent } from './fd-ui-time-input.component';

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

describe('FdUiTimeInputComponent CVA', () => {
    it('should write value via CVA', async () => {
        const { component } = await setupTimeInputAsync();
        component.writeValue('14:30');
        expect(component['internalValue']()).toBe('14:30');
    });

    it('should write null value via CVA as empty string', async () => {
        const { component } = await setupTimeInputAsync();
        component.writeValue('10:00');
        expect(component['internalValue']()).toBe('10:00');

        component.writeValue(null);
        expect(component['internalValue']()).toBe('');
    });

    it('should set disabled state', async () => {
        const { component } = await setupTimeInputAsync();
        component.setDisabledState(true);

        expect(component['disabled']()).toBe(true);
    });

    it('should re-enable after being disabled', async () => {
        const { component, fixture } = await setupTimeInputAsync();
        component.setDisabledState(true);
        component.setDisabledState(false);
        fixture.detectChanges();

        expect(component['disabled']()).toBe(false);
    });

    it('should call onTouched on blur', async () => {
        const { component } = await setupTimeInputAsync();
        const onTouchedSpy = vi.fn();
        component.registerOnTouched(onTouchedSpy);

        component['onBlur']();

        expect(onTouchedSpy).toHaveBeenCalled();
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

describe('FdUiTimeInputComponent input handling', () => {
    it('should call onChange with formatted time on valid input', async () => {
        const { component } = await setupTimeInputAsync();
        const onChangeSpy = vi.fn();
        component.registerOnChange(onChangeSpy);

        component['onInput']('14:30');

        expect(onChangeSpy).toHaveBeenCalledWith('14:30');
        expect(component['internalValue']()).toBe('14:30');
    });

    it('should call onChange with null on empty input', async () => {
        const { component } = await setupTimeInputAsync();
        const onChangeSpy = vi.fn();
        component.registerOnChange(onChangeSpy);

        component['onInput']('');

        expect(onChangeSpy).toHaveBeenCalledWith(null);
        expect(component['internalValue']()).toBe('');
    });

    it('should not call onChange on invalid time input', async () => {
        const { component } = await setupTimeInputAsync();
        const onChangeSpy = vi.fn();
        component.registerOnChange(onChangeSpy);

        component['onInput']('abc');

        expect(onChangeSpy).not.toHaveBeenCalled();
        expect(component['internalValue']()).toBe('abc');
    });

    it('should not process input when disabled', async () => {
        const { component } = await setupTimeInputAsync();
        const onChangeSpy = vi.fn();
        component.registerOnChange(onChangeSpy);
        component.setDisabledState(true);

        component['onInput']('14:30');

        expect(onChangeSpy).not.toHaveBeenCalled();
    });

    it('should pad single-digit hours and minutes', async () => {
        const { component } = await setupTimeInputAsync();
        const onChangeSpy = vi.fn();
        component.registerOnChange(onChangeSpy);

        component['onInput']('9:05');

        expect(onChangeSpy).toHaveBeenCalledWith('09:05');
        expect(component['internalValue']()).toBe('09:05');
    });

    it('should reject hours above 23', async () => {
        const { component } = await setupTimeInputAsync();
        const onChangeSpy = vi.fn();
        component.registerOnChange(onChangeSpy);

        component['onInput']('25:00');

        expect(onChangeSpy).not.toHaveBeenCalled();
    });

    it('should reject minutes above 59', async () => {
        const { component } = await setupTimeInputAsync();
        const onChangeSpy = vi.fn();
        component.registerOnChange(onChangeSpy);

        component['onInput']('12:60');

        expect(onChangeSpy).not.toHaveBeenCalled();
    });
});
