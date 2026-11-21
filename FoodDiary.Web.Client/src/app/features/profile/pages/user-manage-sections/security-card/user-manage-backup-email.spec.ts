import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../testing/translate-testing.module';
import { ProfileManageFacade } from '../../../lib/profile-manage.facade';
import { UserManageBackupEmailComponent } from './user-manage-backup-email';

const RESEND_COOLDOWN_MS = 60_000;

const facade = {
    user: signal<{ hasTelegramIdentity: boolean; email: string | null; hasPassword?: boolean; isEmailConfirmed?: boolean }>({
        hasTelegramIdentity: true,
        email: null,
    }),
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

describe('UserManageBackupEmailComponent', () => {
    it('rejects invalid input and forwards a valid email without claiming verification', () => {
        const fixture = TestBed.createComponent(UserManageBackupEmailComponent);
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;
        const field = host.querySelector('input[type="email"]');
        const button = host.querySelector('button');
        if (!(field instanceof HTMLInputElement) || !(button instanceof HTMLButtonElement)) {
            throw new Error('Expected the backup email input and send action');
        }

        field.value = 'invalid';
        field.dispatchEvent(new Event('input', { bubbles: true }));
        button.click();
        fixture.detectChanges();
        expect(host.querySelector('[role="alert"]')?.textContent).toContain('FORM_ERRORS.EMAIL');

        field.value = '';
        field.dispatchEvent(new Event('input', { bubbles: true }));
        button.click();
        expect(facade.requestBackupEmailAsync).not.toHaveBeenCalled();

        field.value = 'backup@example.com';
        field.dispatchEvent(new Event('input', { bubbles: true }));
        fixture.detectChanges();
        expect(host.querySelector('[role="alert"]')).toBeNull();
        button.click();
        expect(facade.requestBackupEmailAsync).toHaveBeenCalledExactlyOnceWith('backup@example.com');
        expect(facade.user().email).toBeNull();
    });

    it('shows the address but hides its input once an email is assigned', () => {
        facade.user.set({ hasTelegramIdentity: true, email: 'confirmed@example.com' });
        const fixture = TestBed.createComponent(UserManageBackupEmailComponent);
        fixture.detectChanges();
        expect((fixture.nativeElement as HTMLElement).querySelector('fd-ui-input')).toBeNull();
        expect((fixture.nativeElement as HTMLElement).textContent).toContain('confirmed@example.com');
    });

    it.each([false, true])('describes confirmed email according to password availability %s', hasPassword => {
        facade.user.set({ hasTelegramIdentity: false, email: 'user@example.test', isEmailConfirmed: true, hasPassword });
        const fixture = TestBed.createComponent(UserManageBackupEmailComponent);
        fixture.detectChanges();
        expect((fixture.nativeElement as HTMLElement).querySelector('[role="status"]')?.textContent).toContain(
            hasPassword ? 'USER_MANAGE.EMAIL_PASSWORD_READY' : 'USER_MANAGE.EMAIL_PASSWORD_MISSING',
        );
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

describe('Backup email resend from the rendered field', () => {
    it('prefills the sent address, blocks Enter during cooldown, and resends when the wait ends', () => {
        vi.useFakeTimers();
        facade.backupEmailSentTo.set('pending@example.test');
        facade.backupEmailResendAt.set(Date.now() + RESEND_COOLDOWN_MS);
        const fixture = TestBed.createComponent(UserManageBackupEmailComponent);
        try {
            fixture.detectChanges();
            const host = fixture.nativeElement as HTMLElement;
            const field = host.querySelector('input[type="email"]');
            const button = host.querySelector('button');
            if (!(field instanceof HTMLInputElement) || !(button instanceof HTMLButtonElement)) {
                throw new Error('Expected the backup email resend controls');
            }
            expect(field.value).toBe('pending@example.test');
            expect(button.disabled).toBe(true);
            expect(host.querySelector('[role="status"]')?.textContent).toContain('USER_MANAGE.BACKUP_EMAIL_SENT');

            field.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true, cancelable: true }));
            expect(facade.requestBackupEmailAsync).not.toHaveBeenCalled();
            vi.advanceTimersByTime(RESEND_COOLDOWN_MS);
            fixture.detectChanges();
            expect(button.disabled).toBe(false);

            field.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true, cancelable: true }));
            expect(facade.requestBackupEmailAsync).toHaveBeenCalledExactlyOnceWith('pending@example.test');
            expect(facade.user().email).toBeNull();
        } finally {
            fixture.destroy();
            vi.useRealTimers();
        }
    });
});
