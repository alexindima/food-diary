import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { MatCheckboxChange } from '@angular/material/checkbox';
import { FdUiCheckboxComponent } from './fd-ui-checkbox.component';

describe('FdUiCheckboxComponent', () => {
    let component: FdUiCheckboxComponent;
    let fixture: ComponentFixture<FdUiCheckboxComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [FdUiCheckboxComponent],
            providers: [provideNoopAnimations()],
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

        const checkboxEl = fixture.nativeElement.querySelector('mat-checkbox');
        expect(checkboxEl.textContent.trim()).toContain('Accept terms');
    });

    it('should render hint when provided', () => {
        fixture.componentRef.setInput('hint', 'Please read carefully');
        fixture.detectChanges();

        const hintEl = fixture.nativeElement.querySelector('.fd-ui-checkbox__hint');
        expect(hintEl).toBeTruthy();
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

        const changeEvent: MatCheckboxChange = { checked: true, source: undefined as never };
        component['handleChange'](changeEvent);

        expect(component['checked']).toBe(true);
        expect(onChangeSpy).toHaveBeenCalledWith(true);

        const uncheckEvent: MatCheckboxChange = { checked: false, source: undefined as never };
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
