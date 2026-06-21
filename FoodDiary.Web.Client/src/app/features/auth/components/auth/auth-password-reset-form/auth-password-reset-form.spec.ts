import { signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { form } from '@angular/forms/signals';
import { describe, expect, it, vi } from 'vitest';

import { waitForAsyncTasksAsync } from '../../../../../../testing/async-testing';
import { provideTranslateTesting } from '../../../../../../testing/translate-testing.module';
import { createEmptyPasswordResetFieldErrors, createPasswordResetFormModel } from '../auth-lib/auth-form.factory';
import { AuthPasswordResetFormComponent } from './auth-password-reset-form';

type AuthPasswordResetFormTestContext = {
    component: AuthPasswordResetFormComponent;
    fixture: ComponentFixture<AuthPasswordResetFormComponent>;
};

function createComponent(submitPasswordResetFormAsync?: () => Promise<void>): AuthPasswordResetFormTestContext {
    TestBed.configureTestingModule({
        imports: [AuthPasswordResetFormComponent],
        providers: [provideTranslateTesting()],
    });

    const fixture = TestBed.createComponent(AuthPasswordResetFormComponent);
    const model = signal(createPasswordResetFormModel());
    fixture.componentRef.setInput(
        'form',
        TestBed.runInInjectionContext(() => {
            if (submitPasswordResetFormAsync === undefined) {
                return form(model);
            }

            return form(model, () => {}, {
                submission: {
                    action: submitPasswordResetFormAsync,
                },
            });
        }),
    );
    fixture.componentRef.setInput('errors', createEmptyPasswordResetFieldErrors());
    fixture.componentRef.setInput('globalError', null);
    fixture.componentRef.setInput('isPasswordResetting', false);
    fixture.componentRef.setInput('passwordResetSent', false);
    fixture.componentRef.setInput('passwordResetCooldownSeconds', 0);
    fixture.detectChanges();

    return { component: fixture.componentInstance, fixture };
}

describe('AuthPasswordResetFormComponent', () => {
    it('should emit password reset close', () => {
        const { component } = createComponent();
        const closeSpy = vi.fn();
        component['passwordResetClose'].subscribe(closeSpy);

        component['passwordResetClose'].emit();

        expect(closeSpy).toHaveBeenCalledOnce();
    });

    it('should cancel native submit and delegate to FormRoot submission', async () => {
        const submitPasswordResetFormAsync = vi.fn(async (): Promise<void> => {
            await waitForAsyncTasksAsync();
        });
        const { fixture } = createComponent(submitPasswordResetFormAsync);
        const formElement = (fixture.nativeElement as HTMLElement).querySelector('form');
        const submitEvent = new Event('submit', { bubbles: true, cancelable: true });

        const wasNotCancelled = formElement?.dispatchEvent(submitEvent);
        await fixture.whenStable();

        expect(formElement).not.toBeNull();
        expect(wasNotCancelled).toBe(false);
        expect(submitEvent.defaultPrevented).toBe(true);
        expect(submitPasswordResetFormAsync).toHaveBeenCalledOnce();
    });
});
