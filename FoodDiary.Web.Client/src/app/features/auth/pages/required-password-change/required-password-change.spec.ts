import { HttpErrorResponse, HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { firstValueFrom, of, Subject } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { environment } from '../../../../../environments/environment';
import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { AuthService } from '../../../../services/auth.service';
import { NavigationService } from '../../../../services/navigation.service';
import { UserFacade } from '../../../../shared/lib/user.facade';
import { RequiredPasswordChangeComponent } from './required-password-change';

const PASSWORD_CONTRACT_MAX_LENGTH = 256;
const PASSWORD_ENDPOINT = `${environment.apiUrls.users}/password`;
const PASSWORD_UPDATE = { currentPassword: 'temporary-password', newPassword: 'changed-password' };

function createBoundaryComponent(): { component: RequiredPasswordChangeComponent; changePassword: ReturnType<typeof vi.fn> } {
    const changePassword = vi.fn().mockReturnValue(of(true));
    const fixture = TestBed.configureTestingModule({
        imports: [RequiredPasswordChangeComponent],
        providers: [
            { provide: UserFacade, useValue: { changePassword } },
            { provide: AuthService, useValue: { completeRequiredPasswordChangeAsync: vi.fn().mockResolvedValue(undefined) } },
            { provide: NavigationService, useValue: { navigateToHomeAsync: vi.fn().mockResolvedValue(void 0) } },
            { provide: TranslateService, useValue: { instant: (key: string): string => key } },
        ],
    })
        .overrideComponent(RequiredPasswordChangeComponent, { set: { template: '' } })
        .createComponent(RequiredPasswordChangeComponent);
    fixture.detectChanges();
    return { component: fixture.componentInstance, changePassword };
}

describe('Required password change boundaries', () => {
    it.each(['currentPassword', 'newPassword', 'confirmPassword'] as const)('blocks an oversized %s before the API', async field => {
        const { component, changePassword } = createBoundaryComponent();
        component['formModel'].set({ currentPassword: 'current-password', newPassword: 'new-password', confirmPassword: 'new-password' });
        component['form'][field]().value.set('p'.repeat(PASSWORD_CONTRACT_MAX_LENGTH + 1));

        await component['submitAsync']();

        expect(component['form'][field]().getError('maxLength')).toBeDefined();
        expect(changePassword).not.toHaveBeenCalled();
    });

    it('submits boundary passwords with literal whitespace preserved', async () => {
        const { component, changePassword } = createBoundaryComponent();
        const currentPassword = ` ${'p'.repeat(PASSWORD_CONTRACT_MAX_LENGTH - 2)} `;
        const newPassword = ` ${'q'.repeat(PASSWORD_CONTRACT_MAX_LENGTH - 2)} `;
        component['formModel'].set({ currentPassword, newPassword, confirmPassword: newPassword });

        await component['submitAsync']();

        expect(changePassword).toHaveBeenCalledWith({ currentPassword, newPassword });
    });
});

describe('RequiredPasswordChangeComponent', () => {
    it('renders the required password-change route with its dependencies', () => {
        const fixture = TestBed.configureTestingModule({
            imports: [RequiredPasswordChangeComponent],
            providers: [
                { provide: UserFacade, useValue: { changePassword: vi.fn().mockReturnValue(of(true)) } },
                {
                    provide: AuthService,
                    useValue: {
                        completeRequiredPasswordChangeAsync: vi.fn().mockResolvedValue(undefined),
                        onLogoutAsync: vi.fn().mockResolvedValue(void 0),
                    },
                },
                { provide: NavigationService, useValue: { navigateToHomeAsync: vi.fn().mockResolvedValue(void 0) } },
                { provide: TranslateService, useValue: { instant: (key: string): string => key } },
            ],
        })
            .overrideComponent(RequiredPasswordChangeComponent, { set: { template: '' } })
            .createComponent(RequiredPasswordChangeComponent);

        fixture.detectChanges();

        expect(fixture.componentInstance).toBeTruthy();
    });
});

describe('Required password change endpoint', () => {
    it('shows the rate limit from the password endpoint and completes only after a successful retry', async () => {
        const { fixture, host, http, submit, renew, navigate } = await setupPasswordEndpointAsync();

        submit.click();
        const throttled = http.expectOne({ method: 'PATCH', url: PASSWORD_ENDPOINT });
        expect(throttled.request.body).toEqual(PASSWORD_UPDATE);
        throttled.flush(null, { status: HttpStatusCode.TooManyRequests, statusText: 'Too Many Requests' });
        await fixture.whenStable();
        fixture.detectChanges();
        http.expectNone({ method: 'PATCH', url: PASSWORD_ENDPOINT });
        expect(renew).not.toHaveBeenCalled();
        expect(navigate).not.toHaveBeenCalled();
        expect(host.querySelector('[role="alert"]')?.textContent).toContain('FORM_ERRORS.RATE_LIMITED');

        submit.click();
        const retried = http.expectOne({ method: 'PATCH', url: PASSWORD_ENDPOINT });
        expect(retried.request.body).toEqual(PASSWORD_UPDATE);
        retried.flush(null, { status: HttpStatusCode.NoContent, statusText: 'No Content' });
        await fixture.whenStable();
        fixture.detectChanges();
        expect(host.querySelector('[role="alert"]')).toBeNull();
        expect(renew).toHaveBeenCalledExactlyOnceWith(PASSWORD_UPDATE.newPassword);
        expect(navigate).toHaveBeenCalledOnce();
        http.verify();
    });

    it.each([
        { result: 'success', status: HttpStatusCode.NoContent, statusText: 'No Content' },
        { result: 'throttling', status: HttpStatusCode.TooManyRequests, statusText: 'Too Many Requests' },
    ])('ignores password update $result after the user starts logging out', async ({ status, statusText }) => {
        const { fixture, host, http, submit, renew, navigate, logout } = await setupPasswordEndpointAsync();

        submit.click();
        const pending = http.expectOne({ method: 'PATCH', url: PASSWORD_ENDPOINT });
        expect(pending.request.body).toEqual(PASSWORD_UPDATE);
        fixture.detectChanges();
        clickNativeLogout(host);
        expect(logout).toHaveBeenCalledOnce();

        pending.flush(null, { status, statusText });
        await fixture.whenStable();
        fixture.detectChanges();

        expect(host.querySelector('[role="alert"]')).toBeNull();
        expect(renew).not.toHaveBeenCalled();
        expect(navigate).not.toHaveBeenCalled();
        http.verify();
    });

    it('keeps the form clear when session renewal fails after logout starts', async () => {
        const { fixture, host, http, submit, renew, navigate, logout } = await setupPasswordEndpointAsync();
        const renewal = new Subject<void>();
        renew.mockReturnValueOnce(firstValueFrom(renewal));

        submit.click();
        const update = http.expectOne({ method: 'PATCH', url: PASSWORD_ENDPOINT });
        expect(update.request.body).toEqual(PASSWORD_UPDATE);
        update.flush(null, { status: HttpStatusCode.NoContent, statusText: 'No Content' });
        await vi.waitFor(() => {
            expect(renew).toHaveBeenCalledExactlyOnceWith(PASSWORD_UPDATE.newPassword);
        });
        fixture.detectChanges();
        expect(submit.disabled).toBe(true);

        clickNativeLogout(host);
        expect(logout).toHaveBeenCalledOnce();
        renewal.error(new Error('Authentication session changed'));
        await vi.waitFor(() => {
            fixture.detectChanges();
            expect(submit.disabled).toBe(false);
        });

        expect(host.querySelector('[role="alert"]')).toBeNull();
        expect(navigate).not.toHaveBeenCalled();
        http.verify();
    });
});

function clickNativeLogout(host: HTMLElement): void {
    const button = Array.from(host.querySelectorAll<HTMLButtonElement>('button[type="button"]')).find(element =>
        element.textContent.includes('AUTH.REQUIRED_PASSWORD_CHANGE.LOGOUT'),
    );
    if (button === undefined) {
        throw new Error('Expected a password change logout button.');
    }
    button.click();
}

type PasswordEndpointSetup = {
    fixture: ComponentFixture<RequiredPasswordChangeComponent>;
    host: HTMLElement;
    http: HttpTestingController;
    submit: HTMLButtonElement;
    renew: ReturnType<typeof vi.fn>;
    navigate: ReturnType<typeof vi.fn>;
    logout: ReturnType<typeof vi.fn>;
};

async function setupPasswordEndpointAsync(): Promise<PasswordEndpointSetup> {
    const renew = vi.fn().mockResolvedValue(undefined);
    const navigate = vi.fn().mockResolvedValue(undefined);
    const logout = vi.fn().mockResolvedValue(undefined);
    const fixture = TestBed.configureTestingModule({
        imports: [RequiredPasswordChangeComponent],
        providers: [
            provideHttpClient(),
            provideHttpClientTesting(),
            provideTranslateTesting(),
            { provide: AuthService, useValue: { completeRequiredPasswordChangeAsync: renew, onLogoutAsync: logout } },
            { provide: NavigationService, useValue: { navigateToHomeAsync: navigate } },
        ],
    }).createComponent(RequiredPasswordChangeComponent);
    fixture.detectChanges();
    await fixture.whenStable();

    const host = fixture.nativeElement as HTMLElement;
    const values = [PASSWORD_UPDATE.currentPassword, PASSWORD_UPDATE.newPassword, PASSWORD_UPDATE.newPassword];
    const inputs = host.querySelectorAll<HTMLInputElement>('input[type="password"]');
    expect(inputs).toHaveLength(values.length);
    values.forEach((value, index) => {
        inputs[index].value = value;
        inputs[index].dispatchEvent(new Event('input', { bubbles: true }));
    });
    const submit = host.querySelector<HTMLButtonElement>('button[type="submit"]');
    if (submit === null) {
        throw new Error('Expected a password change submit button.');
    }
    return { fixture, host, http: TestBed.inject(HttpTestingController), submit, renew, navigate, logout };
}

describe('Required password change completion', () => {
    it('shows the failed step and retries only session renewal after saving the password', async () => {
        const { fixture, host, http, submit, renew, navigate } = await setupPasswordEndpointAsync();
        renew.mockRejectedValueOnce(new HttpErrorResponse({ status: HttpStatusCode.ServiceUnavailable }));

        submit.click();
        const failedUpdate = http.expectOne({ method: 'PATCH', url: PASSWORD_ENDPOINT });
        failedUpdate.flush(null, { status: HttpStatusCode.ServiceUnavailable, statusText: 'Service Unavailable' });
        await vi.waitFor(() => {
            fixture.detectChanges();
            expect(host.querySelector('[role="alert"]')?.textContent).toContain('AUTH.REQUIRED_PASSWORD_CHANGE.ERROR');
        });

        expect(renew).not.toHaveBeenCalled();
        expect(navigate).not.toHaveBeenCalled();
        submit.click();
        const savedUpdate = http.expectOne({ method: 'PATCH', url: PASSWORD_ENDPOINT });
        expect(savedUpdate.request.body).toEqual(PASSWORD_UPDATE);
        savedUpdate.flush(null, { status: HttpStatusCode.NoContent, statusText: 'No Content' });
        await vi.waitFor(() => {
            fixture.detectChanges();
            expect(host.querySelector('[role="alert"]')?.textContent).toContain('AUTH.REQUIRED_PASSWORD_CHANGE.SESSION_ERROR');
        });

        expect(navigate).not.toHaveBeenCalled();
        submit.click();
        await vi.waitFor(() => {
            expect(navigate).toHaveBeenCalledOnce();
        });
        http.expectNone({ method: 'PATCH', url: PASSWORD_ENDPOINT });
        expect(renew.mock.calls).toEqual([[PASSWORD_UPDATE.newPassword], [PASSWORD_UPDATE.newPassword]]);
        http.verify();
    });

    it('retries session renewal after throttling without changing the password again or navigating with an old token', async () => {
        const changePassword = vi.fn().mockReturnValue(of(true));
        const renew = vi
            .fn()
            .mockRejectedValueOnce(new HttpErrorResponse({ status: 429 }))
            .mockResolvedValue(undefined);
        const navigate = vi.fn().mockResolvedValue(undefined);
        const fixture = TestBed.configureTestingModule({
            imports: [RequiredPasswordChangeComponent],
            providers: [
                { provide: UserFacade, useValue: { changePassword } },
                { provide: AuthService, useValue: { completeRequiredPasswordChangeAsync: renew } },
                { provide: NavigationService, useValue: { navigateToHomeAsync: navigate } },
                { provide: TranslateService, useValue: { instant: (key: string): string => key } },
            ],
        })
            .overrideComponent(RequiredPasswordChangeComponent, { set: { template: '' } })
            .createComponent(RequiredPasswordChangeComponent);
        fixture.detectChanges();
        const component = fixture.componentInstance;
        component['formModel'].set({
            currentPassword: 'temporary-password',
            newPassword: 'changed-password',
            confirmPassword: 'changed-password',
        });
        await component['submitAsync']();
        expect(navigate).not.toHaveBeenCalled();
        expect(component['errorMessage']()).toBe('FORM_ERRORS.RATE_LIMITED');
        expect(component['isSubmitting']()).toBe(false);
        await component['submitAsync']();
        expect(changePassword).toHaveBeenCalledOnce();
        expect(renew).toHaveBeenCalledTimes(2);
        expect(navigate).toHaveBeenCalledOnce();
    });
});
