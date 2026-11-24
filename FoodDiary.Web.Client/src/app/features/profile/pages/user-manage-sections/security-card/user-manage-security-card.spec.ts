import { formatDate, registerLocaleData } from '@angular/common';
import localeRu from '@angular/common/locales/ru';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { firstValueFrom, of, Subject } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { environment } from '../../../../../../environments/environment';
import { provideTranslateTesting } from '../../../../../../testing/translate-testing.module';
import { GoogleIdentityService } from '../../../../../shared/auth/google-identity.service';
import { utcInstant } from '../../../../../shared/models/semantics/date-value';
import { entityId } from '../../../../../shared/models/semantics/entity-id';
import { ActiveSessionsService } from '../../../api/active-sessions.service';
import type { ActiveSession } from '../../../models/active-session.model';
import { UserManageSecurityCardComponent } from './user-manage-security-card';

const originalClientId = environment.googleClientId;
const googleIdentityService = {
    initializeAsync: vi.fn(),
    renderButton: vi.fn(),
};

beforeEach(() => {
    environment.googleClientId = originalClientId;
    googleIdentityService.initializeAsync.mockReset();
    googleIdentityService.renderButton.mockReset();
});

afterEach(() => {
    environment.googleClientId = originalClientId;
});

describe('UserManageSecurityCardComponent', () => {
    it('shows the linked Google account without initializing another sign-in button', async () => {
        const fixture = await createFixtureAsync(true);

        const host = fixture.nativeElement as HTMLElement;
        expect(host.textContent).toContain('USER_MANAGE.GOOGLE_CONNECTED');
        expect(fixture.componentInstance.email()).toBe('alex@example.com');
        expect(googleIdentityService.initializeAsync).not.toHaveBeenCalled();
    });

    it('renders a Google link button and emits the returned credential', async () => {
        environment.googleClientId = 'client-id';
        let callback: ((credential: string) => void) | undefined;
        googleIdentityService.initializeAsync.mockImplementation((options: { callback: (credential: string) => void }) => {
            callback = options.callback;
        });
        const fixture = await createFixtureAsync(false);
        const emitted = vi.fn();
        fixture.componentInstance.googleCredential.subscribe(emitted);

        await fixture.whenStable();
        callback?.('credential');

        expect(googleIdentityService.renderButton).toHaveBeenCalled();
        expect(emitted).toHaveBeenCalledWith('credential');
    });

    it('emits a password change request from the security section', async () => {
        const fixture = await createFixtureAsync(true);
        const emitted = vi.fn();
        fixture.componentInstance.passwordChange.subscribe(emitted);

        const button = Array.from((fixture.nativeElement as HTMLElement).querySelectorAll('button')).find(candidate =>
            candidate.textContent.includes('USER_MANAGE.CHANGE_PASSWORD'),
        );
        button?.click();

        expect(emitted).toHaveBeenCalledOnce();
    });

    it('does not offer password setup before a confirmed email exists', async () => {
        const fixture = await createFixtureAsync(false);
        fixture.componentRef.setInput('canUsePassword', false);
        fixture.detectChanges();
        expect((fixture.nativeElement as HTMLElement).querySelector('fd-user-manage-password-method button')).toBeNull();
    });

    it('offers the configured bot link and emits disconnect from the Telegram card', async () => {
        const fixture = await createFixtureAsync(true);
        fixture.componentRef.setInput('hasTelegramIdentity', true);
        fixture.componentRef.setInput('canUnlinkTelegram', true);
        const disconnect = vi.fn();
        fixture.componentInstance.telegramUnlink.subscribe(disconnect);
        fixture.detectChanges();
        const card = (fixture.nativeElement as HTMLElement).querySelector('fd-user-manage-telegram-connection');
        expect(card?.querySelector('a')?.getAttribute('href')).toBe(`https://t.me/${environment.telegramBotUsername}`);
        card?.querySelector<HTMLButtonElement>('button')?.click();
        expect(disconnect).toHaveBeenCalledOnce();
    });

    it('shows the last-login explanation and unlink error beside the disabled action', async () => {
        const fixture = await createFixtureAsync(false);
        fixture.componentRef.setInput('hasTelegramIdentity', true);
        fixture.componentRef.setInput('telegramUnlinkError', 'USER_MANAGE.TELEGRAM_UNLINK_ERROR');
        fixture.detectChanges();
        const card = (fixture.nativeElement as HTMLElement).querySelector('fd-user-manage-telegram-connection');
        expect(card?.querySelector<HTMLButtonElement>('button')?.disabled).toBe(true);
        expect(card?.textContent).toContain('USER_MANAGE.TELEGRAM_BACKUP_REQUIRED');
        expect(card?.querySelector('[role="alert"]')?.textContent).toContain('USER_MANAGE.TELEGRAM_UNLINK_ERROR');
    });

    it('formats session timestamps in the selected language and updates them when the language changes', async () => {
        await verifySessionDateLocalesAsync();
    });

    it('keeps known sessions after a failed revoke and recovers through a scoped retry', async () => {
        await verifySessionRecoveryUiAsync();
    });
});

describe('Profile Google connection startup', () => {
    it('keeps the password action available when Google is unconfigured', async () => {
        environment.googleClientId = undefined;
        const fixture = await createFixtureAsync(false);
        const host = fixture.nativeElement as HTMLElement;

        expect(host.querySelector('.user-manage__method-actions [role="status"]')?.textContent).toContain('USER_MANAGE.GOOGLE_UNAVAILABLE');
        expect(host.querySelector('fd-user-manage-password-method button')).not.toBeNull();
        expect(googleIdentityService.initializeAsync).not.toHaveBeenCalled();
        expect(googleIdentityService.renderButton).not.toHaveBeenCalled();
    });

    it('shows the unavailable state after the configured Google SDK fails to initialize', async () => {
        environment.googleClientId = 'configured-client';
        googleIdentityService.initializeAsync.mockRejectedValueOnce(new Error('Identity SDK unavailable'));
        const fixture = await createFixtureAsync(false);
        const host = fixture.nativeElement as HTMLElement;
        const status = host.querySelector('.user-manage__method-actions [role="status"]');

        expect(googleIdentityService.initializeAsync).toHaveBeenCalledExactlyOnceWith(
            expect.objectContaining({ clientId: 'configured-client' }),
        );
        expect(status?.textContent).toContain('USER_MANAGE.GOOGLE_UNAVAILABLE');
        expect(status?.textContent).not.toContain('USER_MANAGE.GOOGLE_LOADING');
        expect(googleIdentityService.renderButton).not.toHaveBeenCalled();
    });

    it('re-renders the connect button in the chosen locale until the account is linked', async () => {
        environment.googleClientId = 'configured-client';
        googleIdentityService.initializeAsync.mockResolvedValue(undefined);
        const fixture = await createFixtureAsync(false);
        const host = fixture.nativeElement as HTMLElement;
        const target = host.querySelector('.user-manage__google-button');
        expect(target).not.toBeNull();

        const translate = TestBed.inject(TranslateService);
        await firstValueFrom(translate.use('en'));
        fixture.detectChanges();
        expect(googleIdentityService.renderButton).toHaveBeenLastCalledWith(target, 'filled_blue', 'en');

        await firstValueFrom(translate.use('ru'));
        fixture.detectChanges();
        expect(googleIdentityService.renderButton).toHaveBeenLastCalledWith(target, 'filled_blue', 'ru');
        expect(googleIdentityService.initializeAsync).toHaveBeenCalledOnce();

        fixture.componentRef.setInput('hasGoogleIdentity', true);
        fixture.detectChanges();
        expect(host.querySelector('.user-manage__google-button')).toBeNull();
        expect(host.textContent).toContain('USER_MANAGE.GOOGLE_CONNECTED_AS');
    });
});
async function verifySessionDateLocalesAsync(): Promise<void> {
    registerLocaleData(localeRu);
    const timestamp = utcInstant('2026-10-02T19:37:23Z');
    const session: ActiveSession = {
        id: entityId<'refresh-token-session'>('current-session'),
        isCurrent: true,
        authProvider: 'password',
        browser: 'Edge',
        operatingSystem: 'Windows',
        deviceType: 'Desktop',
        createdAtUtc: timestamp,
        lastActiveAtUtc: timestamp,
    };
    const fixture = await createFixtureAsync(true, { getAll: () => of([session]), revoke: () => of(undefined) });
    const translate = TestBed.inject(TranslateService);

    for (const language of ['ru', 'en', 'ru']) {
        await firstValueFrom(translate.use(language));
        fixture.detectChanges();
        const description = (fixture.nativeElement as HTMLElement).querySelector(
            '.user-manage__sessions .user-manage__login-method-description',
        );
        expect(description?.textContent).toContain(formatDate(timestamp, 'medium', language));
    }
}

async function verifySessionRecoveryUiAsync(): Promise<void> {
    const timestamp = utcInstant('2026-10-02T19:37:23Z');
    const currentSession: ActiveSession = {
        id: entityId<'refresh-token-session'>('current'),
        isCurrent: true,
        authProvider: 'password',
        browser: 'Edge',
        operatingSystem: 'Windows',
        deviceType: 'Desktop',
        createdAtUtc: timestamp,
        lastActiveAtUtc: timestamp,
    };
    const otherSession: ActiveSession = {
        ...currentSession,
        id: entityId<'refresh-token-session'>('other'),
        browser: 'Firefox',
        isCurrent: false,
    };
    const retry = new Subject<ActiveSession[]>();
    const revocation = new Subject<void>();
    const api = {
        getAll: vi
            .fn<ActiveSessionsService['getAll']>()
            .mockReturnValueOnce(of([currentSession, otherSession]))
            .mockReturnValueOnce(retry),
        revoke: vi.fn<ActiveSessionsService['revoke']>().mockReturnValueOnce(revocation),
    };
    const fixture = await createFixtureAsync(true, api);
    const section = (fixture.nativeElement as HTMLElement).querySelector('.user-manage__sessions');
    if (section === null) {
        throw new Error('Expected the rendered active sessions');
    }

    const revoke = sessionButton(section, 'USER_MANAGE.REVOKE_SESSION');
    revoke.click();
    fixture.detectChanges();
    expect(revoke.disabled).toBe(true);
    revoke.click();
    expect(api.revoke).toHaveBeenCalledExactlyOnceWith(otherSession.id);
    revocation.error(new Error('Session revoke failed'));
    fixture.detectChanges();

    expect(section.querySelectorAll('.user-manage__login-method')).toHaveLength(2);
    expect(section.querySelector('[role="alert"]')?.textContent).toContain('USER_MANAGE.ACTIVE_SESSIONS_REVOKE_ERROR');
    sessionButton(section, 'USER_MANAGE.ACTIVE_SESSIONS_RELOAD').click();
    fixture.detectChanges();

    expect(api.getAll).toHaveBeenCalledTimes(2);
    expect(section.querySelectorAll('.user-manage__login-method')).toHaveLength(2);
    expect(section.querySelector('[role="status"]')?.textContent).toContain('USER_MANAGE.ACTIVE_SESSIONS_LOADING');
    expect(Array.from(section.querySelectorAll('button')).every(button => button.disabled)).toBe(true);

    retry.next([currentSession]);
    retry.complete();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(section.querySelectorAll('.user-manage__login-method')).toHaveLength(1);
    expect(section.textContent).toContain('Edge');
    expect(section.textContent).not.toContain('Firefox');
    expect(section.querySelector('[role="alert"]')).toBeNull();
    expect(api.revoke).toHaveBeenCalledOnce();
}

function sessionButton(section: Element, label: string): HTMLButtonElement {
    const button = Array.from(section.querySelectorAll('button')).find(candidate => candidate.textContent.trim() === label);
    if (button === undefined) {
        throw new Error(`Expected the active session action ${label}`);
    }
    return button;
}

async function createFixtureAsync(
    hasGoogleIdentity: boolean,
    sessionApi: Pick<ActiveSessionsService, 'getAll' | 'revoke'> = emptySessionsApi,
): Promise<ComponentFixture<UserManageSecurityCardComponent>> {
    const testingModule = TestBed.configureTestingModule({
        imports: [UserManageSecurityCardComponent],
        providers: [provideTranslateTesting(), provideRouter([]), { provide: GoogleIdentityService, useValue: googleIdentityService }],
    });
    testingModule.overrideComponent(UserManageSecurityCardComponent, {
        add: { providers: [{ provide: ActiveSessionsService, useValue: sessionApi }] },
    });
    await testingModule.compileComponents();

    const fixture = TestBed.createComponent(UserManageSecurityCardComponent);
    fixture.componentRef.setInput('email', 'alex@example.com');
    fixture.componentRef.setInput('hasGoogleIdentity', hasGoogleIdentity);
    fixture.componentRef.setInput('isLinkingGoogle', false);
    fixture.componentRef.setInput('passwordActionState', {
        buttonLabelKey: 'USER_MANAGE.CHANGE_PASSWORD',
        descriptionKey: 'USER_MANAGE.CHANGE_PASSWORD_DESCRIPTION',
    });
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture;
}

const emptySessionsApi: Pick<ActiveSessionsService, 'getAll' | 'revoke'> = {
    getAll: () => of([]),
    revoke: () => of(undefined),
};
