import { Component } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { describe, expect, it, vi } from 'vitest';

import { FdUiInputComponent } from './fd-ui-input.component';

@Component({
    template: '<fd-ui-input [formControl]="ctrl" />',
    standalone: true,
    imports: [FdUiInputComponent, ReactiveFormsModule],
})
class TestHostComponent {
    public ctrl = new FormControl('');
}

type InputTestContext = {
    component: FdUiInputComponent;
    el: HTMLElement;
    fixture: ComponentFixture<FdUiInputComponent>;
    input: () => HTMLInputElement;
};
type InputHostTestContext = {
    hostComponent: TestHostComponent;
    hostFixture: ComponentFixture<TestHostComponent>;
    input: () => HTMLInputElement;
};

async function setupInputAsync(): Promise<InputTestContext> {
    await TestBed.configureTestingModule({
        imports: [FdUiInputComponent],
    }).compileComponents();

    const fixture = TestBed.createComponent(FdUiInputComponent);
    const component = fixture.componentInstance;
    const el = fixture.nativeElement as HTMLElement;
    const input = (): HTMLInputElement => requireInput(el);
    fixture.detectChanges();

    return { component, el, fixture, input };
}

async function setupInputHostAsync(): Promise<InputHostTestContext> {
    await TestBed.configureTestingModule({
        imports: [TestHostComponent],
    }).compileComponents();

    const hostFixture = TestBed.createComponent(TestHostComponent);
    const hostComponent = hostFixture.componentInstance;
    const input = (): HTMLInputElement => requireInput(hostFixture.nativeElement as HTMLElement);
    hostFixture.detectChanges();

    return { hostComponent, hostFixture, input };
}

function requireInput(host: HTMLElement): HTMLInputElement {
    const input = host.querySelector<HTMLInputElement>('.fd-ui-input__control');
    if (input === null) {
        throw new Error('Expected fd-ui input control to exist.');
    }

    return input;
}

describe('FdUiInputComponent', () => {
    it('should create', async () => {
        const { component } = await setupInputAsync();

        expect(component).toBeTruthy();
    });
});

describe('FdUiInputComponent rendering', () => {
    it('should render label when provided', async () => {
        const { el, fixture } = await setupInputAsync();
        fixture.componentRef.setInput('label', 'Username');
        fixture.detectChanges();

        const label = el.querySelector('.fd-ui-input__label-text');
        expect(label).toBeTruthy();
        expect(label?.textContent).toBe('Username');
    });

    it('should show required asterisk when required', async () => {
        const { el, fixture } = await setupInputAsync();
        fixture.componentRef.setInput('label', 'Email');
        fixture.componentRef.setInput('required', true);
        fixture.detectChanges();

        const asterisk = el.querySelector('.fd-ui-input__required');
        expect(asterisk).toBeTruthy();
        expect(asterisk?.textContent).toBe('*');
    });

    it('should render prefix icon', async () => {
        const { el, fixture } = await setupInputAsync();
        fixture.componentRef.setInput('prefixIcon', 'search');
        fixture.detectChanges();

        const prefix = el.querySelector('.fd-ui-input__prefix');
        expect(prefix).toBeTruthy();
    });

    it('should display error message', async () => {
        const { el, fixture } = await setupInputAsync();
        fixture.componentRef.setInput('error', 'Field is required');
        fixture.detectChanges();

        const wrapper = el.querySelector('.fd-ui-input');
        const errorEl = el.querySelector('.fd-ui-input__error');
        expect(wrapper?.classList).toContain('fd-ui-input--has-error');
        expect(errorEl).toBeTruthy();
        expect(errorEl?.textContent).toBe('Field is required');
    });

    it('should not apply error state when error is omitted', async () => {
        const { el } = await setupInputAsync();

        const wrapper = el.querySelector('.fd-ui-input');
        const errorEl = el.querySelector('.fd-ui-input__error');
        expect(wrapper?.classList).not.toContain('fd-ui-input--has-error');
        expect(errorEl).toBeNull();
    });

    it('should not apply error state for blank error text', async () => {
        const { el, fixture } = await setupInputAsync();
        fixture.componentRef.setInput('error', '   ');
        fixture.detectChanges();

        const wrapper = el.querySelector('.fd-ui-input');
        const errorEl = el.querySelector('.fd-ui-input__error');
        expect(wrapper?.classList).not.toContain('fd-ui-input--has-error');
        expect(errorEl).toBeNull();
    });
});

describe('FdUiInputComponent suffix button', () => {
    it('should render suffix button with icon', async () => {
        const { el, fixture } = await setupInputAsync();
        fixture.componentRef.setInput('suffixButtonIcon', 'visibility');
        fixture.detectChanges();

        const suffix = el.querySelector('.fd-ui-input__suffix');
        expect(suffix).toBeTruthy();
    });

    it('should emit suffixButtonClicked when suffix button clicked', async () => {
        const { component, el, fixture } = await setupInputAsync();
        fixture.componentRef.setInput('suffixButtonIcon', 'visibility');
        fixture.detectChanges();

        const spy = vi.spyOn(component.suffixButtonClicked, 'emit');
        const button = el.querySelector<HTMLButtonElement>('.fd-ui-input__suffix');
        expect(button).toBeTruthy();
        button?.click();

        expect(spy).toHaveBeenCalled();
    });

    it('should not emit suffixButtonClicked when disabled', async () => {
        const { component, el, fixture } = await setupInputAsync();
        fixture.componentRef.setInput('suffixButtonIcon', 'visibility');
        component.setDisabledState(true);
        fixture.detectChanges();

        const spy = vi.spyOn(component.suffixButtonClicked, 'emit');
        const button = el.querySelector<HTMLButtonElement>('.fd-ui-input__suffix');
        expect(button).toBeTruthy();
        button?.click();

        expect(spy).not.toHaveBeenCalled();
    });
});

describe('FdUiInputComponent CVA', () => {
    it('should write value via CVA', async () => {
        const { component, fixture, input } = await setupInputAsync();
        component.writeValue('hello');
        fixture.detectChanges();

        expect(input().value).toBe('hello');
    });

    it('should call onChange on input event', async () => {
        const { component, input } = await setupInputAsync();
        const onChangeSpy = vi.fn();
        component.registerOnChange(onChangeSpy);

        const inputEl = input();
        inputEl.value = 'test';
        inputEl.dispatchEvent(new Event('input'));

        expect(onChangeSpy).toHaveBeenCalledWith('test');
    });

    it('should call onTouched on blur', async () => {
        const { component, input } = await setupInputAsync();
        const onTouchedSpy = vi.fn();
        component.registerOnTouched(onTouchedSpy);

        input().dispatchEvent(new Event('blur'));

        expect(onTouchedSpy).toHaveBeenCalled();
    });

    it('should set disabled state via CVA', async () => {
        const { component, fixture, input } = await setupInputAsync();
        component.setDisabledState(true);
        fixture.detectChanges();

        expect(input().disabled).toBe(true);
    });

    it('should not process input when disabled', async () => {
        const { component, input } = await setupInputAsync();
        const onChangeSpy = vi.fn();
        component.registerOnChange(onChangeSpy);
        component.setDisabledState(true);

        const inputEl = input();
        inputEl.value = 'blocked';
        inputEl.dispatchEvent(new Event('input'));

        expect(onChangeSpy).not.toHaveBeenCalled();
    });
});

describe('FdUiInputComponent floating label', () => {
    it('should float label when focused', async () => {
        const { el, fixture, input } = await setupInputAsync();
        fixture.componentRef.setInput('label', 'Name');
        fixture.detectChanges();

        input().dispatchEvent(new Event('focus'));
        fixture.detectChanges();

        const wrapper = el.querySelector('.fd-ui-input');
        expect(wrapper?.classList).toContain('fd-ui-input--floating');
    });

    it('should float label when has value', async () => {
        const { component, el, fixture } = await setupInputAsync();
        fixture.componentRef.setInput('label', 'Name');
        component.writeValue('something');
        fixture.detectChanges();

        const wrapper = el.querySelector('.fd-ui-input');
        expect(wrapper?.classList).toContain('fd-ui-input--floating');
    });

    it('should float label when native input has autofilled value before focus', async () => {
        const { el, fixture, input } = await setupInputAsync();
        fixture.componentRef.setInput('label', 'Email');
        fixture.detectChanges();

        const inputEl = input();
        inputEl.value = 'autofilled@example.com';
        inputEl.dispatchEvent(new Event('focus'));
        fixture.detectChanges();

        const wrapper = el.querySelector('.fd-ui-input');
        expect(wrapper?.classList).toContain('fd-ui-input--floating');
    });
});

describe('FdUiInputComponent attributes', () => {
    it('should show placeholder only when focused and empty', async () => {
        const { component, fixture, input } = await setupInputAsync();
        fixture.componentRef.setInput('placeholder', 'Enter text');
        fixture.detectChanges();

        const inputEl = input();
        expect(inputEl.getAttribute('placeholder')).toBeNull();

        inputEl.dispatchEvent(new Event('focus'));
        fixture.detectChanges();
        expect(inputEl.getAttribute('placeholder')).toBe('Enter text');

        component.writeValue('x');
        fixture.detectChanges();
        expect(inputEl.getAttribute('placeholder')).toBeNull();
    });

    it('should apply size class', async () => {
        const { el, fixture } = await setupInputAsync();
        fixture.componentRef.setInput('size', 'lg');
        fixture.detectChanges();

        const wrapper = el.querySelector('.fd-ui-input');
        expect(wrapper?.classList).toContain('fd-ui-input--size-lg');
    });

    it('should set type attribute on input element', async () => {
        const { fixture, input } = await setupInputAsync();
        fixture.componentRef.setInput('type', 'password');
        fixture.detectChanges();

        expect(input().getAttribute('type')).toBe('password');
    });
});

describe('FdUiInputComponent with TestHost', () => {
    it('should write value from FormControl', async () => {
        const { hostComponent, hostFixture, input } = await setupInputHostAsync();
        hostComponent.ctrl.setValue('from form');
        hostFixture.detectChanges();

        expect(input().value).toBe('from form');
    });

    it('should propagate input value to FormControl', async () => {
        const { hostComponent, hostFixture, input } = await setupInputHostAsync();
        const inputEl = input();
        inputEl.value = 'typed';
        inputEl.dispatchEvent(new Event('input'));
        hostFixture.detectChanges();

        expect(hostComponent.ctrl.value).toBe('typed');
    });

    it('should sync native autofilled value to FormControl on focus', async () => {
        const { hostComponent, hostFixture, input } = await setupInputHostAsync();
        const inputEl = input();
        inputEl.value = 'autofilled@example.com';
        inputEl.dispatchEvent(new Event('focus'));
        hostFixture.detectChanges();

        expect(hostComponent.ctrl.value).toBe('autofilled@example.com');
    });

    it('should mark control as touched on blur', async () => {
        const { hostComponent, hostFixture, input } = await setupInputHostAsync();
        expect(hostComponent.ctrl.touched).toBe(false);

        input().dispatchEvent(new Event('blur'));
        hostFixture.detectChanges();

        expect(hostComponent.ctrl.touched).toBe(true);
    });

    it('should disable input when FormControl is disabled', async () => {
        const { hostComponent, hostFixture, input } = await setupInputHostAsync();
        hostComponent.ctrl.disable();
        hostFixture.detectChanges();

        expect(input().disabled).toBe(true);
    });
});
