import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Component } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { FdUiInputComponent } from './fd-ui-input.component';

@Component({
    template: '<fd-ui-input [formControl]="ctrl" />',
    standalone: true,
    imports: [FdUiInputComponent, ReactiveFormsModule],
})
class TestHostComponent {
    public ctrl = new FormControl('');
}

describe('FdUiInputComponent', () => {
    let fixture: ComponentFixture<FdUiInputComponent>;
    let component: FdUiInputComponent;
    let el: HTMLElement;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [FdUiInputComponent],
        }).compileComponents();

        fixture = TestBed.createComponent(FdUiInputComponent);
        component = fixture.componentInstance;
        el = fixture.nativeElement;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should render label when provided', () => {
        fixture.componentRef.setInput('label', 'Username');
        fixture.detectChanges();

        const label = el.querySelector('.fd-ui-input__label-text');
        expect(label).toBeTruthy();
        expect(label!.textContent).toBe('Username');
    });

    it('should show required asterisk when required', () => {
        fixture.componentRef.setInput('label', 'Email');
        fixture.componentRef.setInput('required', true);
        fixture.detectChanges();

        const asterisk = el.querySelector('.fd-ui-input__required');
        expect(asterisk).toBeTruthy();
        expect(asterisk!.textContent).toBe('*');
    });

    it('should render prefix icon', () => {
        fixture.componentRef.setInput('prefixIcon', 'search');
        fixture.detectChanges();

        const prefix = el.querySelector('.fd-ui-input__prefix');
        expect(prefix).toBeTruthy();
    });

    it('should render suffix button with icon', () => {
        fixture.componentRef.setInput('suffixButtonIcon', 'visibility');
        fixture.detectChanges();

        const suffix = el.querySelector('.fd-ui-input__suffix');
        expect(suffix).toBeTruthy();
    });

    it('should emit suffixButtonClicked when suffix button clicked', () => {
        fixture.componentRef.setInput('suffixButtonIcon', 'visibility');
        fixture.detectChanges();

        const spy = vi.spyOn(component.suffixButtonClicked, 'emit');
        const button = el.querySelector<HTMLButtonElement>('.fd-ui-input__suffix')!;
        button.click();

        expect(spy).toHaveBeenCalled();
    });

    it('should not emit suffixButtonClicked when disabled', () => {
        fixture.componentRef.setInput('suffixButtonIcon', 'visibility');
        component.setDisabledState(true);
        fixture.detectChanges();

        const spy = vi.spyOn(component.suffixButtonClicked, 'emit');
        const button = el.querySelector<HTMLButtonElement>('.fd-ui-input__suffix')!;
        button.click();

        expect(spy).not.toHaveBeenCalled();
    });

    it('should write value via CVA', () => {
        component.writeValue('hello');
        fixture.detectChanges();

        const input = el.querySelector<HTMLInputElement>('.fd-ui-input__control')!;
        expect(input.value).toBe('hello');
    });

    it('should call onChange on input event', () => {
        const onChangeSpy = vi.fn();
        component.registerOnChange(onChangeSpy);

        const input = el.querySelector<HTMLInputElement>('.fd-ui-input__control')!;
        input.value = 'test';
        input.dispatchEvent(new Event('input'));

        expect(onChangeSpy).toHaveBeenCalledWith('test');
    });

    it('should call onTouched on blur', () => {
        const onTouchedSpy = vi.fn();
        component.registerOnTouched(onTouchedSpy);

        const input = el.querySelector<HTMLInputElement>('.fd-ui-input__control')!;
        input.dispatchEvent(new Event('blur'));

        expect(onTouchedSpy).toHaveBeenCalled();
    });

    it('should set disabled state via CVA', () => {
        component.setDisabledState(true);
        fixture.detectChanges();

        const input = el.querySelector<HTMLInputElement>('.fd-ui-input__control')!;
        expect(input.disabled).toBe(true);
    });

    it('should not process input when disabled', () => {
        const onChangeSpy = vi.fn();
        component.registerOnChange(onChangeSpy);
        component.setDisabledState(true);

        const input = el.querySelector<HTMLInputElement>('.fd-ui-input__control')!;
        input.value = 'blocked';
        input.dispatchEvent(new Event('input'));

        expect(onChangeSpy).not.toHaveBeenCalled();
    });

    it('should float label when focused', () => {
        fixture.componentRef.setInput('label', 'Name');
        fixture.detectChanges();

        const input = el.querySelector<HTMLInputElement>('.fd-ui-input__control')!;
        input.dispatchEvent(new Event('focus'));
        fixture.detectChanges();

        const wrapper = el.querySelector('.fd-ui-input');
        expect(wrapper!.classList).toContain('fd-ui-input--floating');
    });

    it('should float label when has value', () => {
        fixture.componentRef.setInput('label', 'Name');
        component.writeValue('something');
        fixture.detectChanges();

        const wrapper = el.querySelector('.fd-ui-input');
        expect(wrapper!.classList).toContain('fd-ui-input--floating');
    });

    it('should show placeholder only when focused and empty', () => {
        fixture.componentRef.setInput('placeholder', 'Enter text');
        fixture.detectChanges();

        const input = el.querySelector<HTMLInputElement>('.fd-ui-input__control')!;

        // Not focused and empty: no placeholder
        expect(input.getAttribute('placeholder')).toBeNull();

        // Focused and empty: show placeholder
        input.dispatchEvent(new Event('focus'));
        fixture.detectChanges();
        expect(input.getAttribute('placeholder')).toBe('Enter text');

        // Focused with value: no placeholder
        component.writeValue('x');
        fixture.detectChanges();
        expect(input.getAttribute('placeholder')).toBeNull();
    });

    it('should display error message', () => {
        fixture.componentRef.setInput('error', 'Field is required');
        fixture.detectChanges();

        const errorEl = el.querySelector('.fd-ui-input__error');
        expect(errorEl).toBeTruthy();
        expect(errorEl!.textContent).toBe('Field is required');
    });

    it('should apply size class', () => {
        fixture.componentRef.setInput('size', 'lg');
        fixture.detectChanges();

        const wrapper = el.querySelector('.fd-ui-input');
        expect(wrapper!.classList).toContain('fd-ui-input--size-lg');
    });

    it('should set type attribute on input element', () => {
        fixture.componentRef.setInput('type', 'password');
        fixture.detectChanges();

        const input = el.querySelector<HTMLInputElement>('.fd-ui-input__control')!;
        expect(input.getAttribute('type')).toBe('password');
    });

    describe('with TestHost (FormControl integration)', () => {
        let hostFixture: ComponentFixture<TestHostComponent>;
        let hostComponent: TestHostComponent;

        beforeEach(async () => {
            hostFixture = TestBed.createComponent(TestHostComponent);
            hostComponent = hostFixture.componentInstance;
            hostFixture.detectChanges();
        });

        it('should write value from FormControl', () => {
            hostComponent.ctrl.setValue('from form');
            hostFixture.detectChanges();

            const input = (hostFixture.nativeElement as HTMLElement).querySelector<HTMLInputElement>('.fd-ui-input__control')!;
            expect(input.value).toBe('from form');
        });

        it('should propagate input value to FormControl', () => {
            const input = (hostFixture.nativeElement as HTMLElement).querySelector<HTMLInputElement>('.fd-ui-input__control')!;
            input.value = 'typed';
            input.dispatchEvent(new Event('input'));
            hostFixture.detectChanges();

            expect(hostComponent.ctrl.value).toBe('typed');
        });

        it('should mark control as touched on blur', () => {
            expect(hostComponent.ctrl.touched).toBe(false);

            const input = (hostFixture.nativeElement as HTMLElement).querySelector<HTMLInputElement>('.fd-ui-input__control')!;
            input.dispatchEvent(new Event('blur'));
            hostFixture.detectChanges();

            expect(hostComponent.ctrl.touched).toBe(true);
        });

        it('should disable input when FormControl is disabled', () => {
            hostComponent.ctrl.disable();
            hostFixture.detectChanges();

            const input = (hostFixture.nativeElement as HTMLElement).querySelector<HTMLInputElement>('.fd-ui-input__control')!;
            expect(input.disabled).toBe(true);
        });
    });
});
