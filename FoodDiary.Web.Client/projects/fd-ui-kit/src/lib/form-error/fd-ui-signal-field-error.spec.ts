import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { form, FormField, required } from '@angular/forms/signals';
import { TranslateService } from '@ngx-translate/core';
import { describe, expect, it } from 'vitest';

import { provideTranslateTesting } from '../../../../../src/testing/translate-testing.module';
import { FdUiInputComponent } from '../input/fd-ui-input';
import { FdUiTextareaComponent } from '../textarea/fd-ui-textarea';

@Component({
    imports: [FdUiInputComponent, FdUiTextareaComponent, FormField],
    template: `
        <fd-ui-input label="Title" [formField]="draftForm.title" />
        <fd-ui-textarea label="Notes" [formField]="draftForm.notes" />
    `,
})
class TestHostComponent {
    public readonly model = signal({ title: 'Draft title', notes: 'Draft notes' });
    public readonly draftForm = form(this.model, path => {
        required(path.title);
        required(path.notes);
    });
}

describe('Signal field error presentation after reset', () => {
    it.each(['input', 'textarea'])('uses the reset field state when %s blur and reset share a render', async selector => {
        await TestBed.configureTestingModule({
            imports: [TestHostComponent],
            providers: [provideTranslateTesting()],
        }).compileComponents();
        const fixture = TestBed.createComponent(TestHostComponent);
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;
        const control = host.querySelector<HTMLInputElement | HTMLTextAreaElement>(selector);
        if (control === null) {
            throw new Error('Expected the tested native form control');
        }

        control.dispatchEvent(new FocusEvent('blur'));
        fixture.componentInstance.draftForm().reset({ title: '', notes: '' });
        fixture.detectChanges();

        expect(fixture.componentInstance.draftForm().touched()).toBe(false);
        expect(host.querySelector('[role="alert"]'), 'Reset fields must not display stale interaction errors').toBeNull();
    });

    it('shows the default message and announces an invalid required field only after blur', async () => {
        await TestBed.configureTestingModule({ imports: [TestHostComponent], providers: [provideTranslateTesting()] }).compileComponents();
        const fixture = TestBed.createComponent(TestHostComponent);
        fixture.componentInstance.model.set({ title: '', notes: 'Draft notes' });
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;
        const input = host.querySelector('input');
        expect(host.querySelector('[role="alert"]')).toBeNull();

        input?.focus();
        input?.blur();
        fixture.detectChanges();
        const message = host.querySelector('[role="alert"]');
        expect(message?.textContent.trim()).toBe('FORM_ERRORS.REQUIRED');
        expect(input?.getAttribute('aria-invalid')).toBe('true');
        expect(input?.getAttribute('aria-describedby')).toBe(message?.id);
    });
});

describe('Signal field error messages in a rendered input', () => {
    it.each([
        { localeMessage: 'Title already used', expected: 'Title already used' },
        { localeMessage: { description: 'Title already used' }, expected: 'TITLE_UNAVAILABLE' },
    ])('displays $expected after touch and gives an explicit error precedence', async ({ localeMessage, expected }) => {
        await TestBed.configureTestingModule({ providers: [provideTranslateTesting()] }).compileComponents();
        const translate = TestBed.inject(TranslateService);
        translate.setTranslation('en', { TITLE_UNAVAILABLE: localeMessage });
        translate.use('en').subscribe();
        const fixture = TestBed.createComponent(FdUiInputComponent);
        fixture.componentRef.setInput('errors', [{ kind: 'server', message: 'TITLE_UNAVAILABLE' }]);
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;
        const input = host.querySelector('input');
        expect(host.querySelector('[role="alert"]')).toBeNull();

        input?.focus();
        input?.blur();
        fixture.detectChanges();
        expect(host.querySelector('[role="alert"]')?.textContent.trim()).toBe(expected);
        expect(input?.getAttribute('aria-invalid')).toBe('true');

        fixture.componentRef.setInput('error', 'File could not be saved');
        fixture.detectChanges();
        expect(host.querySelector('[role="alert"]')?.textContent.trim()).toBe('File could not be saved');
    });
});
