import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../testing/translate-testing.module';
import { ProfileManageFacade } from '../../../lib/profile-manage.facade';
import { UserManageBackupEmailComponent } from './user-manage-backup-email';

const RESEND_COOLDOWN_MS = 60_000;

describe('UserManageBackupEmailComponent', () => {
    const facade = {
        user: signal({ hasTelegramIdentity: true, email: null as string | null }),
        isRequestingBackupEmail: signal(false),
        backupEmailSentTo: signal<string | null>(null),
        backupEmailResendAt: signal(0),
        requestBackupEmailAsync: vi.fn(),
    };
    beforeEach(() => {
        facade.user.set({ hasTelegramIdentity: true, email: null });
        facade.isRequestingBackupEmail.set(false);
        facade.backupEmailSentTo.set(null);
        facade.backupEmailResendAt.set(0);
        facade.requestBackupEmailAsync.mockReset();
        TestBed.configureTestingModule({
            imports: [UserManageBackupEmailComponent],
            providers: [provideTranslateTesting(), { provide: ProfileManageFacade, useValue: facade }],
        });
    });

    it('rejects invalid input and forwards a valid email without claiming verification', () => {
        const fixture = TestBed.createComponent(UserManageBackupEmailComponent);
        fixture.detectChanges();
        const component = fixture.componentInstance;
        component['email'].set('invalid');
        component['submit']();
        expect(facade.requestBackupEmailAsync).not.toHaveBeenCalled();
        component['email'].set('backup@example.com');
        component['submit']();
        expect(facade.requestBackupEmailAsync).toHaveBeenCalledWith('backup@example.com');
        expect(facade.user().email).toBeNull();
    });

    it('shows the address but hides its input once an email is assigned', () => {
        facade.user.set({ hasTelegramIdentity: true, email: 'confirmed@example.com' });
        const fixture = TestBed.createComponent(UserManageBackupEmailComponent);
        fixture.detectChanges();
        expect((fixture.nativeElement as HTMLElement).querySelector('fd-ui-input')).toBeNull();
        expect((fixture.nativeElement as HTMLElement).textContent).toContain('confirmed@example.com');
    });

    it('explains an invalid submit and focuses its field without sending a request', () => {
        const fixture = TestBed.createComponent(UserManageBackupEmailComponent);
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;
        const button = host.querySelector<HTMLButtonElement>('button');
        expect(button?.disabled).toBe(false);
        button?.click();
        fixture.detectChanges();
        const input = host.querySelector<HTMLInputElement>('input');
        expect(host.querySelector('[role="alert"]')?.textContent).toContain('FORM_ERRORS.EMAIL');
        expect(document.activeElement).toBe(input);
        expect(input?.getAttribute('aria-invalid')).toBe('true');
        expect(facade.requestBackupEmailAsync).not.toHaveBeenCalled();
    });

    it('retains request and resend guards even when invoked by the keyboard', () => {
        const fixture = TestBed.createComponent(UserManageBackupEmailComponent);
        fixture.detectChanges();
        const component = fixture.componentInstance;
        component['email'].set('backup@example.com');
        facade.isRequestingBackupEmail.set(true);
        component['submit']();
        facade.isRequestingBackupEmail.set(false);
        facade.backupEmailResendAt.set(Date.now() + RESEND_COOLDOWN_MS);
        component['submit']();
        expect(facade.requestBackupEmailAsync).not.toHaveBeenCalled();
    });
});
