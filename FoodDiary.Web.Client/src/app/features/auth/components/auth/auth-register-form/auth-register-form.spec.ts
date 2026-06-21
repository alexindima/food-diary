import { signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { form } from '@angular/forms/signals';
import { describe, expect, it, vi } from 'vitest';

import { waitForAsyncTasksAsync } from '../../../../../../testing/async-testing';
import { provideTranslateTesting } from '../../../../../../testing/translate-testing.module';
import { createEmptyRegisterFieldErrors, createRegisterFormModel } from '../auth-lib/auth-form.factory';
import { AuthRegisterFormComponent } from './auth-register-form';

type AuthRegisterFormTestContext = {
    component: AuthRegisterFormComponent;
    fixture: ComponentFixture<AuthRegisterFormComponent>;
};

function createComponent(): AuthRegisterFormTestContext {
    TestBed.configureTestingModule({
        imports: [AuthRegisterFormComponent],
        providers: [provideTranslateTesting()],
    });

    const fixture = TestBed.createComponent(AuthRegisterFormComponent);
    const model = signal(createRegisterFormModel());
    fixture.componentRef.setInput(
        'form',
        TestBed.runInInjectionContext(() => form(model)),
    );
    fixture.componentRef.setInput('errors', createEmptyRegisterFieldErrors());
    fixture.componentRef.setInput('globalError', null);
    fixture.componentRef.setInput('isSubmitting', false);
    fixture.componentRef.setInput('googleReady', true);
    fixture.detectChanges();

    return { component: fixture.componentInstance, fixture };
}

describe('AuthRegisterFormComponent', () => {
    it('should expose google button ref and render register fields', () => {
        const { component, fixture } = createComponent();
        const root = fixture.nativeElement as HTMLElement;

        expect(component['googleButton']()?.nativeElement.classList.contains('auth__google-button')).toBe(true);
        expect(root.querySelector('fd-auth-register-fields')).not.toBeNull();
    });

    it('should cancel native submit and delegate to FormRoot submission', async () => {
        const submitRegisterFormAsync = vi.fn(async (): Promise<void> => {
            await waitForAsyncTasksAsync();
        });
        const { fixture } = createComponentWithSubmitAction(submitRegisterFormAsync);

        const root = fixture.nativeElement as HTMLElement;
        const formElement = root.querySelector('form') as HTMLFormElement;
        const submitEvent = new Event('submit', { bubbles: true, cancelable: true });
        const wasNotCancelled = formElement.dispatchEvent(submitEvent);
        await fixture.whenStable();

        expect(wasNotCancelled).toBe(false);
        expect(submitEvent.defaultPrevented).toBe(true);
        expect(submitRegisterFormAsync).toHaveBeenCalledOnce();
    });
});

function createComponentWithSubmitAction(submitRegisterFormAsync: () => Promise<void>): AuthRegisterFormTestContext {
    const context = createComponent();
    const model = signal(createRegisterFormModel());
    context.fixture.componentRef.setInput(
        'form',
        TestBed.runInInjectionContext(() =>
            form(model, () => {}, {
                submission: {
                    action: submitRegisterFormAsync,
                },
            }),
        ),
    );
    context.fixture.detectChanges();

    return context;
}
