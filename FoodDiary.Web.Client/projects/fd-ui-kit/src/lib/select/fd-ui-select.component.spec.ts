import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it, vi } from 'vitest';

import { FdUiSelectComponent, type FdUiSelectOption } from './fd-ui-select.component';

const TEST_OPTIONS: Array<FdUiSelectOption<string>> = [
    { value: 'apple', label: 'Apple' },
    { value: 'banana', label: 'Banana' },
    { value: 'cherry', label: 'Cherry' },
];

type SelectTestContext = {
    component: FdUiSelectComponent<string>;
    fixture: ComponentFixture<FdUiSelectComponent<string>>;
    host: () => HTMLElement;
    requireElement: (selector: string) => HTMLElement;
};

async function setupSelectAsync(): Promise<SelectTestContext> {
    await TestBed.configureTestingModule({
        imports: [FdUiSelectComponent],
        providers: [],
    }).compileComponents();

    const fixture = TestBed.createComponent(FdUiSelectComponent<string>);
    const component = fixture.componentInstance;
    const host = (): HTMLElement => fixture.nativeElement as HTMLElement;
    const requireElement = (selector: string): HTMLElement => {
        const element = host().querySelector<HTMLElement>(selector);
        if (element === null) {
            throw new Error(`Expected element ${selector} to exist.`);
        }

        return element;
    };

    fixture.detectChanges();

    return { component, fixture, host, requireElement };
}

describe('FdUiSelectComponent', () => {
    it('should create', async () => {
        const { component } = await setupSelectAsync();

        expect(component).toBeTruthy();
    });
});

describe('FdUiSelectComponent rendering', () => {
    it('should render label', async () => {
        const { fixture, requireElement } = await setupSelectAsync();
        fixture.componentRef.setInput('label', 'Fruit');
        fixture.detectChanges();

        const labelEl = requireElement('.fd-ui-select__label-text');
        expect(labelEl.textContent.trim()).toBe('Fruit');
    });

    it('should show required asterisk', async () => {
        const { fixture, requireElement } = await setupSelectAsync();
        fixture.componentRef.setInput('label', 'Fruit');
        fixture.componentRef.setInput('required', true);
        fixture.detectChanges();

        const requiredEl = requireElement('.fd-ui-select__required');
        expect(requiredEl.textContent.trim()).toBe('*');
    });

    it('should display error message', async () => {
        const { fixture, requireElement } = await setupSelectAsync();
        fixture.componentRef.setInput('error', 'This field is required');
        fixture.detectChanges();

        const errorEl = requireElement('.fd-ui-select__error');
        expect(errorEl.textContent.trim()).toBe('This field is required');
    });
});

describe('FdUiSelectComponent CVA', () => {
    it('should write value via CVA', async () => {
        const { component, fixture } = await setupSelectAsync();
        fixture.componentRef.setInput('options', TEST_OPTIONS);
        fixture.detectChanges();

        component.writeValue('banana');
        fixture.detectChanges();

        expect(component['internalValue']()).toBe('banana');
    });

    it('should select option and update value', async () => {
        const { component, fixture } = await setupSelectAsync();
        fixture.componentRef.setInput('options', TEST_OPTIONS);
        fixture.detectChanges();

        const onChangeSpy = vi.fn();
        const onTouchedSpy = vi.fn();
        component.registerOnChange(onChangeSpy);
        component.registerOnTouched(onTouchedSpy);

        component['onOptionSelect'](TEST_OPTIONS[1]);
        fixture.detectChanges();

        expect(component['internalValue']()).toBe('banana');
        expect(onChangeSpy).toHaveBeenCalledWith('banana');
        expect(onTouchedSpy).toHaveBeenCalled();
    });

    it('should not select when disabled', async () => {
        const { component, fixture } = await setupSelectAsync();
        fixture.componentRef.setInput('options', TEST_OPTIONS);
        fixture.detectChanges();

        const onChangeSpy = vi.fn();
        component.registerOnChange(onChangeSpy);
        component.setDisabledState(true);

        component['onOptionSelect'](TEST_OPTIONS[0]);
        fixture.detectChanges();

        expect(component['internalValue']()).toBeNull();
        expect(onChangeSpy).not.toHaveBeenCalled();
    });

    it('should set disabled state via CVA', async () => {
        const { component } = await setupSelectAsync();
        component.setDisabledState(true);

        expect(component['disabled']()).toBe(true);
    });
});

describe('FdUiSelectComponent computed state', () => {
    it('should show selected label', async () => {
        const { component, fixture } = await setupSelectAsync();
        fixture.componentRef.setInput('options', TEST_OPTIONS);
        fixture.detectChanges();

        component.writeValue('cherry');
        fixture.detectChanges();

        expect(component['selectedLabel']()).toBe('Cherry');
    });

    it('should float label when has selection', async () => {
        const { component, fixture } = await setupSelectAsync();
        fixture.componentRef.setInput('label', 'Fruit');
        fixture.componentRef.setInput('options', TEST_OPTIONS);
        fixture.detectChanges();

        expect(component['shouldFloatLabel']()).toBe(false);

        component.writeValue('apple');

        expect(component['shouldFloatLabel']()).toBe(true);
    });

    it('should expose active option id when menu is open', async () => {
        const { component, fixture } = await setupSelectAsync();
        fixture.componentRef.setInput('options', TEST_OPTIONS);
        fixture.detectChanges();

        component['openMenu']();
        fixture.detectChanges();

        expect(component['activeOptionId']()).toBe(`${component.id()}-option-0`);
    });
});

describe('FdUiSelectComponent classes', () => {
    it('should apply size class', async () => {
        const { fixture, requireElement } = await setupSelectAsync();
        fixture.componentRef.setInput('size', 'lg');
        fixture.detectChanges();

        const selectEl = requireElement('.fd-ui-select');
        expect(selectEl.classList).toContain('fd-ui-select--size-lg');
    });
});

describe('FdUiSelectComponent overlay', () => {
    it('should focus listbox when overlay attaches', async () => {
        const { component } = await setupSelectAsync();
        const focus = vi.fn();
        Object.defineProperty(component, 'listboxRef', {
            value: vi.fn(() => ({
                nativeElement: { focus },
            })),
        });

        component['onMenuAttached']();
        await Promise.resolve();

        expect(focus).toHaveBeenCalled();
    });
});
