import { signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { form } from '@angular/forms/signals';
import { describe, expect, it, vi } from 'vitest';

import { waitForAsyncTasksAsync } from '../../../../../../testing/async-testing';
import { provideTranslateTesting } from '../../../../../../testing/translate-testing.module';
import { createEmptyLoginFieldErrors, createLoginFormModel } from '../auth-lib/auth-form.factory';
import { AuthLoginFormComponent } from './auth-login-form';

type AuthLoginFormTestContext = {
    component: AuthLoginFormComponent;
    fixture: ComponentFixture<AuthLoginFormComponent>;
};

function createComponent(): AuthLoginFormTestContext {
    TestBed.configureTestingModule({
        imports: [AuthLoginFormComponent],
        providers: [provideTranslateTesting()],
    });

    const fixture = TestBed.createComponent(AuthLoginFormComponent);
    const model = signal(createLoginFormModel());
    fixture.componentRef.setInput(
        'form',
        TestBed.runInInjectionContext(() => form(model)),
    );
    fixture.componentRef.setInput('errors', createEmptyLoginFieldErrors());
    fixture.componentRef.setInput('globalError', null);
    fixture.componentRef.setInput('isSubmitting', false);
    fixture.componentRef.setInput('isRestoring', false);
    fixture.componentRef.setInput('showRestoreAction', false);
    fixture.componentRef.setInput('googleReady', true);
    fixture.componentRef.setInput('loginSubmitLabelKey', 'AUTH.LOGIN.LOGIN');
    fixture.componentRef.setInput('isSubmitDisabled', false);
    fixture.detectChanges();

    return { component: fixture.componentInstance, fixture };
}

describe('AuthLoginFormComponent', () => {
    it('should expose login form and google button refs', () => {
        const { component } = createComponent();

        expect(component['formElement']()?.nativeElement.tagName).toBe('FORM');
        expect(component['googleButton']()?.nativeElement.classList.contains('auth__google-button')).toBe(true);
    });

    it('should cancel native submit and delegate to FormRoot submission', async () => {
        const submitLoginFormAsync = vi.fn(async (): Promise<void> => {
            await waitForAsyncTasksAsync();
        });
        const { fixture } = createComponentWithSubmitAction(submitLoginFormAsync);
        const formElement = (fixture.nativeElement as HTMLElement).querySelector('form');
        const submitEvent = new Event('submit', { bubbles: true, cancelable: true });

        const wasNotCancelled = formElement?.dispatchEvent(submitEvent);
        await fixture.whenStable();

        expect(formElement).not.toBeNull();
        expect(wasNotCancelled).toBe(false);
        expect(submitEvent.defaultPrevented).toBe(true);
        expect(submitLoginFormAsync).toHaveBeenCalledOnce();
    });

    it('should emit native input, reset open, and restore actions', () => {
        const { component, fixture } = createComponent();
        const inputSpy = vi.fn();
        const resetOpenSpy = vi.fn();
        const restoreSpy = vi.fn();
        component['loginNativeInput'].subscribe(inputSpy);
        component['passwordResetOpen'].subscribe(resetOpenSpy);
        component['restoreSubmit'].subscribe(restoreSpy);
        fixture.componentRef.setInput('showRestoreAction', true);
        fixture.detectChanges();

        const root = fixture.nativeElement as HTMLElement;
        const formElement = root.querySelector<HTMLFormElement>('form');
        if (formElement === null) {
            throw new Error('Expected login form element to render');
        }

        formElement.dispatchEvent(new Event('input', { bubbles: true }));

        const buttons = Array.from(root.querySelectorAll('button'));
        const resetButton = root.querySelector<HTMLButtonElement>('.auth__link button');
        const restoreButton = buttons.at(-1);
        resetButton?.click();
        expect(restoreButton).toBeDefined();
        restoreButton?.click();

        expect(inputSpy).toHaveBeenCalledTimes(1);
        expect(resetOpenSpy).toHaveBeenCalledTimes(1);
        expect(restoreSpy).toHaveBeenCalledTimes(1);
    });

    it('should toggle password visibility from the suffix button', () => {
        const { fixture } = createComponent();
        const root = fixture.nativeElement as HTMLElement;
        const passwordInput = root.querySelector<HTMLInputElement>('input[autocomplete="current-password"]');
        const toggleButton = root.querySelector<HTMLButtonElement>('.fd-ui-input__suffix');

        expect(passwordInput?.type).toBe('password');
        expect(toggleButton).not.toBeNull();
        expect(toggleButton?.getAttribute('aria-label')).toBe('AUTH.LOGIN.SHOW_PASSWORD');

        toggleButton?.click();
        fixture.detectChanges();

        expect(passwordInput?.type).toBe('text');
        expect(toggleButton?.getAttribute('aria-label')).toBe('AUTH.LOGIN.HIDE_PASSWORD');
    });
});

function createComponentWithSubmitAction(submitLoginFormAsync: () => Promise<void>): AuthLoginFormTestContext {
    const context = createComponent();
    const model = signal(createLoginFormModel());
    context.fixture.componentRef.setInput(
        'form',
        TestBed.runInInjectionContext(() =>
            form(model, () => {}, {
                submission: {
                    action: submitLoginFormAsync,
                    ignoreValidators: 'all',
                },
            }),
        ),
    );
    context.fixture.detectChanges();

    return context;
}
