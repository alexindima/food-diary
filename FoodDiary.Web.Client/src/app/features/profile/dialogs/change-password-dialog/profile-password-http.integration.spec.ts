import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { firstValueFrom } from 'rxjs';
import { afterEach, expect, it, onTestFinished, vi } from 'vitest';

import { environment } from '../../../../../environments/environment';
import { stubScrollIntoView } from '../../../../../testing/dom-testing';
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
    fixture.detectChanges();
    submit.click();
    const denied = await vi.waitFor(() => http.expectOne(endpoint));
    denied.flush(null, { status: HttpStatusCode.BadRequest, statusText: 'Bad Request' });
    await vi.waitFor(() => {
        fixture.detectChanges();
        expect(host.querySelector('[role="alert"]')?.textContent).toContain('USER_MANAGE.CHANGE_PASSWORD_ERROR');
    });
    expect(close).not.toHaveBeenCalled();

    fillVisiblePasswords(host, [OLD_PASSWORD, NEW_PASSWORD, NEW_PASSWORD]);
    fixture.detectChanges();
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
    fixture.detectChanges();
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

it('keeps the password dialog open when Escape is pressed during the update', async () => {
    const dialog = openPasswordOverlay();
    const closed = vi.fn<(result: boolean | undefined) => void>();
    dialog.afterClosed().subscribe(closed);
    const host = document.querySelector<HTMLElement>('fd-change-password-dialog');
    const firstField = host?.querySelector<HTMLInputElement>('input[type="password"]');
    if (host === null || firstField === null || firstField === undefined) {
        throw new Error('Expected the rendered password dialog and current-password input');
    }

    fillVisiblePasswords(host, [OLD_PASSWORD, NEW_PASSWORD, NEW_PASSWORD]);
    TestBed.tick();
    submitButton(host).click();
    const request = await vi.waitFor(() => http.expectOne({ method: 'PATCH', url: `${USER_URL}/password` }));
    try {
        expect(request.request.body).toEqual({ currentPassword: OLD_PASSWORD, newPassword: NEW_PASSWORD });
        TestBed.tick();
        expect(host.querySelector('form')?.getAttribute('aria-busy')).toBe('true');
        firstField.focus();
        expect(document.activeElement).toBe(firstField);
        firstField.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', keyCode: 27, bubbles: true }));
        TestBed.tick();
        expect(host.isConnected).toBe(true);
        expect(closed).not.toHaveBeenCalled();
    } finally {
        request.flush(null, { status: HttpStatusCode.NoContent, statusText: 'No Content' });
    }

    await vi.waitFor(() => {
        expect(closed).toHaveBeenCalledExactlyOnceWith(true);
    });
    expect(host.isConnected).toBe(false);
});

function openPasswordOverlay(): FdUiDialogRef<ChangePasswordDialogComponent, boolean> {
    vi.stubGlobal(
        'matchMedia',
        vi.fn(() => ({ matches: false })),
    );
    const pushState = vi.spyOn(window.history, 'pushState').mockImplementation(() => {});
    onTestFinished(() => {
        vi.unstubAllGlobals();
        pushState.mockRestore();
    });
    stubScrollIntoView();
    TestBed.configureTestingModule({ providers: [provideTranslateTesting(), provideHttpClient(), provideHttpClientTesting()] });
    http = TestBed.inject(HttpTestingController);
    const dialog = TestBed.inject(FdUiDialogService).open<ChangePasswordDialogComponent, ChangePasswordDialogData, boolean>(
        ChangePasswordDialogComponent,
        { preset: 'form', data: { hasPassword: true } },
    );
    TestBed.tick();
    return dialog;
}
