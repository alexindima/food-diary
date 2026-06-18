import { signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { form } from '@angular/forms/signals';
import { describe, expect, it, vi } from 'vitest';

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

    it('cancels native submit and delegates to FormRoot submission', async () => {
        const submitWaistFormAsync = vi.fn(async (): Promise<void> => {
            await Promise.resolve();
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
    fixture.detectChanges();

    return {
        component: fixture.componentInstance,
        fixture,
    };
}

function getText(fixture: ComponentFixture<WaistHistoryFormCardComponent>): string {
    return (fixture.nativeElement as HTMLElement).textContent;
}
