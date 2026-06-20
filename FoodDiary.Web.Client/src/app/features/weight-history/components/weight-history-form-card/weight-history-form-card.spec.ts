import { signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { form } from '@angular/forms/signals';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { WeightHistoryFormCardComponent } from './weight-history-form-card';

describe('WeightHistoryFormCardComponent', () => {
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
            await Promise.resolve();
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
