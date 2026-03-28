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
        expect(component['checked']).toBeTrue();

        component.writeValue(false);
        expect(component['checked']).toBeFalse();

        component.writeValue(null);
        expect(component['checked']).toBeFalse();
    });

    it('should call onChange when checkbox changes', () => {
        const onChangeSpy = jasmine.createSpy('onChange');
        component.registerOnChange(onChangeSpy);

        const changeEvent = { checked: true, source: {} } as MatCheckboxChange;
        component['handleChange'](changeEvent);

        expect(component['checked']).toBeTrue();
        expect(onChangeSpy).toHaveBeenCalledWith(true);

        const uncheckEvent = { checked: false, source: {} } as MatCheckboxChange;
        component['handleChange'](uncheckEvent);

        expect(component['checked']).toBeFalse();
        expect(onChangeSpy).toHaveBeenCalledWith(false);
    });

    it('should call onTouched on blur', () => {
        const onTouchedSpy = jasmine.createSpy('onTouched');
        component.registerOnTouched(onTouchedSpy);

        component['handleBlur']();

        expect(onTouchedSpy).toHaveBeenCalled();
    });

    it('should set disabled state via CVA', () => {
        component.setDisabledState(true);
        fixture.detectChanges();

        expect(component.disabled()).toBeTrue();

        component.setDisabledState(false);
        fixture.detectChanges();

        expect(component.disabled()).toBeFalse();
    });

    it('should handle null writeValue as false', () => {
        component.writeValue(true);
        expect(component['checked']).toBeTrue();

        component.writeValue(null);
        expect(component['checked']).toBeFalse();
    });
});
