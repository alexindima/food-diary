import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it, vi } from 'vitest';

import { FdUiNutrientInputComponent } from './fd-ui-nutrient-input.component';

const NUMERIC_VALUE = 100;
const CLAMPED_INPUT_SIZE = 5;

type NutrientInputTestContext = {
    component: FdUiNutrientInputComponent;
    fixture: ComponentFixture<FdUiNutrientInputComponent>;
    input: () => HTMLInputElement;
};

async function setupNutrientInputAsync(): Promise<NutrientInputTestContext> {
    await TestBed.configureTestingModule({
        imports: [FdUiNutrientInputComponent],
        providers: [],
    }).compileComponents();

    const fixture = TestBed.createComponent(FdUiNutrientInputComponent);
    const component = fixture.componentInstance;
    const host = (): HTMLElement => fixture.nativeElement as HTMLElement;
    const input = (): HTMLInputElement => {
        const element = host().querySelector<HTMLInputElement>('input');
        if (element === null) {
            throw new Error('Expected nutrient input to exist.');
        }

        return element;
    };

    fixture.detectChanges();

    return { component, fixture, input };
}

describe('FdUiNutrientInputComponent', () => {
    it('should create', async () => {
        const { component } = await setupNutrientInputAsync();

        expect(component).toBeTruthy();
    });
});

describe('FdUiNutrientInputComponent CVA', () => {
    it('should write value via CVA', async () => {
        const { component } = await setupNutrientInputAsync();
        component.writeValue('42');
        expect(component.value).toBe('42');
    });

    it('should write numeric value via CVA and convert to string', async () => {
        const { component } = await setupNutrientInputAsync();
        component.writeValue(NUMERIC_VALUE);
        expect(component.value).toBe(String(NUMERIC_VALUE));
    });

    it('should write null value via CVA as empty string', async () => {
        const { component } = await setupNutrientInputAsync();
        component.writeValue('50');
        expect(component.value).toBe('50');

        component.writeValue(null);
        expect(component.value).toBe('');
    });

    it('should set disabled state', async () => {
        const { component, fixture, input } = await setupNutrientInputAsync();
        expect(component.disabled).toBe(false);

        component.setDisabledState(true);
        fixture.detectChanges();

        expect(component.disabled).toBe(true);
        expect(input().disabled).toBe(true);
    });

    it('should call onTouched on blur', async () => {
        const { component, input } = await setupNutrientInputAsync();
        const onTouchedSpy = vi.fn();
        component.registerOnTouched(onTouchedSpy);

        input().dispatchEvent(new Event('blur'));

        expect(onTouchedSpy).toHaveBeenCalled();
    });
});

describe('FdUiNutrientInputComponent input handling', () => {
    it('should emit onChange on input', async () => {
        const { component, input } = await setupNutrientInputAsync();
        const onChangeSpy = vi.fn();
        component.registerOnChange(onChangeSpy);

        const inputEl = input();
        inputEl.value = '123';
        inputEl.dispatchEvent(new Event('input'));

        expect(onChangeSpy).toHaveBeenCalledWith('123');
    });

    it('should sanitize non-numeric characters for number type', async () => {
        const { component, fixture, input } = await setupNutrientInputAsync();
        fixture.componentRef.setInput('type', 'number');
        const onChangeSpy = vi.fn();
        component.registerOnChange(onChangeSpy);

        const inputEl = input();
        inputEl.value = '12abc3.5';
        inputEl.dispatchEvent(new Event('input'));

        expect(onChangeSpy).toHaveBeenCalledWith('123.5');
        expect(component.value).toBe('123.5');
    });

    it('should replace comma with dot in number type', async () => {
        const { component, fixture, input } = await setupNutrientInputAsync();
        fixture.componentRef.setInput('type', 'number');
        const onChangeSpy = vi.fn();
        component.registerOnChange(onChangeSpy);

        const inputEl = input();
        inputEl.value = '12,5';
        inputEl.dispatchEvent(new Event('input'));

        expect(onChangeSpy).toHaveBeenCalledWith('12.5');
    });

    it('should not sanitize text type input', async () => {
        const { component, fixture, input } = await setupNutrientInputAsync();
        fixture.componentRef.setInput('type', 'text');
        const onChangeSpy = vi.fn();
        component.registerOnChange(onChangeSpy);

        const inputEl = input();
        inputEl.value = 'abc123';
        inputEl.dispatchEvent(new Event('input'));

        expect(onChangeSpy).toHaveBeenCalledWith('abc123');
        expect(component.value).toBe('abc123');
    });
});

describe('FdUiNutrientInputComponent dynamic width', () => {
    it('should update inputWidth based on value length', async () => {
        const { component } = await setupNutrientInputAsync();
        component.writeValue('12345');
        expect(component.inputWidth).toBe(`${CLAMPED_INPUT_SIZE}ch`);
        expect(component.inputSize).toBe(CLAMPED_INPUT_SIZE);
    });

    it('should clamp inputWidth to maxInputChars', async () => {
        const { component } = await setupNutrientInputAsync();
        component.writeValue('1234567890');
        expect(component.inputSize).toBe(component.maxInputChars);
        expect(component.inputWidth).toBe(`${component.maxInputChars}ch`);
    });

    it('should default inputWidth to 1ch for empty value', async () => {
        const { component } = await setupNutrientInputAsync();
        component.writeValue('');
        expect(component.inputWidth).toBe('1ch');
        expect(component.inputSize).toBe(1);
    });
});
