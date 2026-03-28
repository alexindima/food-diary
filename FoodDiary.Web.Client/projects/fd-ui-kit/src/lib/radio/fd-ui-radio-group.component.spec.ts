import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { FdUiRadioGroupComponent, FdUiRadioOption } from './fd-ui-radio-group.component';

describe('FdUiRadioGroupComponent', () => {
    let component: FdUiRadioGroupComponent<string>;
    let fixture: ComponentFixture<FdUiRadioGroupComponent<string>>;

    const testOptions: FdUiRadioOption<string>[] = [
        { value: 'a', label: 'Option A' },
        { value: 'b', label: 'Option B', description: 'Description B' },
        { value: 'c', label: 'Option C' },
    ];

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [FdUiRadioGroupComponent],
            providers: [provideNoopAnimations()],
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

        const label = fixture.debugElement.query(By.css('.fd-ui-radio-group__label'));
        expect(label).toBeTruthy();
        expect(label.nativeElement.textContent).toContain('Choose one');
    });

    it('should render options', () => {
        fixture.componentRef.setInput('options', testOptions);
        fixture.detectChanges();

        const radioButtons = fixture.debugElement.queryAll(By.css('mat-radio-button'));
        expect(radioButtons.length).toBe(3);
        expect(radioButtons[0].nativeElement.textContent).toContain('Option A');
        expect(radioButtons[1].nativeElement.textContent).toContain('Option B');
        expect(radioButtons[2].nativeElement.textContent).toContain('Option C');
    });

    it('should write value via CVA', () => {
        fixture.componentRef.setInput('options', testOptions);
        fixture.detectChanges();

        component.writeValue('b');
        fixture.detectChanges();

        expect(component['internalValue']).toBe('b');
        expect(component['control'].value).toBe('b');
    });

    it('should call onChange on selection', () => {
        fixture.componentRef.setInput('options', testOptions);
        fixture.detectChanges();

        const onChangeSpy = jasmine.createSpy('onChange');
        component.registerOnChange(onChangeSpy);

        component['control'].setValue('a');
        fixture.detectChanges();

        expect(onChangeSpy).toHaveBeenCalledWith('a');
    });

    it('should call onTouched on blur', () => {
        fixture.componentRef.setInput('options', testOptions);
        fixture.detectChanges();

        const onTouchedSpy = jasmine.createSpy('onTouched');
        component.registerOnTouched(onTouchedSpy);

        const radioGroup = fixture.debugElement.query(By.css('mat-radio-group'));
        radioGroup.triggerEventHandler('blur', {});
        fixture.detectChanges();

        expect(onTouchedSpy).toHaveBeenCalled();
    });

    it('should set disabled state', () => {
        fixture.componentRef.setInput('options', testOptions);
        fixture.detectChanges();

        component.setDisabledState(true);
        fixture.detectChanges();

        expect(component['disabled']).toBeTrue();
        expect(component['control'].disabled).toBeTrue();

        component.setDisabledState(false);
        fixture.detectChanges();

        expect(component['disabled']).toBeFalse();
        expect(component['control'].enabled).toBeTrue();
    });

    it('should display error message', () => {
        fixture.componentRef.setInput('error', 'This field is required');
        fixture.detectChanges();

        const error = fixture.debugElement.query(By.css('.fd-ui-radio-group__error'));
        expect(error).toBeTruthy();
        expect(error.nativeElement.textContent).toContain('This field is required');
    });

    it('should display hint text', () => {
        fixture.componentRef.setInput('hint', 'Select your preference');
        fixture.detectChanges();

        const hint = fixture.debugElement.query(By.css('.fd-ui-radio-group__hint'));
        expect(hint).toBeTruthy();
        expect(hint.nativeElement.textContent).toContain('Select your preference');
    });
});
