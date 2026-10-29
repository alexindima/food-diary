import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { form, FormField, required } from '@angular/forms/signals';
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
});
