import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { firstValueFrom } from 'rxjs';
import { expect, it, vi } from 'vitest';

import { environment } from '../../../../environments/environment';
import { provideTranslateTesting } from '../../../../testing/translate-testing.module';
import type { UserHttpResponse } from '../../../shared/api/sdk/generated/model/user-http-response';
import { UserFacade } from '../../../shared/lib/user.facade';
import { ThemeService } from '../../../shared/theme/theme.service';
import { AppearanceDialogComponent, type AppearanceDialogData } from './appearance-dialog';

const BASE_URL = environment.apiUrls.users;
const APPEARANCE_ENDPOINT = { method: 'PATCH', url: `${BASE_URL}/preferences/appearance` };
const SAVED_APPEARANCE = { theme: 'ocean', uiStyle: 'classic', surfaceStyle: 'normal' } as const;
const REMINDER_HOURS = 12;
const FOLLOW_UP_HOURS = 20;
const ACCOUNT = {
    id: '72b286fa-d144-480a-a22a-f68414900943',
    email: 'appearance@example.com',
    hasPassword: true,
    isActive: true,
    isEmailConfirmed: true,
    pushNotificationsEnabled: false,
    fastingPushNotificationsEnabled: false,
    socialPushNotificationsEnabled: false,
    fastingCheckInReminderHours: REMINDER_HOURS,
    fastingCheckInFollowUpReminderHours: FOLLOW_UP_HOURS,
    ...SAVED_APPEARANCE,
} satisfies UserHttpResponse;

it('rolls back a queued theme after the account save fails and persists an explicit retry', async () => {
    const theme = {
        surfaceStyle: signal(SAVED_APPEARANCE.surfaceStyle),
        setTheme: vi.fn<ThemeService['setTheme']>(),
        syncWithUserPreferences: vi.fn<ThemeService['syncWithUserPreferences']>(),
    };
    TestBed.configureTestingModule({
        imports: [AppearanceDialogComponent],
        providers: [
            provideHttpClient(),
            provideHttpClientTesting(),
            provideTranslateTesting(),
            { provide: ThemeService, useValue: theme },
            {
                provide: FD_UI_DIALOG_DATA,
                useValue: { theme: 'ocean', uiStyle: 'classic', persistence: 'account' } satisfies AppearanceDialogData,
            },
        ],
    });
    const requests = TestBed.inject(HttpTestingController);
    const user = TestBed.inject(UserFacade);
    const initialRead = firstValueFrom(user.getInfo());
    requests.expectOne({ method: 'GET', url: `${BASE_URL}/info` }).flush(ACCOUNT);
    await initialRead;

    const fixture = TestBed.createComponent(AppearanceDialogComponent);
    fixture.detectChanges();
    const host = fixture.nativeElement as HTMLElement;
    const ocean = themeButton(host, 'ocean');
    const dark = themeButton(host, 'dark');
    const leaf = themeButton(host, 'leaf');
    dark.click();
    const first = requests.expectOne(APPEARANCE_ENDPOINT);
    expect(first.request.body).toEqual({ ...SAVED_APPEARANCE, theme: 'dark' });
    leaf.click();
    fixture.detectChanges();
    expect(leaf.getAttribute('aria-pressed')).toBe('true');
    expect(host.querySelector('.appearance-dialog')?.getAttribute('aria-busy')).toBe('true');
    requests.expectNone(APPEARANCE_ENDPOINT);

    const preventExpectedErrorLog = vi.spyOn(console, 'error').mockImplementation(() => {});
    try {
        first.flush(null, { status: HttpStatusCode.ServiceUnavailable, statusText: 'Service Unavailable' });
    } finally {
        preventExpectedErrorLog.mockRestore();
    }
    fixture.detectChanges();
    expect(ocean.getAttribute('aria-pressed')).toBe('true');
    expect(host.querySelector('[role="alert"]')?.textContent).toContain('DASHBOARD.APPEARANCE.ERROR');
    expect(theme.syncWithUserPreferences).toHaveBeenLastCalledWith('ocean', 'classic', 'normal');
    expect(user.user()?.theme).toBe('ocean');
    requests.expectNone(APPEARANCE_ENDPOINT);

    leaf.click();
    const retry = requests.expectOne(APPEARANCE_ENDPOINT);
    expect(retry.request.body).toEqual({ ...SAVED_APPEARANCE, theme: 'leaf' });
    retry.flush({ ...ACCOUNT, theme: 'leaf' });
    fixture.detectChanges();

    expect(leaf.getAttribute('aria-pressed')).toBe('true');
    expect(host.querySelector('[role="alert"]')).toBeNull();
    expect(host.querySelector('.appearance-dialog')?.getAttribute('aria-busy')).toBe('false');
    expect(theme.syncWithUserPreferences).toHaveBeenLastCalledWith('leaf', 'classic', 'normal');
    expect(user.user()).toMatchObject({ id: ACCOUNT.id, theme: 'leaf' });
    requests.verify();
});

function themeButton(host: HTMLElement, theme: 'ocean' | 'dark' | 'leaf'): HTMLButtonElement {
    const button = host.querySelector(`.appearance-dialog__swatch--${theme}`)?.closest('button');
    if (button === null || button === undefined) {
        throw new Error(`Expected the ${theme} theme button`);
    }
    return button;
}
