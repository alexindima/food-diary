import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { firstValueFrom } from 'rxjs';
import { afterEach, expect, it, vi } from 'vitest';

import { environment } from '../../../../../environments/environment';
import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import type { UserHttpResponse } from '../../../../shared/api/sdk/generated/model/user-http-response';
import { UserFacade } from '../../../../shared/lib/user.facade';
import { entityId } from '../../../../shared/models/semantics/entity-id';
import { ChangePasswordDialogComponent, type ChangePasswordDialogData } from './change-password-dialog';

const ACCOUNT = entityId<'user'>('aaaaaaaa-5555-4444-8888-222222222222');
const REMINDER_HOURS = 12;
const FOLLOW_UP_HOURS = 20;
const PROFILE = {
    id: ACCOUNT,
    hasPassword: false,
    isActive: true,
    isEmailConfirmed: true,
    pushNotificationsEnabled: false,
    fastingPushNotificationsEnabled: false,
    socialPushNotificationsEnabled: false,
    fastingCheckInReminderHours: REMINDER_HOURS,
    fastingCheckInFollowUpReminderHours: FOLLOW_UP_HOURS,
} satisfies UserHttpResponse;
const USER_URL = environment.apiUrls.users;
const OLD_PASSWORD = ' existing-fixture-password ';
const NEW_PASSWORD = ' next-fixture-password ';

let http: HttpTestingController;
let fixture: ComponentFixture<ChangePasswordDialogComponent>;

function renderPasswordDialog(hasPassword: boolean): { close: ReturnType<typeof vi.fn>; user: UserFacade; host: HTMLElement } {
    const close = vi.fn();
    TestBed.configureTestingModule({
        imports: [ChangePasswordDialogComponent],
        providers: [
            provideTranslateTesting(),
            provideHttpClient(),
            provideHttpClientTesting(),
            { provide: FdUiDialogRef, useValue: { close } },
            { provide: FD_UI_DIALOG_DATA, useValue: { hasPassword } satisfies ChangePasswordDialogData },
        ],
    });
    http = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(ChangePasswordDialogComponent);
    fixture.detectChanges();
    return { close, user: TestBed.inject(UserFacade), host: fixture.nativeElement as HTMLElement };
}

afterEach(() => {
    try {
        http.verify();
    } finally {
        TestBed.resetTestingModule();
    }
});

function fillVisiblePasswords(host: HTMLElement, passwords: string[]): void {
    const fields = host.querySelectorAll<HTMLInputElement>('input[type="password"]');
    expect(fields).toHaveLength(passwords.length);
    fields.forEach((field, index) => {
        field.value = passwords[index];
        field.dispatchEvent(new Event('input', { bubbles: true }));
    });
    fixture.detectChanges();
}

function submitButton(host: HTMLElement): HTMLButtonElement {
    const button = host.querySelector<HTMLButtonElement>('button[type="submit"]');
    if (button === null) {
        throw new Error('Expected password dialog submit button');
    }
    return button;
}

it('shows a rejected current-password change and retries exactly the literal corrected values', async () => {
    const { close, host } = renderPasswordDialog(true);
    const endpoint = { method: 'PATCH', url: `${USER_URL}/password` };
    const submit = submitButton(host);
    fillVisiblePasswords(host, ['incorrect-fixture-password', NEW_PASSWORD, NEW_PASSWORD]);
    submit.click();
    const denied = await vi.waitFor(() => http.expectOne(endpoint));
    denied.flush(null, { status: HttpStatusCode.BadRequest, statusText: 'Bad Request' });
    await vi.waitFor(() => {
        fixture.detectChanges();
        expect(host.querySelector('[role="alert"]')?.textContent).toContain('USER_MANAGE.CHANGE_PASSWORD_ERROR');
    });
    expect(close).not.toHaveBeenCalled();

    fillVisiblePasswords(host, [OLD_PASSWORD, NEW_PASSWORD, NEW_PASSWORD]);
    submit.click();
    const retry = await vi.waitFor(() => http.expectOne(endpoint));
    expect(retry.request.body).toEqual({ currentPassword: OLD_PASSWORD, newPassword: NEW_PASSWORD });
    retry.flush(null, { status: HttpStatusCode.NoContent, statusText: 'No Content' });
    await vi.waitFor(() => {
        expect(close).toHaveBeenCalledExactlyOnceWith(true);
    });
});

it('sets the first password through the dedicated route and promotes the cached profile after success', async () => {
    const { close, user, host } = renderPasswordDialog(false);
    const profileRead = firstValueFrom(user.getInfo());
    http.expectOne({ method: 'GET', url: `${USER_URL}/info` }).flush(PROFILE);
    await expect(profileRead).resolves.toMatchObject({ id: ACCOUNT, hasPassword: false });

    fillVisiblePasswords(host, [NEW_PASSWORD, NEW_PASSWORD]);
    submitButton(host).click();
    const set = await vi.waitFor(() => http.expectOne({ method: 'PATCH', url: `${USER_URL}/password/set` }));
    expect(set.request.body).toEqual({ newPassword: NEW_PASSWORD });
    expect(user.user()?.hasPassword).toBe(false);
    expect(close).not.toHaveBeenCalled();
    set.flush(null, { status: HttpStatusCode.NoContent, statusText: 'No Content' });

    await vi.waitFor(() => {
        expect(close).toHaveBeenCalledExactlyOnceWith(true);
    });
    expect(user.user()?.hasPassword).toBe(true);
});
