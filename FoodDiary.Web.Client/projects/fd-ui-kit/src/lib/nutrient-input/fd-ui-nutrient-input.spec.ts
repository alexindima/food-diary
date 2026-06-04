import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';

import { FdUiNutrientInputComponent } from './fd-ui-nutrient-input';

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

describe('FdUiNutrientInputComponent signal form control', () => {
    it('should write value from model', async () => {
        const { component, fixture } = await setupNutrientInputAsync();
        component.value.set('42');
        fixture.detectChanges();
        expect(component['displayValue']).toBe('42');
    });

    it('should write numeric value from model and convert to string', async () => {
        const { component, fixture } = await setupNutrientInputAsync();
        component.value.set(NUMERIC_VALUE);
        fixture.detectChanges();
        expect(component['displayValue']).toBe(String(NUMERIC_VALUE));
    });

    it('should write null value as empty string', async () => {
        const { component, fixture } = await setupNutrientInputAsync();
        component.value.set('50');
        fixture.detectChanges();
        expect(component['displayValue']).toBe('50');

        component.value.set(null);
        fixture.detectChanges();
        expect(component['displayValue']).toBe('');
    });

    it('should set disabled state', async () => {
        const { fixture, input } = await setupNutrientInputAsync();

        fixture.componentRef.setInput('disabled', true);
        fixture.detectChanges();

        expect(input().disabled).toBe(true);
    });

    it('should mark touched on blur', async () => {
        const { component, input } = await setupNutrientInputAsync();

        input().dispatchEvent(new Event('blur'));

        expect(component.touched()).toBe(true);
    });
});

describe('FdUiNutrientInputComponent input handling', () => {
    it('should update value on input', async () => {
        const { component, input } = await setupNutrientInputAsync();

        const inputEl = input();
        inputEl.value = '123';
        inputEl.dispatchEvent(new Event('input'));

        expect(component.value()).toBe('123');
    });

    it('should sanitize non-numeric characters for number type', async () => {
        const { component, fixture, input } = await setupNutrientInputAsync();
        fixture.componentRef.setInput('type', 'number');

        const inputEl = input();
        inputEl.value = '12abc3.5';
        inputEl.dispatchEvent(new Event('input'));

        expect(component.value()).toBe('123.5');
        expect(component['displayValue']).toBe('123.5');
    });

    it('should replace comma with dot in number type', async () => {
        const { component, fixture, input } = await setupNutrientInputAsync();
        fixture.componentRef.setInput('type', 'number');

        const inputEl = input();
        inputEl.value = '12,5';
        inputEl.dispatchEvent(new Event('input'));

        expect(component.value()).toBe('12.5');
    });

    it('should not sanitize text type input', async () => {
        const { component, fixture, input } = await setupNutrientInputAsync();
        fixture.componentRef.setInput('type', 'text');

        const inputEl = input();
        inputEl.value = 'abc123';
        inputEl.dispatchEvent(new Event('input'));

        expect(component.value()).toBe('abc123');
        expect(component['displayValue']).toBe('abc123');
    });
});

describe('FdUiNutrientInputComponent dynamic width', () => {
    it('should update inputWidth based on value length', async () => {
        const { component, fixture } = await setupNutrientInputAsync();
        component.value.set('12345');
        fixture.detectChanges();
        expect(component['inputWidth']).toBe(`${CLAMPED_INPUT_SIZE}ch`);
        expect(component['inputSize']).toBe(CLAMPED_INPUT_SIZE);
    });

    it('should clamp inputWidth to maxInputChars', async () => {
        const { component, fixture } = await setupNutrientInputAsync();
        component.value.set('1234567890');
        fixture.detectChanges();
        expect(component['inputSize']).toBe(component['maxInputChars']);
        expect(component['inputWidth']).toBe(`${component['maxInputChars']}ch`);
    });

    it('should default inputWidth to 1ch for empty value', async () => {
        const { component, fixture } = await setupNutrientInputAsync();
        component.value.set('');
        fixture.detectChanges();
        expect(component['inputWidth']).toBe('1ch');
        expect(component['inputSize']).toBe(1);
    });
});
