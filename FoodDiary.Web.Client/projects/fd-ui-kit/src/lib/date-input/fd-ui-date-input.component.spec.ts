import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FdUiDateInputComponent } from './fd-ui-date-input.component';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { provideNativeDateAdapter } from '@angular/material/core';

describe('FdUiDateInputComponent', () => {
    let component: FdUiDateInputComponent;
    let fixture: ComponentFixture<FdUiDateInputComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [FdUiDateInputComponent],
            providers: [provideNoopAnimations(), provideNativeDateAdapter()],
        }).compileComponents();

        fixture = TestBed.createComponent(FdUiDateInputComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should render label', () => {
        fixture.componentRef.setInput('label', 'Date of Birth');
        fixture.detectChanges();

        const label = fixture.nativeElement.querySelector('.fd-ui-date-input__label-text');
        expect(label).toBeTruthy();
        expect(label.textContent).toContain('Date of Birth');
    });

    it('should not render label when not provided', () => {
        const label = fixture.nativeElement.querySelector('.fd-ui-date-input__label');
        expect(label).toBeNull();
    });

    it('should show required asterisk', () => {
        fixture.componentRef.setInput('label', 'Date');
        fixture.componentRef.setInput('required', true);
        fixture.detectChanges();

        const asterisk = fixture.nativeElement.querySelector('.fd-ui-date-input__required');
        expect(asterisk).toBeTruthy();
        expect(asterisk.textContent).toContain('*');
    });

    it('should not show required asterisk when not required', () => {
        fixture.componentRef.setInput('label', 'Date');
        fixture.componentRef.setInput('required', false);
        fixture.detectChanges();

        const asterisk = fixture.nativeElement.querySelector('.fd-ui-date-input__required');
        expect(asterisk).toBeNull();
    });

    it('should write value via CVA with string', () => {
        component.writeValue('2025-03-15');

        const dateControl = component['dateControl'];
        expect(dateControl.value).toBeTruthy();
        expect(dateControl.value!.getFullYear()).toBe(2025);
        expect(dateControl.value!.getMonth()).toBe(2); // March = 2 (zero-indexed)
        expect(dateControl.value!.getDate()).toBe(15);
    });

    it('should write null value via CVA', () => {
        component.writeValue('2025-01-01');
        expect(component['dateControl'].value).toBeTruthy();

        component.writeValue(null);
        expect(component['dateControl'].value).toBeNull();
    });

    it('should write Date object via CVA', () => {
        const date = new Date(2025, 5, 20);
        component.writeValue(date);

        const dateControl = component['dateControl'];
        expect(dateControl.value).toBeTruthy();
        expect(dateControl.value!.getFullYear()).toBe(2025);
        expect(dateControl.value!.getMonth()).toBe(5);
        expect(dateControl.value!.getDate()).toBe(20);
    });

    it('should display error', () => {
        fixture.componentRef.setInput('error', 'Date is required');
        fixture.detectChanges();

        const errorEl = fixture.nativeElement.querySelector('.fd-ui-date-input__error');
        expect(errorEl).toBeTruthy();
        expect(errorEl.textContent).toContain('Date is required');
    });

    it('should not display error when null', () => {
        fixture.componentRef.setInput('error', null);
        fixture.detectChanges();

        const errorEl = fixture.nativeElement.querySelector('.fd-ui-date-input__error');
        expect(errorEl).toBeNull();
    });

    it('should apply size class', () => {
        fixture.componentRef.setInput('size', 'lg');
        fixture.detectChanges();

        const container = fixture.nativeElement.querySelector('.fd-ui-date-input');
        expect(container.classList).toContain('fd-ui-date-input--size-lg');
    });

    it('should default to md size class', () => {
        const container = fixture.nativeElement.querySelector('.fd-ui-date-input');
        expect(container.classList).toContain('fd-ui-date-input--size-md');
    });

    it('should set disabled state', () => {
        component.setDisabledState(true);
        fixture.detectChanges();

        expect(component['disabled']).toBe(true);
        expect(component['dateControl'].disabled).toBe(true);

        const inputEl = fixture.nativeElement.querySelector('.fd-ui-date-input__control') as HTMLInputElement;
        expect(inputEl.disabled).toBe(true);
    });

    it('should re-enable after being disabled', () => {
        component.setDisabledState(true);
        component.setDisabledState(false);
        fixture.detectChanges();

        expect(component['disabled']).toBe(false);
        expect(component['dateControl'].enabled).toBe(true);
    });

    it('should call onChange with formatted date string when dateControl value changes', () => {
        const onChangeSpy = vi.fn();
        component.registerOnChange(onChangeSpy);

        component['dateControl'].setValue(new Date(2025, 2, 15));

        expect(onChangeSpy).toHaveBeenCalledWith('2025-03-15');
    });

    it('should call onChange with null when dateControl is cleared', () => {
        const onChangeSpy = vi.fn();
        component.registerOnChange(onChangeSpy);

        component['dateControl'].setValue(new Date(2025, 0, 1));
        onChangeSpy.mockClear();

        component['dateControl'].setValue(null);

        expect(onChangeSpy).toHaveBeenCalledWith(null);
    });
});
