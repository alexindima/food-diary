import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { AuthComponent } from '../../components/auth/auth';
import { AuthDialogComponent } from './auth-dialog';

@Component({
    selector: 'fd-auth',
    template: '',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
class AuthDialogAuthStubComponent {
    public readonly useRouting = input(true);
    public readonly initialMode = input<'login' | 'register'>('login');
    public readonly initialReturnUrl = input<string | null>(null);
    public readonly initialAdminReturnUrl = input<string | null>(null);
}

describe('AuthDialogComponent', () => {
    it('should render auth component with dialog data and close from custom close button', () => {
        const dialogRefSpy = { close: vi.fn() };

        TestBed.configureTestingModule({
            imports: [AuthDialogComponent],
            providers: [
                provideTranslateTesting(),
                { provide: FdUiDialogRef, useValue: dialogRefSpy },
                {
                    provide: FD_UI_DIALOG_DATA,
                    useValue: {
                        mode: 'register',
                        returnUrl: '/meals',
                        adminReturnUrl: '/admin/users',
                    },
                },
            ],
        });
        TestBed.overrideComponent(AuthDialogComponent, {
            remove: { imports: [AuthComponent] },
            add: { imports: [AuthDialogAuthStubComponent] },
        });

        const fixture: ComponentFixture<AuthDialogComponent> = TestBed.createComponent(AuthDialogComponent);
        fixture.detectChanges();

        const root = fixture.nativeElement as HTMLElement;
        const closeButton = root.querySelector<HTMLButtonElement>('.auth-dialog__close');
        const authStub = fixture.debugElement.query(By.directive(AuthDialogAuthStubComponent))
            .componentInstance as AuthDialogAuthStubComponent;

        expect(closeButton?.getAttribute('aria-label')).toBe('COMMON.CLOSE');
        expect(authStub.useRouting()).toBe(false);
        expect(authStub.initialMode()).toBe('register');
        expect(authStub.initialReturnUrl()).toBe('/meals');
        expect(authStub.initialAdminReturnUrl()).toBe('/admin/users');

        closeButton?.click();

        expect(dialogRefSpy.close).toHaveBeenCalledOnce();
    });
});
