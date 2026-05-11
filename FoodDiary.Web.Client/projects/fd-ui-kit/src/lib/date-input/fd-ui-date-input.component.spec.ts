import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { FdUiDateInputComponent } from './fd-ui-date-input.component';

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
const requireElement = <T extends Element>(selector: string): T => {
    const element = host().querySelector<T>(selector);
    if (element === null) {
        throw new Error(`Expected element ${selector} to exist.`);
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
});

function registerLabelTests(): void {
    describe('label', () => {
        it('should render label', () => {
            fixture.componentRef.setInput('label', 'Date of Birth');
            fixture.detectChanges();

            const label = requireElement<HTMLElement>('.fd-ui-date-input__label-text');
            expect(label.textContent).toContain('Date of Birth');
        });

        it('should not render label when not provided', () => {
            const label = host().querySelector('.fd-ui-date-input__label');
            expect(label).toBeNull();
        });

        it('should show required asterisk', () => {
            fixture.componentRef.setInput('label', 'Date');
            fixture.componentRef.setInput('required', true);
            fixture.detectChanges();

            const asterisk = requireElement<HTMLElement>('.fd-ui-date-input__required');
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
    describe('value accessor', () => {
        it('should write value via CVA with string', () => {
            component.writeValue(MARCH_DATE_STRING);

            const dateValue = component['value']();
            expect(dateValue).toBeTruthy();
            expect(dateValue?.getFullYear()).toBe(TEST_YEAR);
            expect(dateValue?.getMonth()).toBe(MARCH_INDEX);
            expect(dateValue?.getDate()).toBe(TEST_DAY);
        });

        it('should write null value via CVA', () => {
            component.writeValue(JANUARY_DATE_STRING);
            expect(component['value']()).toBeTruthy();

            component.writeValue(null);
            expect(component['value']()).toBeNull();
        });

        it('should write Date object via CVA', () => {
            const date = new Date(TEST_YEAR, JUNE_INDEX, JUNE_DAY);
            component.writeValue(date);

            const dateValue = component['value']();
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

            const errorEl = requireElement<HTMLElement>('.fd-ui-date-input__error');
            expect(errorEl.textContent).toContain('Date is required');
        });

        it('should not display error when null', () => {
            fixture.componentRef.setInput('error', null);
            fixture.detectChanges();

            const errorEl = host().querySelector('.fd-ui-date-input__error');
            expect(errorEl).toBeNull();
        });

        it('should apply size class', () => {
            fixture.componentRef.setInput('size', 'lg');
            fixture.detectChanges();

            const container = requireElement<HTMLElement>('.fd-ui-date-input');
            expect(container.classList).toContain('fd-ui-date-input--size-lg');
        });

        it('should default to md size class', () => {
            const container = requireElement<HTMLElement>('.fd-ui-date-input');
            expect(container.classList).toContain('fd-ui-date-input--size-md');
        });

        it('should set disabled state', () => {
            component.setDisabledState(true);
            fixture.detectChanges();

            expect(component['disabled']()).toBe(true);

            const suffixButton = requireElement<HTMLButtonElement>('.fd-ui-date-input__suffix');
            expect(suffixButton.disabled).toBe(true);
        });

        it('should re-enable after being disabled', () => {
            component.setDisabledState(true);
            component.setDisabledState(false);
            fixture.detectChanges();

            expect(component['disabled']()).toBe(false);
        });

        it('should call onChange with formatted date string when date is selected', () => {
            const onChangeSpy = vi.fn();
            component.registerOnChange(onChangeSpy);

            component['onDateSelect'](new Date(TEST_YEAR, MARCH_INDEX, TEST_DAY));

            expect(onChangeSpy).toHaveBeenCalledWith(MARCH_DATE_STRING);
        });

        it('should display selected date value in the control', () => {
            component.writeValue(MARCH_DATE_STRING);
            fixture.detectChanges();

            const inputEl = requireElement<HTMLInputElement>('.fd-ui-date-input__control');
            expect(inputEl.value).toBeTruthy();
        });
    });
}
