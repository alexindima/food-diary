import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { FdUiSelectComponent, FdUiSelectOption } from './fd-ui-select.component';

describe('FdUiSelectComponent', () => {
    let component: FdUiSelectComponent<string>;
    let fixture: ComponentFixture<FdUiSelectComponent<string>>;

    const testOptions: FdUiSelectOption<string>[] = [
        { value: 'apple', label: 'Apple' },
        { value: 'banana', label: 'Banana' },
        { value: 'cherry', label: 'Cherry' },
    ];

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [FdUiSelectComponent],
            providers: [provideNoopAnimations()],
        }).compileComponents();

        fixture = TestBed.createComponent(FdUiSelectComponent<string>);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should render label', () => {
        fixture.componentRef.setInput('label', 'Fruit');
        fixture.detectChanges();

        const labelEl = fixture.nativeElement.querySelector('.fd-ui-select__label-text');
        expect(labelEl).toBeTruthy();
        expect(labelEl.textContent.trim()).toBe('Fruit');
    });

    it('should show required asterisk', () => {
        fixture.componentRef.setInput('label', 'Fruit');
        fixture.componentRef.setInput('required', true);
        fixture.detectChanges();

        const requiredEl = fixture.nativeElement.querySelector('.fd-ui-select__required');
        expect(requiredEl).toBeTruthy();
        expect(requiredEl.textContent.trim()).toBe('*');
    });

    it('should display error message', () => {
        fixture.componentRef.setInput('error', 'This field is required');
        fixture.detectChanges();

        const errorEl = fixture.nativeElement.querySelector('.fd-ui-select__error');
        expect(errorEl).toBeTruthy();
        expect(errorEl.textContent.trim()).toBe('This field is required');
    });

    it('should write value via CVA', () => {
        fixture.componentRef.setInput('options', testOptions);
        fixture.detectChanges();

        component.writeValue('banana');
        fixture.detectChanges();

        expect(component['internalValue']).toBe('banana');
    });

    it('should select option and update value', () => {
        fixture.componentRef.setInput('options', testOptions);
        fixture.detectChanges();

        const onChangeSpy = vi.fn();
        const onTouchedSpy = vi.fn();
        component.registerOnChange(onChangeSpy);
        component.registerOnTouched(onTouchedSpy);

        component['onOptionSelect'](testOptions[1]);
        fixture.detectChanges();

        expect(component['internalValue']).toBe('banana');
        expect(onChangeSpy).toHaveBeenCalledWith('banana');
        expect(onTouchedSpy).toHaveBeenCalled();
    });

    it('should not select when disabled', () => {
        fixture.componentRef.setInput('options', testOptions);
        fixture.detectChanges();

        const onChangeSpy = vi.fn();
        component.registerOnChange(onChangeSpy);
        component.setDisabledState(true);

        component['onOptionSelect'](testOptions[0]);
        fixture.detectChanges();

        expect(component['internalValue']).toBeNull();
        expect(onChangeSpy).not.toHaveBeenCalled();
    });

    it('should show selected label', () => {
        fixture.componentRef.setInput('options', testOptions);
        fixture.detectChanges();

        component.writeValue('cherry');
        fixture.detectChanges();

        expect(component['selectedLabel']).toBe('Cherry');
    });

    it('should float label when has selection', () => {
        fixture.componentRef.setInput('label', 'Fruit');
        fixture.componentRef.setInput('options', testOptions);
        fixture.detectChanges();

        expect(component['shouldFloatLabel']).toBe(false);

        component.writeValue('apple');

        expect(component['shouldFloatLabel']).toBe(true);
    });

    it('should apply size class', () => {
        fixture.componentRef.setInput('size', 'lg');
        fixture.detectChanges();

        const selectEl = fixture.nativeElement.querySelector('.fd-ui-select');
        expect(selectEl.classList).toContain('fd-ui-select--size-lg');
    });

    it('should set disabled state via CVA', () => {
        component.setDisabledState(true);

        expect(component['disabled']).toBe(true);
    });

    it('should expose active option id when menu is open', () => {
        fixture.componentRef.setInput('options', testOptions);
        fixture.detectChanges();

        component['openMenu']();
        fixture.detectChanges();

        expect(component['activeOptionId']).toBe(`${component.id()}-option-0`);
    });

    it('should focus listbox when overlay attaches', async () => {
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
