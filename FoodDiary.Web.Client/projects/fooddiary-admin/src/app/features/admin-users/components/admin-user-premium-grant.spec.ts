import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { FormRoot } from '@angular/forms/signals';
import { By } from '@angular/platform-browser';
import { defer, of, Subject, throwError } from 'rxjs';
import { describe, expect, it, type Mock, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../../src/testing/translate-testing.module';
import { adminId, adminUtcInstant } from '../../../shared/models/semantics/admin-meaning';
import { AdminUsersService } from '../api/admin-users.service';
import type { AdminUserPremiumGrant } from '../models/admin-user.models';
import { AdminUserPremiumGrantComponent } from './admin-user-premium-grant';

const ACCOUNT = adminId<'user'>('account-1');
const OTHER_ACCOUNT = adminId<'user'>('account-2');
const LOCAL_END = '2099-04-12T17:23';
const NEW_GRANT: AdminUserPremiumGrant = {
    endsAtUtc: adminUtcInstant(new Date(`${LOCAL_END}:00`).toISOString()),
    reason: 'Support credit',
};
const PREVIOUS_GRANT: AdminUserPremiumGrant = {
    endsAtUtc: adminUtcInstant(new Date(`${LOCAL_END}:48.123`).toISOString()),
    reason: 'Previous credit',
};

type GrantFixture = ComponentFixture<AdminUserPremiumGrantComponent>;
type GrantApi = {
    [Method in 'getPremiumGrant' | 'setPremiumGrant' | 'revokePremiumGrant']: Mock<AdminUsersService[Method]>;
};

function api(): GrantApi {
    return {
        getPremiumGrant: vi.fn<AdminUsersService['getPremiumGrant']>().mockReturnValue(of(null)),
        setPremiumGrant: vi.fn<AdminUsersService['setPremiumGrant']>().mockReturnValue(of(NEW_GRANT)),
        revokePremiumGrant: vi.fn<AdminUsersService['revokePremiumGrant']>().mockReturnValue(of(null)),
    };
}

async function renderAsync(users: GrantApi): Promise<GrantFixture> {
    await TestBed.configureTestingModule({
        imports: [AdminUserPremiumGrantComponent],
        providers: [provideTranslateTesting(), { provide: AdminUsersService, useValue: users }],
    }).compileComponents();
    const fixture = TestBed.createComponent(AdminUserPremiumGrantComponent);
    fixture.componentRef.setInput('userId', ACCOUNT);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture;
}

function host(fixture: GrantFixture): HTMLElement {
    return fixture.nativeElement as HTMLElement;
}

function button(fixture: GrantFixture, text: string): HTMLButtonElement {
    const element = [...host(fixture).querySelectorAll<HTMLButtonElement>('button')].find(
        candidate => candidate.textContent.trim() === text,
    );
    if (element === undefined) {
        throw new Error(`Expected Premium grant button: ${text}`);
    }
    return element;
}

function field(fixture: GrantFixture, selector: string): HTMLInputElement | HTMLTextAreaElement {
    const element = host(fixture).querySelector<HTMLInputElement | HTMLTextAreaElement>(selector);
    if (element === null) {
        throw new Error(`Expected Premium grant field: ${selector}`);
    }
    return element;
}

function click(fixture: GrantFixture, text: string): void {
    button(fixture, text).click();
    fixture.detectChanges();
}

function enter(fixture: GrantFixture, selector: string, value: string): void {
    const control = field(fixture, selector);
    control.value = value;
    control.dispatchEvent(new Event('input', { bubbles: true }));
    fixture.detectChanges();
}

async function renderUpdateAsync(fixture: GrantFixture): Promise<void> {
    await fixture.whenStable();
    fixture.detectChanges();
}

describe('Manual Premium grant loading', () => {
    it('announces a failed load and retries the same user before offering a new grant', async () => {
        const users = api();
        const pending = new Subject<AdminUserPremiumGrant | null>();
        users.getPremiumGrant.mockReturnValueOnce(pending);
        const fixture = await renderAsync(users);
        expect(host(fixture).querySelector('[role="status"]')?.textContent).toContain('ADMIN_COMMON.LOADING');

        pending.error(new Error('offline'));
        fixture.detectChanges();
        expect(host(fixture).querySelector('[role="alert"]')?.textContent).toContain('ADMIN_COMMON.LOAD_ERROR');

        click(fixture, 'ADMIN_COMMON.RETRY');
        expect(users.getPremiumGrant.mock.calls).toEqual([[ACCOUNT], [ACCOUNT]]);
        expect(host(fixture).querySelector('[role="alert"]')).toBeNull();
        expect(host(fixture).textContent).toContain('ADMIN_USER_PREMIUM.EMPTY');
        expect(button(fixture, 'ADMIN_USER_PREMIUM.GRANT').disabled).toBe(false);
    });

    it('cancels the previous account read and ignores a response after its input has changed', async () => {
        const users = api();
        const pending = new Subject<AdminUserPremiumGrant | null>();
        users.getPremiumGrant.mockReturnValueOnce(pending);
        const fixture = await renderAsync(users);
        expect(pending.observed).toBe(true);

        fixture.componentRef.setInput('userId', OTHER_ACCOUNT);
        fixture.detectChanges();
        await renderUpdateAsync(fixture);
        expect(pending.observed).toBe(false);
        pending.next(PREVIOUS_GRANT);
        fixture.detectChanges();

        expect(users.getPremiumGrant.mock.calls).toEqual([[ACCOUNT], [OTHER_ACCOUNT]]);
        expect(host(fixture).textContent).toContain('ADMIN_USER_PREMIUM.EMPTY');
        expect(host(fixture).textContent).not.toContain(PREVIOUS_GRANT.reason);
    });
});

describe('Manual Premium grant editor', () => {
    it('validates required fields, date and whitespace-only reasons before creating the grant', async () => {
        const users = api();
        const fixture = await renderAsync(users);
        click(fixture, 'ADMIN_USER_PREMIUM.GRANT');
        expect(button(fixture, 'COMMON.SAVE').disabled).toBe(true);

        enter(fixture, 'input[type="datetime-local"]', '2000-04-12T17:23');
        enter(fixture, 'textarea', 'Support credit');
        click(fixture, 'COMMON.SAVE');
        await renderUpdateAsync(fixture);
        expect(host(fixture).querySelector('[role="alert"]')?.textContent).toContain('ADMIN_USER_PREMIUM.DATE_INVALID');
        expect(users.setPremiumGrant).not.toHaveBeenCalled();

        enter(fixture, 'input[type="datetime-local"]', LOCAL_END);
        enter(fixture, 'textarea', '   ');
        click(fixture, 'COMMON.SAVE');
        await renderUpdateAsync(fixture);
        expect(host(fixture).querySelector('[role="alert"]')?.textContent).toContain('ADMIN_USER_PREMIUM.REASON_INVALID');
        expect(users.setPremiumGrant).not.toHaveBeenCalled();

        enter(fixture, 'textarea', '  Support credit  ');
        click(fixture, 'COMMON.SAVE');
        await renderUpdateAsync(fixture);
        expect(users.setPremiumGrant).toHaveBeenCalledExactlyOnceWith(ACCOUNT, NEW_GRANT);
        expect(host(fixture).querySelector('form')).toBeNull();
        expect(host(fixture).querySelector('[role="alert"]')).toBeNull();
        expect(host(fixture).textContent).toContain(NEW_GRANT.reason);
    });

    it('prefills the current grant and preserves its exact expiry when editing only the reason', async () => {
        const users = api();
        const amended = { ...PREVIOUS_GRANT, reason: NEW_GRANT.reason };
        users.getPremiumGrant.mockReturnValue(of(PREVIOUS_GRANT));
        users.setPremiumGrant.mockReturnValue(of(amended));
        const fixture = await renderAsync(users);
        click(fixture, 'ADMIN_USER_PREMIUM.EDIT');
        expect(field(fixture, 'input[type="datetime-local"]').value).toBe(LOCAL_END);
        expect(field(fixture, 'textarea').value).toBe(PREVIOUS_GRANT.reason);
        enter(fixture, 'textarea', amended.reason);
        click(fixture, 'COMMON.SAVE');
        await renderUpdateAsync(fixture);

        expect(users.setPremiumGrant).toHaveBeenCalledExactlyOnceWith(ACCOUNT, amended);
        expect(host(fixture).textContent).toContain(amended.reason);
        expect(host(fixture).textContent).not.toContain(PREVIOUS_GRANT.reason);
    });

    it('retains inputs after save failure and prevents mutation while the save is pending', async () => {
        const users = api();
        const pending = new Subject<AdminUserPremiumGrant | null>();
        users.setPremiumGrant.mockReturnValueOnce(pending);
        const fixture = await renderAsync(users);
        click(fixture, 'ADMIN_USER_PREMIUM.GRANT');
        enter(fixture, 'input[type="datetime-local"]', LOCAL_END);
        enter(fixture, 'textarea', NEW_GRANT.reason);
        click(fixture, 'COMMON.SAVE');
        expect(button(fixture, 'COMMON.SAVE').disabled).toBe(true);
        expect(button(fixture, 'COMMON.CANCEL').disabled).toBe(true);
        expect(field(fixture, 'textarea').disabled).toBe(true);
        click(fixture, 'COMMON.SAVE');
        expect(users.setPremiumGrant).toHaveBeenCalledExactlyOnceWith(ACCOUNT, NEW_GRANT);

        const form = fixture.debugElement.query(By.directive(FormRoot)).injector.get(FormRoot);
        pending.error(new Error('offline'));
        await vi.waitFor(() => {
            expect(form.fieldTree()().submitting()).toBe(false);
        });
        await renderUpdateAsync(fixture);
        expect(host(fixture).querySelector('[role="alert"]')?.textContent).toContain('ADMIN_USER_PREMIUM.SAVE_ERROR');
        expect(field(fixture, 'textarea').value).toBe(NEW_GRANT.reason);
        expect(field(fixture, 'input[type="datetime-local"]').value).toBe(LOCAL_END);
        expect(button(fixture, 'COMMON.SAVE').disabled).toBe(false);
        expect(field(fixture, 'textarea').disabled).toBe(false);

        click(fixture, 'COMMON.SAVE');
        await renderUpdateAsync(fixture);
        expect(users.setPremiumGrant.mock.calls).toEqual([
            [ACCOUNT, NEW_GRANT],
            [ACCOUNT, NEW_GRANT],
        ]);
        expect(host(fixture).querySelector('[role="alert"]')).toBeNull();
        expect(host(fixture).querySelector('form')).toBeNull();
    });
});

describe('Manual Premium grant revocation', () => {
    it('submits only one revocation when the confirmation receives two clicks before rendering', async () => {
        const users = api();
        const pending = new Subject<AdminUserPremiumGrant | null>();
        const subscribe = vi.fn(() => pending);
        users.getPremiumGrant.mockReturnValue(of(PREVIOUS_GRANT));
        users.revokePremiumGrant.mockReturnValue(defer(subscribe));
        const fixture = await renderAsync(users);
        click(fixture, 'ADMIN_USER_PREMIUM.REVOKE');
        const confirm = button(fixture, 'ADMIN_USER_PREMIUM.REVOKE');

        confirm.click();
        confirm.click();
        expect(subscribe).toHaveBeenCalledOnce();
        fixture.detectChanges();
        expect(confirm.disabled).toBe(true);

        pending.next(null);
        await renderUpdateAsync(fixture);
        expect(host(fixture).textContent).toContain('ADMIN_USER_PREMIUM.EMPTY');
        expect(host(fixture).querySelector('[role="alert"]')).toBeNull();
    });

    it('requires confirmation, preserves the current grant on error and clears it after retry', async () => {
        const users = api();
        users.getPremiumGrant.mockReturnValue(of(PREVIOUS_GRANT));
        users.revokePremiumGrant.mockReturnValueOnce(throwError(() => new Error('offline')));
        const fixture = await renderAsync(users);
        click(fixture, 'ADMIN_USER_PREMIUM.REVOKE');
        expect(host(fixture).textContent).toContain('ADMIN_USER_PREMIUM.REVOKE_CONFIRM');
        expect(users.revokePremiumGrant).not.toHaveBeenCalled();
        click(fixture, 'COMMON.CANCEL');
        expect(host(fixture).textContent).not.toContain('ADMIN_USER_PREMIUM.REVOKE_CONFIRM');
        expect(users.revokePremiumGrant).not.toHaveBeenCalled();

        click(fixture, 'ADMIN_USER_PREMIUM.REVOKE');
        click(fixture, 'ADMIN_USER_PREMIUM.REVOKE');
        await renderUpdateAsync(fixture);
        expect(host(fixture).querySelector('[role="alert"]')?.textContent).toContain('ADMIN_USER_PREMIUM.SAVE_ERROR');
        expect(host(fixture).textContent).toContain(PREVIOUS_GRANT.reason);
        expect(host(fixture).textContent).toContain('ADMIN_USER_PREMIUM.REVOKE_CONFIRM');

        click(fixture, 'ADMIN_USER_PREMIUM.REVOKE');
        await renderUpdateAsync(fixture);
        expect(users.revokePremiumGrant.mock.calls).toEqual([[ACCOUNT], [ACCOUNT]]);
        expect(host(fixture).querySelector('[role="alert"]')).toBeNull();
        expect(host(fixture).textContent).toContain('ADMIN_USER_PREMIUM.EMPTY');
    });

    it.each(['success', 'error'] as const)('does not apply a stale revoke %s to a different account', async outcome => {
        const users = api();
        const pending = new Subject<AdminUserPremiumGrant | null>();
        users.getPremiumGrant.mockReturnValueOnce(of(PREVIOUS_GRANT)).mockReturnValueOnce(of(NEW_GRANT));
        users.revokePremiumGrant.mockReturnValue(pending);
        const fixture = await renderAsync(users);
        click(fixture, 'ADMIN_USER_PREMIUM.REVOKE');
        click(fixture, 'ADMIN_USER_PREMIUM.REVOKE');
        fixture.componentRef.setInput('userId', OTHER_ACCOUNT);
        fixture.detectChanges();
        await renderUpdateAsync(fixture);
        expect(users.getPremiumGrant).toHaveBeenLastCalledWith(OTHER_ACCOUNT);
        expect(host(fixture).textContent).toContain(NEW_GRANT.reason);
        expect(pending.observed).toBe(true);

        if (outcome === 'error') {
            pending.error(new Error('offline'));
        } else {
            pending.next(null);
        }
        await renderUpdateAsync(fixture);

        expect(users.revokePremiumGrant).toHaveBeenCalledExactlyOnceWith(ACCOUNT);
        expect(host(fixture).textContent).toContain(NEW_GRANT.reason);
        expect(host(fixture).textContent).not.toContain('ADMIN_USER_PREMIUM.REVOKE_CONFIRM');
        expect(host(fixture).querySelector('[role="alert"]')).toBeNull();
    });
});
