import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { form, FormField, required } from '@angular/forms/signals';
import { firstValueFrom, of } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../src/testing/translate-testing.module';
import { FdUiCheckboxComponent } from '../checkbox/fd-ui-checkbox';
import { FdUiInputComponent } from '../input/fd-ui-input';
import { FdUiSelectComponent } from '../select/fd-ui-select';
import { FdUiTextareaComponent } from '../textarea/fd-ui-textarea';
import { FdUiFormDirective } from './fd-ui-form.directive';

const REQUIRED_CONTROL_COUNT = 4;

@Component({
    imports: [FdUiFormDirective, FormField, FdUiInputComponent, FdUiTextareaComponent, FdUiSelectComponent, FdUiCheckboxComponent],
    template: `
        <form [formRoot]="fields">
            <fd-ui-input label="Name" [formField]="fields.name" />
            <fd-ui-textarea label="Notes" [formField]="fields.notes" />
            <fd-ui-select label="Choice" [options]="options" [formField]="fields.choice" />
            <fd-ui-checkbox label="Agree" [formField]="fields.agree" />
            <button type="submit">Save</button>
        </form>
    `,
})
class FormExample {
    public readonly action = vi.fn();
    public readonly model = signal({ name: '', notes: '', choice: null as string | null, agree: false });
    public readonly options = [{ value: 'a', label: 'A' }];
    private readonly submitAsync = async (): Promise<void> => {
        this.action();
        await firstValueFrom(of(undefined));
    };
    public readonly fields = form(
        this.model,
        path => {
            required(path.name);
            required(path.notes);
            required(path.choice);
            required(path.agree);
        },
        {
            submission: {
                action: this.submitAsync,
            },
        },
    );
}

describe('UI-kit form validation', () => {
    it('reveals framework errors on submit and focuses the first invalid input without running the action', async () => {
        TestBed.configureTestingModule({ providers: [provideTranslateTesting()] });
        const fixture = TestBed.createComponent(FormExample);
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;
        expect(host.querySelectorAll('[role=alert]')).toHaveLength(0);
        host.querySelector<HTMLButtonElement>('button[type=submit]')?.click();
        await fixture.whenStable();
        expect(host.querySelectorAll('[role=alert]')).toHaveLength(REQUIRED_CONTROL_COUNT);
        const input = host.querySelector('input');
        expect(input?.getAttribute('aria-invalid')).toBe('true');
        expect(input?.getAttribute('aria-describedby')).toContain('-error');
        expect(document.activeElement).toBe(input);
        expect(fixture.componentInstance.action).not.toHaveBeenCalled();
    });

    it('supports focusing each custom control and removes errors when corrected', async () => {
        TestBed.configureTestingModule({ providers: [provideTranslateTesting()] });
        const fixture = TestBed.createComponent(FormExample);
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;
        const component = fixture.componentInstance;
        const attemptAsync = async (): Promise<void> => {
            fixture.detectChanges();
            host.querySelector<HTMLButtonElement>('button[type=submit]')?.click();
            await fixture.whenStable();
        };
        component.model.update(value => ({ ...value, name: 'Valid' }));
        await attemptAsync();
        expect(document.activeElement).toBe(host.querySelector('textarea'));
        component.model.update(value => ({ ...value, notes: 'Valid' }));
        await attemptAsync();
        expect(document.activeElement).toBe(host.querySelector('fd-ui-select button'));
        component.model.update(value => ({ ...value, choice: 'a' }));
        await attemptAsync();
        expect(document.activeElement).toBe(host.querySelector('input[type=checkbox]'));
        component.model.update(value => ({ ...value, agree: true }));
        await attemptAsync();
        expect(host.querySelectorAll('[role=alert]')).toHaveLength(0);
        expect(component.action).toHaveBeenCalledOnce();
    });
});
