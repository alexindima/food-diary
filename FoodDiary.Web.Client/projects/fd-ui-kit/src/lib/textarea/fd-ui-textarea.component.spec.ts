import { Component } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { describe, expect, it, vi } from 'vitest';

import { FdUiTextareaComponent } from './fd-ui-textarea.component';

const NUMERIC_VALUE = 42;
const TEXTAREA_ROWS = 8;

@Component({
    template: '<fd-ui-textarea [formControl]="ctrl" />',
    standalone: true,
    imports: [FdUiTextareaComponent, ReactiveFormsModule],
})
class TestHostComponent {
    public ctrl = new FormControl('');
}

type TextareaTestContext = {
    component: FdUiTextareaComponent;
    el: HTMLElement;
    fixture: ComponentFixture<FdUiTextareaComponent>;
    textarea: () => HTMLTextAreaElement;
};
type TextareaHostTestContext = {
    hostComponent: TestHostComponent;
    hostFixture: ComponentFixture<TestHostComponent>;
    textarea: () => HTMLTextAreaElement;
};

async function setupTextareaAsync(): Promise<TextareaTestContext> {
    await TestBed.configureTestingModule({
        imports: [FdUiTextareaComponent],
    }).compileComponents();

    const fixture = TestBed.createComponent(FdUiTextareaComponent);
    const component = fixture.componentInstance;
    const el = fixture.nativeElement as HTMLElement;
    const textarea = (): HTMLTextAreaElement => requireTextarea(el);
    fixture.detectChanges();

    return { component, el, fixture, textarea };
}

async function setupTextareaHostAsync(): Promise<TextareaHostTestContext> {
    await TestBed.configureTestingModule({
        imports: [TestHostComponent],
    }).compileComponents();

    const hostFixture = TestBed.createComponent(TestHostComponent);
    const hostComponent = hostFixture.componentInstance;
    const textarea = (): HTMLTextAreaElement => requireTextarea(hostFixture.nativeElement as HTMLElement);
    hostFixture.detectChanges();

    return { hostComponent, hostFixture, textarea };
}

function requireTextarea(host: HTMLElement): HTMLTextAreaElement {
    const textarea = host.querySelector<HTMLTextAreaElement>('.fd-ui-textarea__control');
    if (textarea === null) {
        throw new Error('Expected fd-ui textarea control to exist.');
    }

    return textarea;
}

describe('FdUiTextareaComponent', () => {
    it('should create', async () => {
        const { component } = await setupTextareaAsync();

        expect(component).toBeTruthy();
    });
});

describe('FdUiTextareaComponent rendering', () => {
    it('should render label when provided', async () => {
        const { el, fixture } = await setupTextareaAsync();
        fixture.componentRef.setInput('label', 'Description');
        fixture.detectChanges();

        const label = el.querySelector('.fd-ui-textarea__label-text');
        expect(label).toBeTruthy();
        expect(label?.textContent).toBe('Description');
    });

    it('should show required asterisk', async () => {
        const { el, fixture } = await setupTextareaAsync();
        fixture.componentRef.setInput('label', 'Notes');
        fixture.componentRef.setInput('required', true);
        fixture.detectChanges();

        const asterisk = el.querySelector('.fd-ui-textarea__required');
        expect(asterisk).toBeTruthy();
        expect(asterisk?.textContent).toBe('*');
    });

    it('should display error message', async () => {
        const { el, fixture } = await setupTextareaAsync();
        fixture.componentRef.setInput('error', 'Too short');
        fixture.detectChanges();

        const errorEl = el.querySelector('.fd-ui-textarea__error');
        expect(errorEl).toBeTruthy();
        expect(errorEl?.textContent).toBe('Too short');
    });
});

describe('FdUiTextareaComponent CVA', () => {
    it('should write value via CVA (string)', async () => {
        const { component, fixture, textarea } = await setupTextareaAsync();
        component.writeValue('hello');
        fixture.detectChanges();

        expect(textarea().value).toBe('hello');
    });

    it('should write value via CVA (null converts to empty string)', async () => {
        const { component, fixture, textarea } = await setupTextareaAsync();
        component.writeValue(null);
        fixture.detectChanges();

        expect(textarea().value).toBe('');
    });

    it('should write value via CVA (number converts to string)', async () => {
        const { component, fixture, textarea } = await setupTextareaAsync();
        component.writeValue(NUMERIC_VALUE);
        fixture.detectChanges();

        expect(textarea().value).toBe(String(NUMERIC_VALUE));
    });

    it('should call onChange on input', async () => {
        const { component, textarea } = await setupTextareaAsync();
        const onChangeSpy = vi.fn();
        component.registerOnChange(onChangeSpy);

        const textareaEl = textarea();
        textareaEl.value = 'new text';
        textareaEl.dispatchEvent(new Event('input'));

        expect(onChangeSpy).toHaveBeenCalledWith('new text');
    });

    it('should call onTouched on blur', async () => {
        const { component, textarea } = await setupTextareaAsync();
        const onTouchedSpy = vi.fn();
        component.registerOnTouched(onTouchedSpy);

        textarea().dispatchEvent(new Event('blur'));

        expect(onTouchedSpy).toHaveBeenCalled();
    });

    it('should set disabled state', async () => {
        const { component, fixture, textarea } = await setupTextareaAsync();
        component.setDisabledState(true);
        fixture.detectChanges();

        expect(textarea().disabled).toBe(true);
    });

    it('should not process input when disabled', async () => {
        const { component, textarea } = await setupTextareaAsync();
        const onChangeSpy = vi.fn();
        component.registerOnChange(onChangeSpy);
        component.setDisabledState(true);

        const textareaEl = textarea();
        textareaEl.value = 'blocked';
        textareaEl.dispatchEvent(new Event('input'));

        expect(onChangeSpy).not.toHaveBeenCalled();
    });
});

describe('FdUiTextareaComponent floating label', () => {
    it('should float label when focused', async () => {
        const { el, fixture, textarea } = await setupTextareaAsync();
        fixture.componentRef.setInput('label', 'Bio');
        fixture.detectChanges();

        textarea().dispatchEvent(new Event('focus'));
        fixture.detectChanges();

        const wrapper = el.querySelector('.fd-ui-textarea');
        expect(wrapper?.classList).toContain('fd-ui-textarea--floating');
    });

    it('should float label when has value', async () => {
        const { component, el, fixture } = await setupTextareaAsync();
        fixture.componentRef.setInput('label', 'Bio');
        component.writeValue('content');
        fixture.detectChanges();

        const wrapper = el.querySelector('.fd-ui-textarea');
        expect(wrapper?.classList).toContain('fd-ui-textarea--floating');
    });
});

describe('FdUiTextareaComponent attributes', () => {
    it('should set rows attribute', async () => {
        const { fixture, textarea } = await setupTextareaAsync();
        fixture.componentRef.setInput('rows', TEXTAREA_ROWS);
        fixture.detectChanges();

        expect(textarea().getAttribute('rows')).toBe(String(TEXTAREA_ROWS));
    });

    it('should apply size class', async () => {
        const { el, fixture } = await setupTextareaAsync();
        fixture.componentRef.setInput('size', 'sm');
        fixture.detectChanges();

        const wrapper = el.querySelector('.fd-ui-textarea');
        expect(wrapper?.classList).toContain('fd-ui-textarea--size-sm');
    });
});

describe('FdUiTextareaComponent with TestHost', () => {
    it('should write value from FormControl', async () => {
        const { hostComponent, hostFixture, textarea } = await setupTextareaHostAsync();
        hostComponent.ctrl.setValue('form value');
        hostFixture.detectChanges();

        expect(textarea().value).toBe('form value');
    });

    it('should propagate input value to FormControl', async () => {
        const { hostComponent, hostFixture, textarea } = await setupTextareaHostAsync();
        const textareaEl = textarea();
        textareaEl.value = 'typed';
        textareaEl.dispatchEvent(new Event('input'));
        hostFixture.detectChanges();

        expect(hostComponent.ctrl.value).toBe('typed');
    });

    it('should mark control as touched on blur', async () => {
        const { hostComponent, hostFixture, textarea } = await setupTextareaHostAsync();
        expect(hostComponent.ctrl.touched).toBe(false);

        textarea().dispatchEvent(new Event('blur'));
        hostFixture.detectChanges();

        expect(hostComponent.ctrl.touched).toBe(true);
    });

    it('should disable textarea when FormControl is disabled', async () => {
        const { hostComponent, hostFixture, textarea } = await setupTextareaHostAsync();
        hostComponent.ctrl.disable();
        hostFixture.detectChanges();

        expect(textarea().disabled).toBe(true);
    });
});
