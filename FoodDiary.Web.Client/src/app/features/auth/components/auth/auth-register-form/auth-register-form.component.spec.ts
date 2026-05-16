import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { describe, expect, it, vi } from 'vitest';

import { createEmptyRegisterFieldErrors, createRegisterForm } from '../auth-lib/auth-form.factory';
import { AuthRegisterFormComponent } from './auth-register-form.component';

type AuthRegisterFormTestContext = {
    component: AuthRegisterFormComponent;
    fixture: ComponentFixture<AuthRegisterFormComponent>;
};

function createComponent(): AuthRegisterFormTestContext {
    TestBed.configureTestingModule({
        imports: [AuthRegisterFormComponent, TranslateModule.forRoot()],
    });

    const fixture = TestBed.createComponent(AuthRegisterFormComponent);
    fixture.componentRef.setInput('form', createRegisterForm());
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

        expect(component.googleButton()?.nativeElement.classList.contains('auth__google-button')).toBe(true);
        expect(root.querySelector('fd-auth-register-fields')).not.toBeNull();
    });

    it('should emit register submit', () => {
        const { component, fixture } = createComponent();
        const submitSpy = vi.fn();
        component.registerSubmit.subscribe(submitSpy);

        const root = fixture.nativeElement as HTMLElement;
        const form = root.querySelector('form') as HTMLFormElement;
        form.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));

        expect(submitSpy).toHaveBeenCalledTimes(1);
    });
});
