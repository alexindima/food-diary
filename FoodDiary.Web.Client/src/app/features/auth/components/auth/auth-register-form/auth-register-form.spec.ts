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
    it('offers one new password field whose visibility can be toggled without losing its value', () => {
        const { fixture } = createComponent();
        const root = fixture.nativeElement as HTMLElement;
        expect(root.querySelectorAll('input[autocomplete="new-password"]')).toHaveLength(1);
        const control = root.querySelector<HTMLInputElement>('input[type="password"]');
        if (control === null) {
            throw new Error('Password field is required');
        }
        control.value = '  password123  ';
        control.dispatchEvent(new Event('input', { bubbles: true }));
        fixture.detectChanges();
        const toggle = root.querySelector<HTMLButtonElement>('button[aria-label="AUTH.LOGIN.SHOW_PASSWORD"]');
        expect(toggle).not.toBeNull();
        toggle?.click();
        fixture.detectChanges();
        expect(control.type).toBe('text');
        expect(control.value).toBe('  password123  ');
        expect(toggle?.getAttribute('aria-label')).toBe('AUTH.LOGIN.HIDE_PASSWORD');
        toggle?.click();
        fixture.detectChanges();
        expect(control.type).toBe('password');
        expect(control.value).toBe('  password123  ');
    });

    it('should expose google button ref and render register fields', () => {
        const { component, fixture } = createComponent();
        const root = fixture.nativeElement as HTMLElement;

        expect(component['googleButton']()?.nativeElement.classList.contains('auth__google-button')).toBe(true);
        expect(root.querySelector('fd-auth-register-fields')).not.toBeNull();
    });

    it('should render terms acceptance error', () => {
        const { fixture } = createComponent();
        const errors = createEmptyRegisterFieldErrors();
        errors.agreeTerms = 'Accept the privacy policy';
        fixture.componentRef.setInput('errors', errors);
        fixture.detectChanges();

        const root = fixture.nativeElement as HTMLElement;

        const checkbox = root.querySelector('fd-ui-checkbox');
        expect(checkbox?.querySelector('[role="alert"]')?.textContent).toContain('Accept the privacy policy');
        expect(checkbox?.querySelector('input')?.getAttribute('aria-invalid')).toBe('true');
    });

    it('should render unavailable Google hint without reserving button space', () => {
        const { fixture } = createComponent();
        fixture.componentRef.setInput('googleReady', false);
        fixture.detectChanges();

        const root = fixture.nativeElement as HTMLElement;

        expect(root.querySelector('.auth__google-button')).toBeNull();
        expect(root.querySelector('.auth__google-hint')?.textContent).toContain('AUTH.GOOGLE.UNAVAILABLE');
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
