import { signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { form } from '@angular/forms/signals';
import { TranslateModule } from '@ngx-translate/core';
import { describe, expect, it, vi } from 'vitest';

import { createEmptyLoginFieldErrors, createLoginFormModel } from '../auth-lib/auth-form.factory';
import { AuthLoginFormComponent } from './auth-login-form';

type AuthLoginFormTestContext = {
    component: AuthLoginFormComponent;
    fixture: ComponentFixture<AuthLoginFormComponent>;
};

function createComponent(): AuthLoginFormTestContext {
    TestBed.configureTestingModule({
        imports: [AuthLoginFormComponent, TranslateModule.forRoot()],
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

    it('should emit submit, native input, reset open, and restore actions', () => {
        const { component, fixture } = createComponent();
        const submitSpy = vi.fn();
        const inputSpy = vi.fn();
        const resetOpenSpy = vi.fn();
        const restoreSpy = vi.fn();
        component['loginSubmit'].subscribe(submitSpy);
        component['loginNativeInput'].subscribe(inputSpy);
        component['passwordResetOpen'].subscribe(resetOpenSpy);
        component['restoreSubmit'].subscribe(restoreSpy);
        fixture.componentRef.setInput('showRestoreAction', true);
        fixture.detectChanges();

        const root = fixture.nativeElement as HTMLElement;
        const formElement = root.querySelector('form') as HTMLFormElement;
        formElement.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
        formElement.dispatchEvent(new Event('input', { bubbles: true }));

        const buttons = Array.from(root.querySelectorAll('button'));
        const lastButton = buttons.at(-1);
        buttons.find(button => button.type === 'button')?.click();
        expect(lastButton).toBeDefined();
        lastButton?.click();

        expect(submitSpy).toHaveBeenCalledTimes(1);
        expect(inputSpy).toHaveBeenCalledTimes(1);
        expect(resetOpenSpy).toHaveBeenCalledTimes(1);
        expect(restoreSpy).toHaveBeenCalledTimes(1);
    });
});
