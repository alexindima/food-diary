import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { FdUiCheckboxComponent } from './fd-ui-checkbox.component';

const dispatchCheckboxChange = (checked: boolean): Event => {
    const input = document.createElement('input');
    input.type = 'checkbox';
    input.checked = checked;
    const event = new Event('change');
    input.dispatchEvent(event);
    return event;
};

describe('FdUiCheckboxComponent', () => {
    let component: FdUiCheckboxComponent;
    let fixture: ComponentFixture<FdUiCheckboxComponent>;

    const host = (): HTMLElement => fixture.nativeElement as HTMLElement;
    const requireElement = (selector: string): HTMLElement => {
        const element = host().querySelector<HTMLElement>(selector);
        if (element === null) {
            throw new Error(`Expected element ${selector} to exist.`);
        }

        return element;
    };

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [FdUiCheckboxComponent],
        }).compileComponents();

        fixture = TestBed.createComponent(FdUiCheckboxComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should render label text', () => {
        fixture.componentRef.setInput('label', 'Accept terms');
        fixture.detectChanges();

        const checkboxEl = requireElement('.fd-ui-checkbox__label');
        expect(checkboxEl.textContent.trim()).toContain('Accept terms');
    });

    it('should render hint when provided', () => {
        fixture.componentRef.setInput('hint', 'Please read carefully');
        fixture.detectChanges();

        const hintEl = requireElement('.fd-ui-checkbox__hint');
        expect(hintEl.textContent.trim()).toBe('Please read carefully');
    });

    it('should write value via CVA (true/false/null)', () => {
        component.writeValue(true);
        expect(component['checked']).toBe(true);

        component.writeValue(false);
        expect(component['checked']).toBe(false);

        component.writeValue(null);
        expect(component['checked']).toBe(false);
    });

    it('should call onChange when checkbox changes', () => {
        const onChangeSpy = vi.fn();
        component.registerOnChange(onChangeSpy);

        const changeEvent = dispatchCheckboxChange(true);
        component['handleChange'](changeEvent);

        expect(component['checked']).toBe(true);
        expect(onChangeSpy).toHaveBeenCalledWith(true);

        const uncheckEvent = dispatchCheckboxChange(false);
        component['handleChange'](uncheckEvent);

        expect(component['checked']).toBe(false);
        expect(onChangeSpy).toHaveBeenCalledWith(false);
    });

    it('should call onTouched on blur', () => {
        const onTouchedSpy = vi.fn();
        component.registerOnTouched(onTouchedSpy);

        component['handleBlur']();

        expect(onTouchedSpy).toHaveBeenCalled();
    });

    it('should set disabled state via CVA', () => {
        component.setDisabledState(true);
        fixture.detectChanges();

        expect(component.disabled()).toBe(true);

        component.setDisabledState(false);
        fixture.detectChanges();

        expect(component.disabled()).toBe(false);
    });

    it('should handle null writeValue as false', () => {
        component.writeValue(true);
        expect(component['checked']).toBe(true);

        component.writeValue(null);
        expect(component['checked']).toBe(false);
    });
});
