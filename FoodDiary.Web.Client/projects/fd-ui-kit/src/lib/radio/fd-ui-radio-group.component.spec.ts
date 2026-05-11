import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { FdUiRadioGroupComponent, type FdUiRadioOption } from './fd-ui-radio-group.component';

const OPTION_COUNT = 3;

describe('FdUiRadioGroupComponent', () => {
    let component: FdUiRadioGroupComponent<string>;
    let fixture: ComponentFixture<FdUiRadioGroupComponent<string>>;

    const testOptions: FdUiRadioOption<string>[] = [
        { value: 'a', label: 'Option A' },
        { value: 'b', label: 'Option B', description: 'Description B' },
        { value: 'c', label: 'Option C' },
    ];

    const host = (): HTMLElement => fixture.nativeElement as HTMLElement;
    const requireElement = <T extends Element>(selector: string): T => {
        const element = host().querySelector<T>(selector);
        if (element === null) {
            throw new Error(`Expected element ${selector} to exist.`);
        }

        return element;
    };

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [FdUiRadioGroupComponent],
        }).compileComponents();

        fixture = TestBed.createComponent(FdUiRadioGroupComponent<string>);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should render label', () => {
        fixture.componentRef.setInput('label', 'Choose one');
        fixture.detectChanges();

        const label = requireElement<HTMLElement>('.fd-ui-radio-group__label');
        expect(label.textContent).toContain('Choose one');
    });

    it('should render options', () => {
        fixture.componentRef.setInput('options', testOptions);
        fixture.detectChanges();

        const radioButtons = Array.from(host().querySelectorAll<HTMLElement>('.fd-ui-radio'));
        expect(radioButtons.length).toBe(OPTION_COUNT);
        expect(radioButtons[0].textContent).toContain('Option A');
        expect(radioButtons[1].textContent).toContain('Option B');
        expect(radioButtons[2].textContent).toContain('Option C');
    });

    it('should write value via CVA', () => {
        fixture.componentRef.setInput('options', testOptions);
        fixture.detectChanges();

        component.writeValue('b');
        fixture.detectChanges();

        expect(component['internalValue']).toBe('b');
    });

    it('should call onChange on selection', () => {
        fixture.componentRef.setInput('options', testOptions);
        fixture.detectChanges();

        const onChangeSpy = vi.fn();
        component.registerOnChange(onChangeSpy);

        component['selectOption'](testOptions[0]);
        fixture.detectChanges();

        expect(onChangeSpy).toHaveBeenCalledWith('a');
    });

    it('should call onTouched on blur', () => {
        fixture.componentRef.setInput('options', testOptions);
        fixture.detectChanges();

        const onTouchedSpy = vi.fn();
        component.registerOnTouched(onTouchedSpy);

        const radioInput = fixture.debugElement.query(By.css('.fd-ui-radio__input'));
        radioInput.triggerEventHandler('blur', {});
        fixture.detectChanges();

        expect(onTouchedSpy).toHaveBeenCalled();
    });

    it('should set disabled state', () => {
        fixture.componentRef.setInput('options', testOptions);
        fixture.detectChanges();

        component.setDisabledState(true);
        fixture.detectChanges();

        expect(component['disabled']()).toBe(true);

        component.setDisabledState(false);
        fixture.detectChanges();

        expect(component['disabled']()).toBe(false);
    });

    it('should display error message', () => {
        fixture.componentRef.setInput('error', 'This field is required');
        fixture.detectChanges();

        const error = requireElement<HTMLElement>('.fd-ui-radio-group__error');
        expect(error.textContent).toContain('This field is required');
    });

    it('should display hint text', () => {
        fixture.componentRef.setInput('hint', 'Select your preference');
        fixture.detectChanges();

        const hint = requireElement<HTMLElement>('.fd-ui-radio-group__hint');
        expect(hint.textContent).toContain('Select your preference');
    });
});
