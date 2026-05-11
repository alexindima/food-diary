import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { describe, expect, it, vi } from 'vitest';

import { FdUiRadioGroupComponent, type FdUiRadioOption } from './fd-ui-radio-group.component';

const OPTION_COUNT = 3;
const TEST_OPTIONS: FdUiRadioOption<string>[] = [
    { value: 'a', label: 'Option A' },
    { value: 'b', label: 'Option B', description: 'Description B' },
    { value: 'c', label: 'Option C' },
];

interface RadioGroupTestContext {
    component: FdUiRadioGroupComponent<string>;
    fixture: ComponentFixture<FdUiRadioGroupComponent<string>>;
    host: () => HTMLElement;
    requireElement: <T extends Element>(selector: string) => T;
}

async function setupRadioGroupAsync(): Promise<RadioGroupTestContext> {
    await TestBed.configureTestingModule({
        imports: [FdUiRadioGroupComponent],
    }).compileComponents();

    const fixture = TestBed.createComponent(FdUiRadioGroupComponent<string>);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    const host = (): HTMLElement => fixture.nativeElement as HTMLElement;
    const requireElement = <T extends Element>(selector: string): T => {
        const element = host().querySelector<T>(selector);
        if (element === null) {
            throw new Error(`Expected element ${selector} to exist.`);
        }

        return element;
    };

    return { component, fixture, host, requireElement };
}

function setRadioOptions(fixture: ComponentFixture<FdUiRadioGroupComponent<string>>): void {
    fixture.componentRef.setInput('options', TEST_OPTIONS);
    fixture.detectChanges();
}

describe('FdUiRadioGroupComponent', () => {
    it('should create', async () => {
        const { component } = await setupRadioGroupAsync();

        expect(component).toBeTruthy();
    });

    it('should render label', async () => {
        const { fixture, requireElement } = await setupRadioGroupAsync();

        fixture.componentRef.setInput('label', 'Choose one');
        fixture.detectChanges();

        const label = requireElement<HTMLElement>('.fd-ui-radio-group__label');
        expect(label.textContent).toContain('Choose one');
    });

    it('should render options', async () => {
        const { fixture, host } = await setupRadioGroupAsync();

        setRadioOptions(fixture);
        const radioButtons = Array.from(host().querySelectorAll<HTMLElement>('.fd-ui-radio'));
        expect(radioButtons.length).toBe(OPTION_COUNT);
        expect(radioButtons[0].textContent).toContain('Option A');
        expect(radioButtons[1].textContent).toContain('Option B');
        expect(radioButtons[2].textContent).toContain('Option C');
    });
});

describe('FdUiRadioGroupComponent CVA', () => {
    it('should write value via CVA', async () => {
        const { component, fixture } = await setupRadioGroupAsync();

        setRadioOptions(fixture);
        component.writeValue('b');
        fixture.detectChanges();

        expect(component['internalValue']).toBe('b');
    });

    it('should call onChange on selection', async () => {
        const { component, fixture } = await setupRadioGroupAsync();
        const onChangeSpy = vi.fn();

        setRadioOptions(fixture);
        component.registerOnChange(onChangeSpy);

        component['selectOption'](TEST_OPTIONS[0]);
        fixture.detectChanges();

        expect(onChangeSpy).toHaveBeenCalledWith('a');
    });

    it('should call onTouched on blur', async () => {
        const { component, fixture } = await setupRadioGroupAsync();
        const onTouchedSpy = vi.fn();

        setRadioOptions(fixture);
        component.registerOnTouched(onTouchedSpy);

        const radioInput = fixture.debugElement.query(By.css('.fd-ui-radio__input'));
        radioInput.triggerEventHandler('blur', {});
        fixture.detectChanges();

        expect(onTouchedSpy).toHaveBeenCalled();
    });

    it('should set disabled state', async () => {
        const { component, fixture } = await setupRadioGroupAsync();

        setRadioOptions(fixture);
        component.setDisabledState(true);
        fixture.detectChanges();

        expect(component['disabled']()).toBe(true);

        component.setDisabledState(false);
        fixture.detectChanges();

        expect(component['disabled']()).toBe(false);
    });
});

describe('FdUiRadioGroupComponent helper text', () => {
    it('should display error message', async () => {
        const { fixture, requireElement } = await setupRadioGroupAsync();

        fixture.componentRef.setInput('error', 'This field is required');
        fixture.detectChanges();

        const error = requireElement<HTMLElement>('.fd-ui-radio-group__error');
        expect(error.textContent).toContain('This field is required');
    });

    it('should display hint text', async () => {
        const { fixture, requireElement } = await setupRadioGroupAsync();

        fixture.componentRef.setInput('hint', 'Select your preference');
        fixture.detectChanges();

        const hint = requireElement<HTMLElement>('.fd-ui-radio-group__hint');
        expect(hint.textContent).toContain('Select your preference');
    });
});
