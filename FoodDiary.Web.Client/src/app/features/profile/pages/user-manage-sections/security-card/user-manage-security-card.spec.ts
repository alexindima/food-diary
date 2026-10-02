import { formatDate, registerLocaleData } from '@angular/common';
import localeRu from '@angular/common/locales/ru';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { firstValueFrom } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { environment } from '../../../../../../environments/environment';
import { provideTranslateTesting } from '../../../../../../testing/translate-testing.module';
import { GoogleIdentityService } from '../../../../../shared/auth/google-identity.service';
import { UserManageSecurityCardComponent } from './user-manage-security-card';

describe('UserManageSecurityCardComponent', () => {
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

    it('formats session timestamps in the selected language and updates them when the language changes', async () => {
        await verifySessionDateLocalesAsync(await createFixtureAsync(true));
    });

    it('keeps known sessions visible after an error and offers a guarded reload', async () => {
        verifySessionRecoveryUi(await createFixtureAsync(true));
    });

    async function createFixtureAsync(hasGoogleIdentity: boolean): Promise<ComponentFixture<UserManageSecurityCardComponent>> {
        await TestBed.configureTestingModule({
            imports: [UserManageSecurityCardComponent],
            providers: [provideTranslateTesting(), provideRouter([]), { provide: GoogleIdentityService, useValue: googleIdentityService }],
        }).compileComponents();

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
});

async function verifySessionDateLocalesAsync(fixture: ComponentFixture<UserManageSecurityCardComponent>): Promise<void> {
    registerLocaleData(localeRu);
    const translate = TestBed.inject(TranslateService);
    const timestamp = '2026-10-02T19:37:23Z';
    fixture.componentInstance['activeSessions'].isLoading.set(false);
    fixture.componentInstance['activeSessions'].error.set(null);
    fixture.componentInstance['activeSessions'].sessions.set([
        {
            id: 'current-session',
            isCurrent: true,
            authProvider: 'password',
            browser: 'Edge',
            operatingSystem: 'Windows',
            deviceType: 'Desktop',
            createdAtUtc: timestamp,
            lastActiveAtUtc: timestamp,
        },
    ]);

    for (const language of ['ru', 'en', 'ru']) {
        await firstValueFrom(translate.use(language));
        fixture.detectChanges();
        const description = (fixture.nativeElement as HTMLElement).querySelector(
            '.user-manage__sessions .user-manage__login-method-description',
        );
        expect(description?.textContent).toContain(formatDate(timestamp, 'medium', language));
    }
}

function verifySessionRecoveryUi(fixture: ComponentFixture<UserManageSecurityCardComponent>): void {
    const activeSessions = fixture.componentInstance['activeSessions'];
    const timestamp = '2026-10-02T19:37:23Z';
    const currentSession = {
        id: 'current',
        isCurrent: true,
        authProvider: 'password',
        browser: 'Edge',
        operatingSystem: 'Windows',
        deviceType: 'Desktop',
        createdAtUtc: timestamp,
        lastActiveAtUtc: timestamp,
    };
    activeSessions.isLoading.set(false);
    activeSessions.sessions.set([currentSession, { ...currentSession, id: 'other', isCurrent: false }]);
    activeSessions.error.set('revoke');
    fixture.detectChanges();

    const section = (fixture.nativeElement as HTMLElement).querySelector('.user-manage__sessions');
    expect(section?.querySelectorAll('.user-manage__login-method')).toHaveLength(2);
    expect(section?.querySelector('[role="alert"]')).not.toBeNull();
    const reload = Array.from(section?.querySelectorAll('button') ?? []).find(button =>
        button.textContent.includes('USER_MANAGE.ACTIVE_SESSIONS_RELOAD'),
    );
    expect(reload).toBeDefined();
    const load = vi.spyOn(activeSessions, 'load').mockImplementation(() => {});
    reload?.click();
    expect(load).toHaveBeenCalledOnce();

    activeSessions.isLoading.set(true);
    fixture.detectChanges();
    expect(section?.querySelectorAll('.user-manage__login-method')).toHaveLength(2);
    expect(Array.from(section?.querySelectorAll('button') ?? []).every(button => button.disabled)).toBe(true);
}
