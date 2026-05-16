import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { describe, expect, it, vi } from 'vitest';

import { createEmptyLoginFieldErrors, createLoginForm } from '../auth-lib/auth-form.factory';
import { AuthLoginFormComponent } from './auth-login-form.component';

type AuthLoginFormTestContext = {
    component: AuthLoginFormComponent;
    fixture: ComponentFixture<AuthLoginFormComponent>;
};

function createComponent(): AuthLoginFormTestContext {
    TestBed.configureTestingModule({
        imports: [AuthLoginFormComponent, TranslateModule.forRoot()],
    });

    const fixture = TestBed.createComponent(AuthLoginFormComponent);
    fixture.componentRef.setInput('form', createLoginForm());
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

        expect(component.formElement()?.nativeElement.tagName).toBe('FORM');
        expect(component.googleButton()?.nativeElement.classList.contains('auth__google-button')).toBe(true);
    });

    it('should emit submit, native input, reset open, and restore actions', () => {
        const { component, fixture } = createComponent();
        const submitSpy = vi.fn();
        const inputSpy = vi.fn();
        const resetOpenSpy = vi.fn();
        const restoreSpy = vi.fn();
        component.loginSubmit.subscribe(submitSpy);
        component.loginNativeInput.subscribe(inputSpy);
        component.passwordResetOpen.subscribe(resetOpenSpy);
        component.restoreSubmit.subscribe(restoreSpy);
        component.form().controls.email.setValue('user@example.com');
        component.form().controls.password.setValue('password123');
        fixture.componentRef.setInput('showRestoreAction', true);
        fixture.detectChanges();

        const root = fixture.nativeElement as HTMLElement;
        const form = root.querySelector('form') as HTMLFormElement;
        form.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
        form.dispatchEvent(new Event('input', { bubbles: true }));

        const buttons = Array.from(root.querySelectorAll('button'));
        buttons.find(button => button.type === 'button')?.click();
        buttons[buttons.length - 1].click();

        expect(submitSpy).toHaveBeenCalledTimes(1);
        expect(inputSpy).toHaveBeenCalledTimes(1);
        expect(resetOpenSpy).toHaveBeenCalledTimes(1);
        expect(restoreSpy).toHaveBeenCalledTimes(1);
    });
});
