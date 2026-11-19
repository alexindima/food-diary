import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { form, FormField } from '@angular/forms/signals';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { FdUiCheckboxComponent } from './fd-ui-checkbox';

@Component({
    imports: [FdUiCheckboxComponent, FormField],
    template: '<fd-ui-checkbox label="Enabled" [formField]="fields.enabled" />',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
class CheckboxFormHost {
    public readonly value = signal({ enabled: true });
    public readonly fields = form(this.value);
}

describe('FdUiCheckboxComponent', () => {
    let component: FdUiCheckboxComponent;
    let fixture: ComponentFixture<FdUiCheckboxComponent>;

    const host = (): HTMLElement => fixture.nativeElement as HTMLElement;
    const requireElement = (selector: string): HTMLElement => {
        const element = host().querySelector<HTMLElement>(selector);
        if (element === null) {
            throw new Error(`Expected element ${selector} to exist.`);
        }

        return element;
    };

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [FdUiCheckboxComponent],
        }).compileComponents();

        fixture = TestBed.createComponent(FdUiCheckboxComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should render label text', () => {
        fixture.componentRef.setInput('label', 'Accept terms');
        fixture.detectChanges();

        const checkboxEl = requireElement('.fd-ui-checkbox__label');
        expect(checkboxEl.textContent.trim()).toContain('Accept terms');
    });

    it('should render hint when provided', () => {
        fixture.componentRef.setInput('hint', 'Please read carefully');
        fixture.detectChanges();

        const hintEl = requireElement('.fd-ui-checkbox__hint');
        expect(hintEl.textContent.trim()).toBe('Please read carefully');
        expect(requireElement('input').getAttribute('aria-describedby')).toBe(hintEl.id);
    });

    it('should reflect external checked model changes in the native checkbox', () => {
        const input = host().querySelector<HTMLInputElement>('input');
        component.checked.set(true);
        fixture.detectChanges();
        expect(input?.checked).toBe(true);

        component.checked.set(false);
        fixture.detectChanges();
        expect(input?.checked).toBe(false);
    });

    it('updates the checked model through the label and native checkbox and ignores disabled label clicks', () => {
        const input = requireElement('input');
        const label = requireElement('label');
        const changed = vi.fn();
        component.checked.subscribe(changed);
        label.click();
        fixture.detectChanges();
        expect(component.checked()).toBe(true);

        input.click();
        fixture.detectChanges();
        expect(component.checked()).toBe(false);

        fixture.componentRef.setInput('disabled', true);
        fixture.detectChanges();
        label.click();
        fixture.detectChanges();
        expect(input.matches(':disabled')).toBe(true);
        expect(component.checked()).toBe(false);
        expect(changed.mock.calls).toEqual([[true], [false]]);
    });

    it('focuses the native checkbox for Signal Forms and marks it touched on blur', () => {
        const touched = vi.fn();
        component.touch.subscribe(touched);
        component.focus();
        const input = requireElement('input');
        expect(document.activeElement).toBe(input);
        expect(component.touched()).toBe(false);

        input.blur();
        expect(component.touched()).toBe(true);
        expect(touched).toHaveBeenCalledOnce();
    });

    it('should set disabled state from input', () => {
        fixture.componentRef.setInput('disabled', true);
        fixture.detectChanges();

        expect(component['disabled']()).toBe(true);

        fixture.componentRef.setInput('disabled', false);
        fixture.detectChanges();

        expect(component['disabled']()).toBe(false);
    });
});

describe('FdUiCheckboxComponent validation messages', () => {
    let fixture: ComponentFixture<FdUiCheckboxComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [FdUiCheckboxComponent],
        }).compileComponents();

        fixture = TestBed.createComponent(FdUiCheckboxComponent);
    });

    it('should announce an error instead of the hint and restore the hint when cleared', () => {
        fixture.componentRef.setInput('hint', 'Please read carefully');
        fixture.componentRef.setInput('error', 'Accept the terms');
        fixture.detectChanges();

        const host = fixture.nativeElement as HTMLElement;
        const input = host.querySelector('input');
        const error = host.querySelector('.fd-ui-checkbox__error');
        expect(error?.textContent.trim()).toBe('Accept the terms');
        expect(error?.getAttribute('role')).toBe('alert');
        expect(input?.getAttribute('aria-describedby')).toBe(error?.id);
        expect(input?.getAttribute('aria-invalid')).toBe('true');
        expect(host.querySelector('.fd-ui-checkbox__hint')).toBeNull();

        fixture.componentRef.setInput('error', null);
        fixture.detectChanges();

        expect(host.querySelector('.fd-ui-checkbox__error')).toBeNull();
        expect(input?.getAttribute('aria-invalid')).toBeNull();
        expect(input?.getAttribute('aria-describedby')).toBe(host.querySelector('.fd-ui-checkbox__hint')?.id);
    });

    it('should not reserve an error or description when the message is empty', () => {
        fixture.componentRef.setInput('error', '');
        fixture.detectChanges();

        const host = fixture.nativeElement as HTMLElement;
        expect(host.querySelector('.fd-ui-checkbox__error')).toBeNull();
        expect(host.querySelector('input')?.getAttribute('aria-invalid')).toBeNull();
        expect(host.querySelector('input')?.getAttribute('aria-describedby')).toBeNull();
    });
});

describe('FdUiCheckboxComponent with Signal Forms', () => {
    it('updates the native state when a record is edited and the form is reset', async () => {
        await TestBed.configureTestingModule({ imports: [CheckboxFormHost] }).compileComponents();
        const fixture = TestBed.createComponent(CheckboxFormHost);
        fixture.detectChanges();
        const input = (fixture.nativeElement as HTMLElement).querySelector<HTMLInputElement>('input');
        expect(input?.checked).toBe(true);

        fixture.componentInstance.value.set({ enabled: false });
        fixture.detectChanges();
        expect(input?.checked).toBe(false);

        fixture.componentInstance.value.set({ enabled: true });
        fixture.detectChanges();
        expect(input?.checked).toBe(true);
    });
});
