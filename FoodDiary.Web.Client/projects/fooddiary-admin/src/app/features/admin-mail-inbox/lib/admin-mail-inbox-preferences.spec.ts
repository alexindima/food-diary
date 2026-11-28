import { DOCUMENT } from '@angular/common';
import { PLATFORM_ID, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { expect, it, vi } from 'vitest';

import { adminId } from '../../../shared/models/semantics/admin-meaning';
import { AdminAuthService } from '../../admin-auth/contracts/admin-session';
import { AdminMailInboxPreferences } from './admin-mail-inbox-preferences';

const SUPPORT = 'support@fooddiary.club';

function setupPreferences(platform = 'browser'): {
    preferences: AdminMailInboxPreferences;
    user: ReturnType<typeof signal<ReturnType<AdminAuthService['getUserId']>>>;
    getItem: ReturnType<typeof vi.fn<Storage['getItem']>>;
    setItem: ReturnType<typeof vi.fn<Storage['setItem']>>;
} {
    const user = signal<ReturnType<AdminAuthService['getUserId']>>(null);
    const getItem = vi.fn<Storage['getItem']>().mockReturnValue(null);
    const setItem = vi.fn<Storage['setItem']>();
    TestBed.configureTestingModule({
        providers: [
            AdminMailInboxPreferences,
            { provide: PLATFORM_ID, useValue: platform },
            { provide: DOCUMENT, useValue: { defaultView: { localStorage: { getItem, setItem } } } },
            { provide: AdminAuthService, useValue: { getUserId: user } },
        ],
    });
    return { preferences: TestBed.inject(AdminMailInboxPreferences), user, getItem, setItem };
}

it('keeps temporary mailbox preferences without writing to another user’s storage', () => {
    const { preferences, user, getItem, setItem } = setupPreferences();
    preferences.setHidden(SUPPORT, true);
    expect(preferences.hiddenRecipients()).toEqual([SUPPORT]);
    expect(setItem).not.toHaveBeenCalled();

    user.set(adminId<'user'>('new-owner'));
    getItem.mockReturnValue(JSON.stringify([SUPPORT]));
    expect(preferences.hiddenRecipients()).toEqual([SUPPORT]);
    preferences.setHidden(SUPPORT, false);
    expect(preferences.hiddenRecipients()).toEqual([]);
    expect(setItem).toHaveBeenCalledExactlyOnceWith('fooddiary-admin.mail-inbox.hidden-recipients.new-owner', '[]');
});

it.each(['{', '{"unexpected":true}'])('recovers from invalid stored mailbox preferences: %s', stored => {
    const { preferences, user, getItem, setItem } = setupPreferences();
    user.set(adminId<'user'>('admin'));
    getItem.mockReturnValue(stored);
    setItem.mockImplementation(() => {
        throw new Error('Storage denied');
    });
    expect(preferences.hiddenRecipients()).toEqual([]);
    preferences.setHidden(SUPPORT, true);
    expect(preferences.hiddenRecipients()).toEqual([SUPPORT]);
});

it('avoids accessing browser storage on the server', () => {
    const { preferences, user, getItem, setItem } = setupPreferences('server');
    user.set(adminId<'user'>('ssr'));
    expect(preferences.hiddenRecipients()).toEqual([]);
    preferences.setHidden(SUPPORT, true);
    expect(preferences.hiddenRecipients()).toEqual([SUPPORT]);
    expect(getItem).not.toHaveBeenCalled();
    expect(setItem).not.toHaveBeenCalled();
});
