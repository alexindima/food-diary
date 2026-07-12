import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { FdUiDateInputComponent } from './fd-ui-date-input';

const TEST_YEAR = 2025;
const MARCH_INDEX = 2;
const JUNE_INDEX = 5;
const TEST_DAY = 15;
const JUNE_DAY = 20;
const MARCH_DATE_STRING = '2025-03-15';
const JANUARY_DATE_STRING = '2025-01-01';

let component: FdUiDateInputComponent;
let fixture: ComponentFixture<FdUiDateInputComponent>;

const host = (): HTMLElement => fixture.nativeElement as HTMLElement;
const requireElement = (selector: string): HTMLElement => {
    const element = host().querySelector<HTMLElement>(selector);
    if (element === null) {
        throw new Error(`Expected element ${selector} to exist.`);
    }

    return element;
};

const requireButtonElement = (selector: string): HTMLButtonElement => {
    const element = host().querySelector<HTMLButtonElement>(selector);
    if (element === null) {
        throw new Error(`Expected button ${selector} to exist.`);
    }

    return element;
};

const requireInputElement = (selector: string): HTMLInputElement => {
    const element = host().querySelector<HTMLInputElement>(selector);
    if (element === null) {
        throw new Error(`Expected input ${selector} to exist.`);
    }

    return element;
};

describe('FdUiDateInputComponent', () => {
    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [FdUiDateInputComponent],
        }).compileComponents();

        fixture = TestBed.createComponent(FdUiDateInputComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    registerLabelTests();
    registerValueAccessorTests();
    registerStateTests();
    registerInteractionTests();
});

function registerLabelTests(): void {
    describe('label', () => {
        it('should render label', () => {
            fixture.componentRef.setInput('label', 'Date of Birth');
            fixture.detectChanges();

            const label = requireElement('.fd-ui-date-input__label-text');
            expect(label.textContent).toContain('Date of Birth');
        });

        it('should not render label when not provided', () => {
            const label = host().querySelector('.fd-ui-date-input__label');
            expect(label).toBeNull();
        });

        it('should use the field label as the calendar button accessible name', () => {
            fixture.componentRef.setInput('label', 'Filter from');
            fixture.detectChanges();

            expect(requireButtonElement('.fd-ui-date-input__suffix').getAttribute('aria-label')).toBe('Filter from');
        });

        it('should support a dedicated calendar button accessible name', () => {
            fixture.componentRef.setInput('label', 'Date');
            fixture.componentRef.setInput('pickerAriaLabel', 'Open start date calendar');
            fixture.detectChanges();

            expect(requireButtonElement('.fd-ui-date-input__suffix').getAttribute('aria-label')).toBe('Open start date calendar');
        });

        it('should show required asterisk', () => {
            fixture.componentRef.setInput('label', 'Date');
            fixture.componentRef.setInput('required', true);
            fixture.detectChanges();

            const asterisk = requireElement('.fd-ui-date-input__required');
            expect(asterisk.textContent).toContain('*');
        });

        it('should not show required asterisk when not required', () => {
            fixture.componentRef.setInput('label', 'Date');
            fixture.componentRef.setInput('required', false);
            fixture.detectChanges();

            const asterisk = host().querySelector('.fd-ui-date-input__required');
            expect(asterisk).toBeNull();
        });
    });
}

function registerValueAccessorTests(): void {
    describe('signal form control', () => {
        it('should write value from model with string', () => {
            component.value.set(MARCH_DATE_STRING);
            fixture.detectChanges();

            const dateValue = component['internalValue']();
            expect(dateValue).toBeTruthy();
            expect(dateValue?.getFullYear()).toBe(TEST_YEAR);
            expect(dateValue?.getMonth()).toBe(MARCH_INDEX);
            expect(dateValue?.getDate()).toBe(TEST_DAY);
        });

        it('should write null value', () => {
            component.value.set(JANUARY_DATE_STRING);
            fixture.detectChanges();
            expect(component['internalValue']()).toBeTruthy();

            component.value.set(null);
            fixture.detectChanges();
            expect(component['internalValue']()).toBeNull();
        });

        it('should write Date object from model', () => {
            const date = new Date(TEST_YEAR, JUNE_INDEX, JUNE_DAY);
            component.value.set(date);
            fixture.detectChanges();

            const dateValue = component['internalValue']();
            expect(dateValue).toBeTruthy();
            expect(dateValue?.getFullYear()).toBe(TEST_YEAR);
            expect(dateValue?.getMonth()).toBe(JUNE_INDEX);
            expect(dateValue?.getDate()).toBe(JUNE_DAY);
        });
    });
}

function registerStateTests(): void {
    describe('state', () => {
        it('should display error', () => {
            fixture.componentRef.setInput('error', 'Date is required');
            fixture.detectChanges();

            const errorEl = requireElement('.fd-ui-date-input__error');
            expect(errorEl.textContent).toContain('Date is required');
        });

        it('should not display error when null', () => {
            fixture.componentRef.setInput('error', null);
            fixture.detectChanges();

            const errorEl = host().querySelector('.fd-ui-date-input__error');
            expect(errorEl).toBeNull();
        });

        it('should not apply error class when error is omitted', () => {
            const container = requireElement('.fd-ui-date-input');
            expect(container.classList).not.toContain('fd-ui-date-input--has-error');
        });

        it('should not apply error class when error is empty', () => {
            fixture.componentRef.setInput('error', '');
            fixture.detectChanges();

            const container = requireElement('.fd-ui-date-input');
            expect(container.classList).not.toContain('fd-ui-date-input--has-error');
        });

        it('should apply error class when error is provided', () => {
            fixture.componentRef.setInput('error', 'Date is required');
            fixture.detectChanges();

            const container = requireElement('.fd-ui-date-input');
            expect(container.classList).toContain('fd-ui-date-input--has-error');
        });

        it('should apply size class', () => {
            fixture.componentRef.setInput('size', 'lg');
            fixture.detectChanges();

            const container = requireElement('.fd-ui-date-input');
            expect(container.classList).toContain('fd-ui-date-input--size-lg');
        });

        it('should default to md size class', () => {
            const container = requireElement('.fd-ui-date-input');
            expect(container.classList).toContain('fd-ui-date-input--size-md');
        });

        it('should set disabled state', () => {
            fixture.componentRef.setInput('disabled', true);
            fixture.detectChanges();

            expect(component['disabled']()).toBe(true);

            const suffixButton = requireButtonElement('.fd-ui-date-input__suffix');
            expect(suffixButton.disabled).toBe(true);
        });

        it('should re-enable after being disabled', () => {
            fixture.componentRef.setInput('disabled', true);
            fixture.detectChanges();
            fixture.componentRef.setInput('disabled', false);
            fixture.detectChanges();

            expect(component['disabled']()).toBe(false);
        });

        it('should update value with formatted date string when date is selected', () => {
            component['onDateSelect'](new Date(TEST_YEAR, MARCH_INDEX, TEST_DAY));

            expect(component.value()).toBe(MARCH_DATE_STRING);
        });

        it('should display selected date value in the control', () => {
            component.value.set(MARCH_DATE_STRING);
            fixture.detectChanges();

            const inputEl = requireInputElement('.fd-ui-date-input__control');
            expect(inputEl.value).toBeTruthy();
        });
    });
}

function registerInteractionTests(): void {
    describe('interaction', () => {
        it('should not change value when selected date is null', () => {
            component.value.set(MARCH_DATE_STRING);
            fixture.detectChanges();

            component['onDateSelect'](null);

            expect(component['internalValue']()?.getDate()).toBe(TEST_DAY);
            expect(component.value()).toBe(MARCH_DATE_STRING);
        });

        it('should open date picker from keyboard and close with escape', () => {
            const openEvent = new KeyboardEvent('keydown', { key: 'Enter' });
            const openPreventDefaultSpy = vi.spyOn(openEvent, 'preventDefault');

            component['onInputKeydown'](openEvent);

            expect(openPreventDefaultSpy).toHaveBeenCalled();
            expect(component['isOpen']()).toBe(true);
            expect(component['isFocused']()).toBe(true);

            const closeEvent = new KeyboardEvent('keydown', { key: 'Escape' });
            const closePreventDefaultSpy = vi.spyOn(closeEvent, 'preventDefault');
            component['onInputKeydown'](closeEvent);

            expect(closePreventDefaultSpy).toHaveBeenCalled();
            expect(component['isOpen']()).toBe(false);
        });

        it('should close date picker from overlay escape', () => {
            component['openDatePicker']();
            const event = new KeyboardEvent('keydown', { key: 'Escape' });
            const preventDefaultSpy = vi.spyOn(event, 'preventDefault');

            component['onOverlayKeydown'](event);

            expect(preventDefaultSpy).toHaveBeenCalled();
            expect(component['isOpen']()).toBe(false);
        });

        it('should ignore unsupported overlay key', () => {
            component['openDatePicker']();

            component['onOverlayKeydown'](new KeyboardEvent('keydown', { key: 'ArrowDown' }));

            expect(component['isOpen']()).toBe(true);
        });

        it('should update display month when calendar emits a value', () => {
            const nextMonth = new Date(TEST_YEAR, JUNE_INDEX, JUNE_DAY);

            component['onDisplayMonthChange'](nextMonth);

            expect(component['displayMonth']()).toBe(nextMonth);
        });

        it('should ignore null display month changes', () => {
            const initialMonth = component['displayMonth']();

            component['onDisplayMonthChange'](null);

            expect(component['displayMonth']()).toBe(initialMonth);
        });

        it('should close date picker when disabled while open', () => {
            component['openDatePicker']();

            fixture.componentRef.setInput('disabled', true);
            fixture.detectChanges();

            expect(component['disabled']()).toBe(true);
            expect(component['isOpen']()).toBe(false);
        });
    });
}
