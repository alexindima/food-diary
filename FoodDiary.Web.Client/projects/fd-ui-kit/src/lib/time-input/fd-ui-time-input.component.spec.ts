import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FdUiTimeInputComponent } from './fd-ui-time-input.component';
import { provideNoopAnimations } from '@angular/platform-browser/animations';

describe('FdUiTimeInputComponent', () => {
    let component: FdUiTimeInputComponent;
    let fixture: ComponentFixture<FdUiTimeInputComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [FdUiTimeInputComponent],
            providers: [provideNoopAnimations()],
        }).compileComponents();

        fixture = TestBed.createComponent(FdUiTimeInputComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should render label', () => {
        fixture.componentRef.setInput('label', 'Meal Time');
        fixture.detectChanges();

        const label = fixture.nativeElement.querySelector('.fd-ui-time-input__label-text');
        expect(label).toBeTruthy();
        expect(label.textContent).toContain('Meal Time');
    });

    it('should not render label when not provided', () => {
        const label = fixture.nativeElement.querySelector('.fd-ui-time-input__label');
        expect(label).toBeNull();
    });

    it('should show required asterisk', () => {
        fixture.componentRef.setInput('label', 'Time');
        fixture.componentRef.setInput('required', true);
        fixture.detectChanges();

        const asterisk = fixture.nativeElement.querySelector('.fd-ui-time-input__required');
        expect(asterisk).toBeTruthy();
        expect(asterisk.textContent).toContain('*');
    });

    it('should write value via CVA', () => {
        component.writeValue('14:30');
        expect(component['internalValue']).toBe('14:30');
    });

    it('should write null value via CVA as empty string', () => {
        component.writeValue('10:00');
        expect(component['internalValue']).toBe('10:00');

        component.writeValue(null);
        expect(component['internalValue']).toBe('');
    });

    it('should display error', () => {
        fixture.componentRef.setInput('error', 'Invalid time');
        fixture.detectChanges();

        const errorEl = fixture.nativeElement.querySelector('.fd-ui-time-input__error');
        expect(errorEl).toBeTruthy();
        expect(errorEl.textContent).toContain('Invalid time');
    });

    it('should not display error when null', () => {
        fixture.componentRef.setInput('error', null);
        fixture.detectChanges();

        const errorEl = fixture.nativeElement.querySelector('.fd-ui-time-input__error');
        expect(errorEl).toBeNull();
    });

    it('should set disabled state', () => {
        component.setDisabledState(true);

        expect(component['disabled']).toBe(true);
    });

    it('should re-enable after being disabled', () => {
        component.setDisabledState(true);
        component.setDisabledState(false);
        fixture.detectChanges();

        expect(component['disabled']).toBe(false);
    });

    it('should apply size class', () => {
        fixture.componentRef.setInput('size', 'lg');
        fixture.detectChanges();

        const container = fixture.nativeElement.querySelector('.fd-ui-time-input');
        expect(container.classList).toContain('fd-ui-time-input--size-lg');
    });

    it('should default to md size class', () => {
        const container = fixture.nativeElement.querySelector('.fd-ui-time-input');
        expect(container.classList).toContain('fd-ui-time-input--size-md');
    });

    it('should call onChange with formatted time on valid input', () => {
        const onChangeSpy = vi.fn();
        component.registerOnChange(onChangeSpy);

        component['onInput']('14:30');

        expect(onChangeSpy).toHaveBeenCalledWith('14:30');
        expect(component['internalValue']).toBe('14:30');
    });

    it('should call onChange with null on empty input', () => {
        const onChangeSpy = vi.fn();
        component.registerOnChange(onChangeSpy);

        component['onInput']('');

        expect(onChangeSpy).toHaveBeenCalledWith(null);
        expect(component['internalValue']).toBe('');
    });

    it('should not call onChange on invalid time input', () => {
        const onChangeSpy = vi.fn();
        component.registerOnChange(onChangeSpy);

        component['onInput']('abc');

        expect(onChangeSpy).not.toHaveBeenCalled();
        expect(component['internalValue']).toBe('abc');
    });

    it('should not process input when disabled', () => {
        const onChangeSpy = vi.fn();
        component.registerOnChange(onChangeSpy);
        component.setDisabledState(true);

        component['onInput']('14:30');

        expect(onChangeSpy).not.toHaveBeenCalled();
    });

    it('should call onTouched on blur', () => {
        const onTouchedSpy = vi.fn();
        component.registerOnTouched(onTouchedSpy);

        component['onBlur']();

        expect(onTouchedSpy).toHaveBeenCalled();
    });

    it('should pad single-digit hours and minutes', () => {
        const onChangeSpy = vi.fn();
        component.registerOnChange(onChangeSpy);

        component['onInput']('9:05');

        expect(onChangeSpy).toHaveBeenCalledWith('09:05');
        expect(component['internalValue']).toBe('09:05');
    });

    it('should reject hours above 23', () => {
        const onChangeSpy = vi.fn();
        component.registerOnChange(onChangeSpy);

        component['onInput']('25:00');

        expect(onChangeSpy).not.toHaveBeenCalled();
    });

    it('should reject minutes above 59', () => {
        const onChangeSpy = vi.fn();
        component.registerOnChange(onChangeSpy);

        component['onInput']('12:60');

        expect(onChangeSpy).not.toHaveBeenCalled();
    });
});
