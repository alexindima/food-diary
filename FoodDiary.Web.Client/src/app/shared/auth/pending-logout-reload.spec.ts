import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { environment } from '../../../environments/environment';
import { AuthService } from '../../services/auth.service';
import { NavigationService } from '../../services/navigation.service';
import { LocalizationService } from '../i18n/localization.service';
import { AuthRecoveryStateService } from './auth-recovery-state.service';

beforeEach(() => {
    localStorage.clear();
    sessionStorage.clear();
    localStorage.setItem(
        'pendingBrowserLogout',
        JSON.stringify({ sessionId: 'public-previous-session', contextId: null, telegram: false }),
    );
    TestBed.configureTestingModule({
        providers: [
            provideHttpClient(),
            provideHttpClientTesting(),
            { provide: NavigationService, useValue: { navigateToAuthAsync: vi.fn(), navigateToLandingAsync: vi.fn() } },
            { provide: LocalizationService, useValue: { applyLanguagePreferenceAsync: vi.fn(), clearStoredLanguage: vi.fn() } },
        ],
    });
});

afterEach(() => {
    TestBed.inject(HttpTestingController).verify();
    localStorage.clear();
    sessionStorage.clear();
});

describe('confirmed pending logout after reload', () => {
    it.each([HttpStatusCode.NoContent, HttpStatusCode.Unauthorized])(
        'keeps this tab anonymous after logout confirmation %s',
        async status => {
            const auth = TestBed.inject(AuthService);
            const recovery = TestBed.inject(AuthRecoveryStateService);
            const requests = TestBed.inject(HttpTestingController);
            const base = environment.apiUrls.auth;
            const startup = auth.restoreSessionAsync();
            requests.expectOne(`${base}/logout`).flush({}, { status: HttpStatusCode.ServiceUnavailable, statusText: 'Unavailable' });
            await startup;
            const retry = auth.retryPendingLogoutAsync();
            requests
                .expectOne(`${base}/logout`)
                .flush(status === HttpStatusCode.NoContent ? null : { error: 'Authentication.SessionChanged' }, {
                    status,
                    statusText: 'Finished',
                });
            await retry;
            expect(recovery.pendingLogout()).toBeNull();
            const nextRoute = auth.ensureSessionReadyAsync();
            const implicitRestore = requests.match(`${base}/session`);
            for (const request of implicitRestore) {
                request.flush({}, { status: HttpStatusCode.Unauthorized, statusText: 'Rejected' });
            }
            await nextRoute;
            expect(implicitRestore, 'Confirmed logout retry must not implicitly restore another cookie session').toHaveLength(0);
            expect(auth.isAuthenticated()).toBe(false);
        },
    );
});
