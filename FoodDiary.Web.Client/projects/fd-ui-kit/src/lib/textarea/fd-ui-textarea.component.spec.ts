import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Component } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { FdUiTextareaComponent } from './fd-ui-textarea.component';

@Component({
    template: '<fd-ui-textarea [formControl]="ctrl" />',
    standalone: true,
    imports: [FdUiTextareaComponent, ReactiveFormsModule],
})
class TestHostComponent {
    public ctrl = new FormControl('');
}

describe('FdUiTextareaComponent', () => {
    let fixture: ComponentFixture<FdUiTextareaComponent>;
    let component: FdUiTextareaComponent;
    let el: HTMLElement;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [FdUiTextareaComponent],
        }).compileComponents();

        fixture = TestBed.createComponent(FdUiTextareaComponent);
        component = fixture.componentInstance;
        el = fixture.nativeElement;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should render label when provided', () => {
        fixture.componentRef.setInput('label', 'Description');
        fixture.detectChanges();

        const label = el.querySelector('.fd-ui-textarea__label-text');
        expect(label).toBeTruthy();
        expect(label!.textContent).toBe('Description');
    });

    it('should show required asterisk', () => {
        fixture.componentRef.setInput('label', 'Notes');
        fixture.componentRef.setInput('required', true);
        fixture.detectChanges();

        const asterisk = el.querySelector('.fd-ui-textarea__required');
        expect(asterisk).toBeTruthy();
        expect(asterisk!.textContent).toBe('*');
    });

    it('should write value via CVA (string)', () => {
        component.writeValue('hello');
        fixture.detectChanges();

        const textarea = el.querySelector<HTMLTextAreaElement>('.fd-ui-textarea__control')!;
        expect(textarea.value).toBe('hello');
    });

    it('should write value via CVA (null converts to empty string)', () => {
        component.writeValue(null);
        fixture.detectChanges();

        const textarea = el.querySelector<HTMLTextAreaElement>('.fd-ui-textarea__control')!;
        expect(textarea.value).toBe('');
    });

    it('should write value via CVA (number converts to string)', () => {
        component.writeValue(42);
        fixture.detectChanges();

        const textarea = el.querySelector<HTMLTextAreaElement>('.fd-ui-textarea__control')!;
        expect(textarea.value).toBe('42');
    });

    it('should call onChange on input', () => {
        const onChangeSpy = vi.fn();
        component.registerOnChange(onChangeSpy);

        const textarea = el.querySelector<HTMLTextAreaElement>('.fd-ui-textarea__control')!;
        textarea.value = 'new text';
        textarea.dispatchEvent(new Event('input'));

        expect(onChangeSpy).toHaveBeenCalledWith('new text');
    });

    it('should call onTouched on blur', () => {
        const onTouchedSpy = vi.fn();
        component.registerOnTouched(onTouchedSpy);

        const textarea = el.querySelector<HTMLTextAreaElement>('.fd-ui-textarea__control')!;
        textarea.dispatchEvent(new Event('blur'));

        expect(onTouchedSpy).toHaveBeenCalled();
    });

    it('should set disabled state', () => {
        component.setDisabledState(true);
        fixture.detectChanges();

        const textarea = el.querySelector<HTMLTextAreaElement>('.fd-ui-textarea__control')!;
        expect(textarea.disabled).toBe(true);
    });

    it('should not process input when disabled', () => {
        const onChangeSpy = vi.fn();
        component.registerOnChange(onChangeSpy);
        component.setDisabledState(true);

        const textarea = el.querySelector<HTMLTextAreaElement>('.fd-ui-textarea__control')!;
        textarea.value = 'blocked';
        textarea.dispatchEvent(new Event('input'));

        expect(onChangeSpy).not.toHaveBeenCalled();
    });

    it('should float label when focused', () => {
        fixture.componentRef.setInput('label', 'Bio');
        fixture.detectChanges();

        const textarea = el.querySelector<HTMLTextAreaElement>('.fd-ui-textarea__control')!;
        textarea.dispatchEvent(new Event('focus'));
        fixture.detectChanges();

        const wrapper = el.querySelector('.fd-ui-textarea');
        expect(wrapper!.classList).toContain('fd-ui-textarea--floating');
    });

    it('should float label when has value', () => {
        fixture.componentRef.setInput('label', 'Bio');
        component.writeValue('content');
        fixture.detectChanges();

        const wrapper = el.querySelector('.fd-ui-textarea');
        expect(wrapper!.classList).toContain('fd-ui-textarea--floating');
    });

    it('should display error message', () => {
        fixture.componentRef.setInput('error', 'Too short');
        fixture.detectChanges();

        const errorEl = el.querySelector('.fd-ui-textarea__error');
        expect(errorEl).toBeTruthy();
        expect(errorEl!.textContent).toBe('Too short');
    });

    it('should set rows attribute', () => {
        fixture.componentRef.setInput('rows', 8);
        fixture.detectChanges();

        const textarea = el.querySelector<HTMLTextAreaElement>('.fd-ui-textarea__control')!;
        expect(textarea.getAttribute('rows')).toBe('8');
    });

    it('should apply size class', () => {
        fixture.componentRef.setInput('size', 'sm');
        fixture.detectChanges();

        const wrapper = el.querySelector('.fd-ui-textarea');
        expect(wrapper!.classList).toContain('fd-ui-textarea--size-sm');
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
            hostComponent.ctrl.setValue('form value');
            hostFixture.detectChanges();

            const textarea = (hostFixture.nativeElement as HTMLElement).querySelector<HTMLTextAreaElement>('.fd-ui-textarea__control')!;
            expect(textarea.value).toBe('form value');
        });

        it('should propagate input value to FormControl', () => {
            const textarea = (hostFixture.nativeElement as HTMLElement).querySelector<HTMLTextAreaElement>('.fd-ui-textarea__control')!;
            textarea.value = 'typed';
            textarea.dispatchEvent(new Event('input'));
            hostFixture.detectChanges();

            expect(hostComponent.ctrl.value).toBe('typed');
        });

        it('should mark control as touched on blur', () => {
            expect(hostComponent.ctrl.touched).toBe(false);

            const textarea = (hostFixture.nativeElement as HTMLElement).querySelector<HTMLTextAreaElement>('.fd-ui-textarea__control')!;
            textarea.dispatchEvent(new Event('blur'));
            hostFixture.detectChanges();

            expect(hostComponent.ctrl.touched).toBe(true);
        });

        it('should disable textarea when FormControl is disabled', () => {
            hostComponent.ctrl.disable();
            hostFixture.detectChanges();

            const textarea = (hostFixture.nativeElement as HTMLElement).querySelector<HTMLTextAreaElement>('.fd-ui-textarea__control')!;
            expect(textarea.disabled).toBe(true);
        });
    });
});
