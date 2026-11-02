import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { FdUiDialogService } from 'fd-ui-kit';
import { of, Subject, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../testing/translate-testing.module';
import { AuthService } from '../../../services/auth.service';
import { UserService } from '../../../shared/api/user.service';
import { SessionEventsService } from '../../../shared/auth/session-events.service';
import { AccountDeletionFacade } from './account-deletion.facade';

const users = { user: signal<{ id: string } | null>({ id: 'owner' }), deleteCurrentUser: vi.fn() };
const auth = { onLogoutAsync: vi.fn() };
const dialogs = { open: vi.fn() };
const closeDialog = vi.fn();
let facade: AccountDeletionFacade;
let sessions: SessionEventsService;

beforeEach(() => {
    vi.resetAllMocks();
    users.user.set({ id: 'owner' });
    users.deleteCurrentUser.mockReturnValue(of(true));
    auth.onLogoutAsync.mockResolvedValue(undefined);
    dialogs.open.mockReturnValue({ afterClosed: () => of(true), close: closeDialog });
    TestBed.configureTestingModule({
        providers: [
            AccountDeletionFacade,
            provideTranslateTesting(),
            { provide: UserService, useValue: users },
            { provide: AuthService, useValue: auth },
            { provide: FdUiDialogService, useValue: dialogs },
        ],
    });
    facade = TestBed.inject(AccountDeletionFacade);
    sessions = TestBed.inject(SessionEventsService);
});

describe('AccountDeletionFacade', () => {
    it('uses the existing account deletion endpoint only after confirmation and logs out', async () => {
        await facade.confirmAsync();
        expect(users.deleteCurrentUser).toHaveBeenCalledOnce();
        expect(auth.onLogoutAsync).toHaveBeenCalledWith(true);
        expect(facade.busy()).toBe(false);
    });

    it('does not delete when the confirmation is cancelled', async () => {
        dialogs.open.mockReturnValue({ afterClosed: () => of(false), close: closeDialog });
        await facade.confirmAsync();
        expect(users.deleteCurrentUser).not.toHaveBeenCalled();
        expect(auth.onLogoutAsync).not.toHaveBeenCalled();
        expect(facade.failed()).toBe(false);
    });

    it.each([of(false), throwError(() => new Error('unavailable'))])(
        'keeps the account signed in and exposes a retryable deletion failure',
        async result => {
            users.deleteCurrentUser.mockReturnValue(result);
            await facade.confirmAsync();
            expect(facade.failed()).toBe(true);
            expect(facade.busy()).toBe(false);
            expect(auth.onLogoutAsync).not.toHaveBeenCalled();
        },
    );

    it('does not open duplicate confirmations', async () => {
        const confirmation = new Subject<boolean>();
        dialogs.open.mockReturnValue({ afterClosed: () => confirmation, close: closeDialog });
        const pending = facade.confirmAsync();
        await facade.confirmAsync();
        expect(dialogs.open).toHaveBeenCalledOnce();
        confirmation.next(false);
        await pending;
    });

    it('does not delete a replacement account when the previous confirmation resolves', async () => {
        const confirmation = new Subject<boolean>();
        dialogs.open.mockReturnValue({ afterClosed: () => confirmation, close: closeDialog });
        const pending = facade.confirmAsync();
        users.user.set({ id: 'replacement' });
        sessions.notifyAuthenticated();
        confirmation.next(true);
        await pending;
        expect(users.deleteCurrentUser).not.toHaveBeenCalled();
        expect(closeDialog).toHaveBeenCalledWith(false);
        expect(facade.failed()).toBe(false);
    });

    it('cancels the old request without logging out a replacement session', async () => {
        const response = new Subject<boolean>();
        users.deleteCurrentUser.mockReturnValue(response);
        const pending = facade.confirmAsync();
        await vi.waitFor(() => {
            expect(users.deleteCurrentUser).toHaveBeenCalledOnce();
        });
        users.user.set({ id: 'replacement' });
        sessions.notifyAuthenticated();
        response.next(true);
        await pending;
        expect(auth.onLogoutAsync).not.toHaveBeenCalled();
        expect(users.user()?.id).toBe('replacement');
        expect(facade.failed()).toBe(false);
    });

    it('does not delete after leaving the page with an open confirmation', async () => {
        const confirmation = new Subject<boolean>();
        dialogs.open.mockReturnValue({ afterClosed: () => confirmation, close: closeDialog });
        const pending = facade.confirmAsync();
        TestBed.resetTestingModule();
        confirmation.next(true);
        await pending;
        expect(users.deleteCurrentUser).not.toHaveBeenCalled();
        expect(closeDialog).toHaveBeenCalledWith(false);
    });
});
