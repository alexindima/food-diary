import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../testing/translate-testing.module';
import { ProfileManageFacade } from '../../../lib/profile-manage.facade';
import { UserManageBackupEmailComponent } from './user-manage-backup-email';

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
});
