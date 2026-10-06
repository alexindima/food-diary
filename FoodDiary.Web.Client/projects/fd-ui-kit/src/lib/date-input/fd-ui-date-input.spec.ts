import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../src/testing/translate-testing.module';
import { fdUiFormatDateInputValue } from '../date/fd-ui-date.utils';
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
    it('keeps accessible required semantics when the asterisk is hidden', () => {
        fixture.componentRef.setInput('label', 'Date');
        fixture.componentRef.setInput('required', true);
        fixture.componentRef.setInput('showRequiredIndicator', false);
        fixture.detectChanges();
        expect(requireInputElement('input').getAttribute('aria-required')).toBe('true');
        expect(host().querySelector('.fd-ui-date-input__required')).toBeNull();
    });
    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [FdUiDateInputComponent],
            providers: [provideTranslateTesting()],
        }).compileComponents();

        fixture = TestBed.createComponent(FdUiDateInputComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('accepts a distant birthday through native keyboard input and rejects dates outside bounds', () => {
        fixture.componentRef.setInput('allowManualInput', true);
        fixture.componentRef.setInput('max', '2026-09-30');
        fixture.detectChanges();
        const input = requireInputElement('input');
        expect(input.type).toBe('date');
        expect(input.readOnly).toBe(false);
        expect(input.getAttribute('role')).toBeNull();
        expect(input.getAttribute('aria-expanded')).toBeNull();
        expect(input.getAttribute('aria-haspopup')).toBeNull();
        const picker = requireButtonElement('.fd-ui-date-input__suffix');
        expect(picker.getAttribute('aria-haspopup')).toBe('dialog');
        expect(picker.getAttribute('aria-expanded')).toBe('false');
        picker.click();
        fixture.detectChanges();
        expect(picker.getAttribute('aria-expanded')).toBe('true');
        input.value = '1995-06-15';
        input.dispatchEvent(new Event('input'));
        fixture.detectChanges();
        expect(component.value()).toBe('1995-06-15');
        input.value = '2027-01-01';
        input.dispatchEvent(new Event('input'));
        fixture.detectChanges();
        expect(component.value()).toBe('1995-06-15');
        expect(component.touched()).toBe(true);
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('clears an optional date and emits null', () => {
        fixture.componentRef.setInput('value', MARCH_DATE_STRING);
        fixture.componentRef.setInput('clearAriaLabel', 'Clear date');
        fixture.detectChanges();
        const changed = vi.fn();
        component.value.subscribe(changed);
        requireButtonElement('[aria-label="Clear date"]').click();
        fixture.detectChanges();
        expect(component.value()).toBeNull();
        expect(component.touched()).toBe(true);
        expect(changed).toHaveBeenCalledWith(null);
        expect(requireInputElement('input').value).toBe('');
        expect(host().querySelector('[aria-label="Clear date"]')).toBeNull();
    });

    registerLabelTests();
    registerManualInputValidationTests();
    registerValueAccessorTests();
    registerStateTests();
    registerCalendarFocusTests();
    registerInteractionTests();
});

function registerManualInputValidationTests(): void {
    describe('manual input validation state', () => {
        it('reports invalid input without emitting a replacement value, then recovers', () => {
            const input = prepareManualDateInput();
            const invalid = vi.fn();
            const changed = vi.fn();
            component.manualInputInvalidChange.subscribe(invalid);
            component.value.subscribe(changed);

            input.value = '2027-01-01';
            input.dispatchEvent(new Event('input'));
            fixture.detectChanges();

            expect(invalid).toHaveBeenLastCalledWith(true);
            expect(changed).not.toHaveBeenCalled();
            expect(component.value()).toBe(MARCH_DATE_STRING);
            expect(input.value).toBe('2027-01-01');

            input.value = MARCH_DATE_STRING;
            input.dispatchEvent(new Event('input'));
            fixture.detectChanges();
            expect(invalid).toHaveBeenLastCalledWith(false);
        });

        it('resets an invalid DOM draft when the model value has not changed', () => {
            const input = prepareManualDateInput();
            input.value = '2027-01-01';
            input.dispatchEvent(new Event('input'));
            fixture.detectChanges();
            const invalid = vi.fn();
            component.manualInputInvalidChange.subscribe(invalid);

            component.reset();
            fixture.detectChanges();

            expect(input.value).toBe(MARCH_DATE_STRING);
            expect(input.getAttribute('aria-invalid')).toBeNull();
            expect(invalid).toHaveBeenLastCalledWith(false);
            expect(component.touched()).toBe(false);
        });

        it('clears an invalid optional draft intentionally', () => {
            const input = prepareManualDateInput();
            input.value = '2027-01-01';
            input.dispatchEvent(new Event('input'));
            fixture.detectChanges();

            input.value = '';
            input.dispatchEvent(new Event('input'));
            fixture.detectChanges();

            expect(component.value()).toBeNull();
            expect(input.getAttribute('aria-invalid')).toBeNull();
        });
    });
}

function prepareManualDateInput(): HTMLInputElement {
    fixture.componentRef.setInput('allowManualInput', true);
    fixture.componentRef.setInput('max', '2026-09-30');
    fixture.componentRef.setInput('value', MARCH_DATE_STRING);
    fixture.componentRef.setInput('invalidDateLabel', 'Invalid date');
    fixture.detectChanges();
    return requireInputElement('input');
}

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

        it('should expose the popup input as a combobox', () => {
            expect(requireInputElement('.fd-ui-date-input__control').getAttribute('role')).toBe('combobox');
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

        it('should display the supplied today label for the local current date', () => {
            fixture.componentRef.setInput('todayLabel', 'Today');
            component.value.set(fdUiFormatDateInputValue(new Date()));
            fixture.detectChanges();

            expect(requireInputElement('.fd-ui-date-input__control').value).toBe('Today');
        });

        it('should preserve formatted dates when the selected date is not today', () => {
            fixture.componentRef.setInput('todayLabel', 'Today');
            component.value.set(MARCH_DATE_STRING);
            fixture.detectChanges();

            expect(requireInputElement('.fd-ui-date-input__control').value).not.toBe('Today');
        });
    });
}

function registerCalendarFocusTests(): void {
    describe('calendar focus', () => {
        it.each(['escape', 'selection'])('returns calendar focus to the date field after %s', action => {
            component.value.set(MARCH_DATE_STRING);
            fixture.detectChanges();
            component['openDatePicker']();
            fixture.detectChanges();
            const day = document
                .getElementById(`${component.id()}-dialog`)
                ?.querySelector<HTMLButtonElement>(`[data-date="${MARCH_DATE_STRING}"]`);
            if (day === undefined || day === null) {
                throw new Error('Expected selected calendar day to exist.');
            }
            day.focus();
            expect(document.activeElement).toBe(day);

            if (action === 'escape') {
                component['onOverlayKeydown'](new KeyboardEvent('keydown', { key: 'Escape' }));
            } else {
                day.click();
            }
            fixture.detectChanges();

            expect(component['isOpen']()).toBe(false);
            expect(document.activeElement).toBe(requireInputElement('input'));
            expect(component.value()).toBe(MARCH_DATE_STRING);
        });

        it('keeps focus on an outside control when closing the calendar', () => {
            const outside = document.createElement('button');
            document.body.append(outside);
            try {
                component['openDatePicker']();
                fixture.detectChanges();
                outside.focus();
                component['closeDatePicker']();
                fixture.detectChanges();

                expect(component['isOpen']()).toBe(false);
                expect(document.activeElement).toBe(outside);
            } finally {
                outside.remove();
            }
        });
    });
}

function registerInteractionTests(): void {
    describe('interaction', () => {
        it.each(['input', '.fd-ui-date-input__suffix'])('keeps the enclosing dialog open when escaping the calendar from %s', selector => {
            const dismissParent = vi.fn();
            const parent = document.createElement('div');
            parent.addEventListener('keydown', dismissParent);
            parent.append(host());
            component['openDatePicker']();
            fixture.detectChanges();

            const escape = new KeyboardEvent('keydown', { key: 'Escape', bubbles: true, cancelable: true });
            requireElement(selector).dispatchEvent(escape);

            expect(component['isOpen']()).toBe(false);
            expect(escape.defaultPrevented).toBe(true);
            expect(dismissParent).not.toHaveBeenCalled();

            requireElement(selector).dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true, cancelable: true }));
            expect(dismissParent).toHaveBeenCalledOnce();
        });

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
