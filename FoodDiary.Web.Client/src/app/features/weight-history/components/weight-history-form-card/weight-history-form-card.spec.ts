import { signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { form, required, validate } from '@angular/forms/signals';
import { describe, expect, it, vi } from 'vitest';

import { waitForAsyncTasksAsync } from '../../../../../testing/async-testing';
import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { WeightHistoryFormCardComponent } from './weight-history-form-card';

describe('WeightHistoryFormCardComponent', () => {
    it('explains invalid input and clears the error after correction', () => {
        const { fixture } = setupComponent(false);
        const model = signal({ date: '2026-05-15', weight: '0' });
        const fields = TestBed.runInInjectionContext(() =>
            form(model, path => {
                required(path.weight);
                validate(path.weight, ({ value }) => (value() === '0' ? { kind: 'weightRange' } : undefined));
            }),
        );
        fields.weight().markAsTouched();
        fixture.componentRef.setInput('form', fields);
        fixture.detectChanges();
        const root = fixture.nativeElement as HTMLElement;

        expect(root.querySelector('fd-ui-input input')?.getAttribute('aria-invalid')).toBe('true');
        expect(root.querySelector('[role="alert"]')?.textContent).toContain('WEIGHT_HISTORY.VALUE_RANGE');

        model.set({ date: '2026-05-15', weight: '68.5' });
        fixture.detectChanges();
        expect(root.querySelector('[role="alert"]')).toBeNull();
        expect(root.querySelector('fd-ui-input input')?.getAttribute('aria-invalid')).toBeNull();
    });

    it('explains required fields when an empty form is submitted', async () => {
        const submitWeightFormAsync = vi.fn(async (): Promise<void> => {});
        const { fixture } = setupComponent(false);
        const model = signal({ date: '', weight: '' });
        const fields = TestBed.runInInjectionContext(() =>
            form(
                model,
                path => {
                    required(path.date);
                    required(path.weight);
                },
                { submission: { action: submitWeightFormAsync } },
            ),
        );
        fixture.componentRef.setInput('form', fields);
        fixture.detectChanges();
        const root = fixture.nativeElement as HTMLElement;
        root.querySelector('form')?.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
        await fixture.whenStable();
        fixture.detectChanges();

        expect(submitWeightFormAsync).not.toHaveBeenCalled();
        expect(root.querySelectorAll('[role="alert"]')).toHaveLength(2);
        expect(getText(fixture)).toContain('FORM_ERRORS.REQUIRED');
    });

    it('renders add mode by default', () => {
        const { fixture } = setupComponent(false);

        expect(getText(fixture)).toContain('WEIGHT_HISTORY.ADD');
    });

    it('renders edit mode and emits cancel', () => {
        const { component, fixture } = setupComponent(true);
        const cancelHandler = vi.fn();
        component['editCancel'].subscribe(cancelHandler);

        component['editCancel'].emit();

        expect(getText(fixture)).toContain('WEIGHT_HISTORY.UPDATE');
        expect(getText(fixture)).toContain('WEIGHT_HISTORY.CANCEL_EDIT');
        expect(cancelHandler).toHaveBeenCalledOnce();
    });

    it('renders entry save error near the form actions', () => {
        const { fixture } = setupComponent(false, undefined, 'WEIGHT_HISTORY.ERROR_DUPLICATE_DATE');

        expect(getText(fixture)).toContain('WEIGHT_HISTORY.ERROR_DUPLICATE_DATE');
    });

    it('cancels native submit and delegates to FormRoot submission', async () => {
        const submitWeightFormAsync = vi.fn(async (): Promise<void> => {
            await waitForAsyncTasksAsync();
        });
        const { fixture } = setupComponent(false, submitWeightFormAsync);
        const formElement = (fixture.nativeElement as HTMLElement).querySelector('form');
        const submitEvent = new Event('submit', { bubbles: true, cancelable: true });

        const wasNotCancelled = formElement?.dispatchEvent(submitEvent);
        await fixture.whenStable();

        expect(formElement).not.toBeNull();
        expect(wasNotCancelled).toBe(false);
        expect(submitEvent.defaultPrevented).toBe(true);
        expect(submitWeightFormAsync).toHaveBeenCalledOnce();
    });
});

function setupComponent(
    isEditing: boolean,
    submitWeightFormAsync?: () => Promise<void>,
    error: string | null = null,
): {
    component: WeightHistoryFormCardComponent;
    fixture: ComponentFixture<WeightHistoryFormCardComponent>;
} {
    TestBed.configureTestingModule({
        imports: [WeightHistoryFormCardComponent],
        providers: [provideTranslateTesting()],
    });

    const fixture = TestBed.createComponent(WeightHistoryFormCardComponent);
    const model = signal({ date: '2026-05-15', weight: '71.5' });
    fixture.componentRef.setInput(
        'form',
        TestBed.runInInjectionContext(() => {
            if (submitWeightFormAsync === undefined) {
                return form(model);
            }

            return form(model, () => {}, {
                submission: {
                    action: submitWeightFormAsync,
                },
            });
        }),
    );
    fixture.componentRef.setInput('isSaving', false);
    fixture.componentRef.setInput('isEditing', isEditing);
    fixture.componentRef.setInput('error', error);
    fixture.detectChanges();

    return {
        component: fixture.componentInstance,
        fixture,
    };
}

function getText(fixture: ComponentFixture<WeightHistoryFormCardComponent>): string {
    return (fixture.nativeElement as HTMLElement).textContent;
}

it('offers cancel in create mode and a decimal text field with a unit', () => {
    const { fixture, component } = setupComponent(false);
    const cancel = vi.fn();
    component.editCancel.subscribe(cancel);
    const root = fixture.nativeElement as HTMLElement;
    const value = root.querySelector<HTMLInputElement>('fd-ui-input input');
    expect(value?.type).toBe('text');
    expect(value?.inputMode).toBe('decimal');
    expect(root.querySelector('.fd-ui-input__unit')).not.toBeNull();
    expect(root.querySelector('.fd-ui-input__required')).toBeNull();
    Array.from(root.querySelectorAll('button'))
        .find(button => button.textContent.includes('CANCEL_EDIT'))
        ?.click();
    expect(cancel).toHaveBeenCalledOnce();
});
