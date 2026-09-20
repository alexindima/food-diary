import { signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { form } from '@angular/forms/signals';
import { describe, expect, it, vi } from 'vitest';

import { waitForAsyncTasksAsync } from '../../../../../testing/async-testing';
import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { WaistHistoryFormCardComponent } from './waist-history-form-card';

describe('WaistHistoryFormCardComponent', () => {
    it('renders add mode by default', () => {
        const { fixture } = setupComponent(false);

        expect(getText(fixture)).toContain('WAIST_HISTORY.ADD');
    });

    it('renders edit mode and emits cancel', () => {
        const { component, fixture } = setupComponent(true);
        const cancelHandler = vi.fn();
        component['editCancel'].subscribe(cancelHandler);

        component['editCancel'].emit();

        expect(getText(fixture)).toContain('WAIST_HISTORY.UPDATE');
        expect(getText(fixture)).toContain('WAIST_HISTORY.CANCEL_EDIT');
        expect(cancelHandler).toHaveBeenCalledOnce();
    });

    it('renders entry save error near the form actions', () => {
        const { fixture } = setupComponent(false, undefined, 'WAIST_HISTORY.ERROR_DUPLICATE_DATE');

        expect(getText(fixture)).toContain('WAIST_HISTORY.ERROR_DUPLICATE_DATE');
    });

    it('cancels native submit and delegates to FormRoot submission', async () => {
        const submitWaistFormAsync = vi.fn(async (): Promise<void> => {
            await waitForAsyncTasksAsync();
        });
        const { fixture } = setupComponent(false, submitWaistFormAsync);
        const formElement = (fixture.nativeElement as HTMLElement).querySelector('form');
        const submitEvent = new Event('submit', { bubbles: true, cancelable: true });

        const wasNotCancelled = formElement?.dispatchEvent(submitEvent);
        await fixture.whenStable();

        expect(formElement).not.toBeNull();
        expect(wasNotCancelled).toBe(false);
        expect(submitEvent.defaultPrevented).toBe(true);
        expect(submitWaistFormAsync).toHaveBeenCalledOnce();
    });
});

function setupComponent(
    isEditing: boolean,
    submitWaistFormAsync?: () => Promise<void>,
    error: string | null = null,
): {
    component: WaistHistoryFormCardComponent;
    fixture: ComponentFixture<WaistHistoryFormCardComponent>;
} {
    TestBed.configureTestingModule({
        imports: [WaistHistoryFormCardComponent],
        providers: [provideTranslateTesting()],
    });

    const fixture = TestBed.createComponent(WaistHistoryFormCardComponent);
    const model = signal({ date: '2026-05-15', circumference: '81.5' });
    fixture.componentRef.setInput(
        'form',
        TestBed.runInInjectionContext(() => {
            if (submitWaistFormAsync === undefined) {
                return form(model);
            }

            return form(model, () => {}, {
                submission: {
                    action: submitWaistFormAsync,
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

function getText(fixture: ComponentFixture<WaistHistoryFormCardComponent>): string {
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
