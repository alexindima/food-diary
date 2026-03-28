import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FdUiNutrientInputComponent } from './fd-ui-nutrient-input.component';
import { provideNoopAnimations } from '@angular/platform-browser/animations';

describe('FdUiNutrientInputComponent', () => {
    let component: FdUiNutrientInputComponent;
    let fixture: ComponentFixture<FdUiNutrientInputComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [FdUiNutrientInputComponent],
            providers: [provideNoopAnimations()],
        }).compileComponents();

        fixture = TestBed.createComponent(FdUiNutrientInputComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should write value via CVA', () => {
        component.writeValue('42');
        expect(component.value).toBe('42');
    });

    it('should write numeric value via CVA and convert to string', () => {
        component.writeValue(100);
        expect(component.value).toBe('100');
    });

    it('should write null value via CVA as empty string', () => {
        component.writeValue('50');
        expect(component.value).toBe('50');

        component.writeValue(null);
        expect(component.value).toBe('');
    });

    it('should emit onChange on input', () => {
        const onChangeSpy = vi.fn();
        component.registerOnChange(onChangeSpy);

        const inputEl = fixture.nativeElement.querySelector('input') as HTMLInputElement;
        inputEl.value = '123';
        inputEl.dispatchEvent(new Event('input'));

        expect(onChangeSpy).toHaveBeenCalledWith('123');
    });

    it('should sanitize non-numeric characters for number type', () => {
        component.type = 'number';
        const onChangeSpy = vi.fn();
        component.registerOnChange(onChangeSpy);

        const inputEl = fixture.nativeElement.querySelector('input') as HTMLInputElement;
        inputEl.value = '12abc3.5';
        inputEl.dispatchEvent(new Event('input'));

        expect(onChangeSpy).toHaveBeenCalledWith('123.5');
        expect(component.value).toBe('123.5');
    });

    it('should replace comma with dot in number type', () => {
        component.type = 'number';
        const onChangeSpy = vi.fn();
        component.registerOnChange(onChangeSpy);

        const inputEl = fixture.nativeElement.querySelector('input') as HTMLInputElement;
        inputEl.value = '12,5';
        inputEl.dispatchEvent(new Event('input'));

        expect(onChangeSpy).toHaveBeenCalledWith('12.5');
    });

    it('should not sanitize text type input', () => {
        component.type = 'text';
        const onChangeSpy = vi.fn();
        component.registerOnChange(onChangeSpy);

        const inputEl = fixture.nativeElement.querySelector('input') as HTMLInputElement;
        inputEl.value = 'abc123';
        inputEl.dispatchEvent(new Event('input'));

        expect(onChangeSpy).toHaveBeenCalledWith('abc123');
        expect(component.value).toBe('abc123');
    });

    it('should set disabled state', () => {
        expect(component.disabled).toBe(false);

        component.setDisabledState(true);
        fixture.detectChanges();

        expect(component.disabled).toBe(true);
        const inputEl = fixture.nativeElement.querySelector('input') as HTMLInputElement;
        expect(inputEl.disabled).toBe(true);
    });

    it('should call onTouched on blur', () => {
        const onTouchedSpy = vi.fn();
        component.registerOnTouched(onTouchedSpy);

        const inputEl = fixture.nativeElement.querySelector('input') as HTMLInputElement;
        inputEl.dispatchEvent(new Event('blur'));

        expect(onTouchedSpy).toHaveBeenCalled();
    });

    it('should update inputWidth based on value length', () => {
        component.writeValue('12345');
        expect(component.inputWidth).toBe('5ch');
        expect(component.inputSize).toBe(5);
    });

    it('should clamp inputWidth to maxInputChars', () => {
        component.writeValue('1234567890');
        expect(component.inputSize).toBe(component.maxInputChars);
        expect(component.inputWidth).toBe(`${component.maxInputChars}ch`);
    });

    it('should default inputWidth to 1ch for empty value', () => {
        component.writeValue('');
        expect(component.inputWidth).toBe('1ch');
        expect(component.inputSize).toBe(1);
    });
});
