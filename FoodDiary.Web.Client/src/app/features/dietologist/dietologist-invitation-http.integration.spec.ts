import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting, type TestRequest } from '@angular/common/http/testing';
import { ChangeDetectionStrategy, Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { TranslateService } from '@ngx-translate/core';
import { firstValueFrom, of, throwError } from 'rxjs';
import { afterEach, beforeEach, expect, it, vi } from 'vitest';

import { environment } from '../../../environments/environment';
import { provideTranslateTesting } from '../../../testing/translate-testing.module';
import { AuthService } from '../../services/auth.service';
import type { DietologistInvitationForCurrentUserHttpResponse } from '../../shared/api/sdk/generated/model/dietologist-invitation-for-current-user-http-response';
import { FoodDiaryTranslationLoader } from '../../shared/i18n/food-diary-translation.loader';
import { DietologistInvitationPageComponent } from './pages/invitation/dietologist-invitation-page';

@Component({ template: '', changeDetection: ChangeDetectionStrategy.OnPush })
class InvitationDestinationComponent {}

const INVITATION = 'invitation-initial';
const NEXT_INVITATION = 'invitation-next';
const INVITATION_EXPIRY = '2026-05-24T09:00:00.000Z';
const URL_ROOT = '/dietologist-invitations';
const ACCEPT = 'DIETOLOGIST_INVITATION.ACCEPT';
const DECLINE = 'DIETOLOGIST_INVITATION.DECLINE';
const refresh = vi.fn<AuthService['refreshSession']>();
let http: HttpTestingController;

beforeEach(() => {
    refresh.mockReset().mockReturnValue(of(true));
    TestBed.configureTestingModule({
        providers: [
            provideRouter([
                { path: 'dietologist-invitations/:invitationId', component: DietologistInvitationPageComponent },
                { path: 'dietologist', component: InvitationDestinationComponent },
                { path: 'dashboard', component: InvitationDestinationComponent },
            ]),
            provideHttpClient(),
            provideHttpClientTesting(),
            provideTranslateTesting(),
            {
                provide: FoodDiaryTranslationLoader,
                useValue: {
                    isPublicRoute: vi.fn<FoodDiaryTranslationLoader['isPublicRoute']>().mockReturnValue(false),
                    loadApplicationTranslations: vi.fn<FoodDiaryTranslationLoader['loadApplicationTranslations']>().mockReturnValue(of({})),
                },
            },
            { provide: AuthService, useValue: { refreshSession: refresh } },
        ],
    });
    http = TestBed.inject(HttpTestingController);
});

afterEach(() => {
    try {
        http.verify();
    } finally {
        TestBed.resetTestingModule();
    }
});

it('retries accepting a pending invitation and opens the directory even if session refresh is unavailable', async () => {
    const harness = await RouterTestingHarness.create(`${URL_ROOT}/${INVITATION}`);
    expect(harness.routeNativeElement?.textContent).toContain('DIETOLOGIST_INVITATION.LOADING');
    respondToInvitation(INVITATION);
    await harness.fixture.whenStable();
    harness.detectChanges();

    button(harness, ACCEPT).click();
    const failed = actionRequest(INVITATION, 'accept-current-user');
    harness.detectChanges();
    expect(button(harness, ACCEPT).disabled).toBe(true);
    expect(button(harness, DECLINE).disabled).toBe(true);
    failed.flush(null, { status: HttpStatusCode.ServiceUnavailable, statusText: 'Unavailable' });
    harness.detectChanges();
    expect(harness.routeNativeElement?.textContent).toContain('DIETOLOGIST_INVITATION.ERROR_ACCEPT');
    expect(button(harness, ACCEPT).disabled).toBe(false);
    expect(refresh).not.toHaveBeenCalled();

    refresh.mockReturnValueOnce(throwError(() => new Error('Session refresh unavailable')));
    button(harness, ACCEPT).click();
    actionRequest(INVITATION, 'accept-current-user').flush(null, { status: HttpStatusCode.NoContent, statusText: 'No Content' });
    harness.detectChanges();
    expect(refresh).toHaveBeenCalledOnce();
    expect(harness.routeNativeElement?.textContent).toContain('DIETOLOGIST_INVITATION.SUCCESS_ACCEPT');
    button(harness, 'DIETOLOGIST_INVITATION.OPEN_CLIENTS').click();
    await vi.waitFor(() => {
        expect(TestBed.inject(Router).url).toBe('/dietologist');
    });
});

it('cancels the old decline request on navigation and submits only the newly displayed invitation', async () => {
    const harness = await RouterTestingHarness.create(`${URL_ROOT}/${INVITATION}`);
    respondToInvitation(INVITATION);
    await harness.fixture.whenStable();
    harness.detectChanges();
    button(harness, DECLINE).click();
    const old = actionRequest(INVITATION, 'decline-current-user');

    await harness.navigateByUrl(`${URL_ROOT}/${NEXT_INVITATION}`);
    expect(old.cancelled).toBe(true);
    respondToInvitation(NEXT_INVITATION);
    await harness.fixture.whenStable();
    harness.detectChanges();
    expect(button(harness, DECLINE).disabled).toBe(false);

    button(harness, DECLINE).click();
    actionRequest(NEXT_INVITATION, 'decline-current-user').flush(null, { status: HttpStatusCode.NoContent, statusText: 'No Content' });
    harness.detectChanges();
    expect(harness.routeNativeElement?.textContent).toContain('DIETOLOGIST_INVITATION.SUCCESS_DECLINE');
    expect(refresh).not.toHaveBeenCalled();
    button(harness, 'DIETOLOGIST_INVITATION.GO_HOME').click();
    await vi.waitFor(() => {
        expect(TestBed.inject(Router).url).toBe('/dashboard');
    });
});

it.each([
    {
        first: ACCEPT,
        later: DECLINE,
        ownedAction: 'accept-current-user',
        rejectedAction: 'decline-current-user',
        status: 'DIETOLOGIST_INVITATION.SUCCESS_ACCEPT',
        refreshCount: 1,
    },
    {
        first: DECLINE,
        later: ACCEPT,
        ownedAction: 'decline-current-user',
        rejectedAction: 'accept-current-user',
        status: 'DIETOLOGIST_INVITATION.SUCCESS_DECLINE',
        refreshCount: 0,
    },
] as const)('executes only $first when both rendered actions are activated before repaint', async input => {
    const harness = await RouterTestingHarness.create(`${URL_ROOT}/${INVITATION}`);
    respondToInvitation(INVITATION);
    await harness.fixture.whenStable();
    harness.detectChanges();
    const first = button(harness, input.first);
    const later = button(harness, input.later);

    first.click();
    later.click();
    const request = actionRequest(INVITATION, input.ownedAction);
    http.expectNone({ method: 'POST', url: `${invitationApi(INVITATION)}/${input.rejectedAction}` });
    harness.detectChanges();
    expect(first.disabled).toBe(true);
    expect(later.disabled).toBe(true);
    expect(refresh).not.toHaveBeenCalled();

    request.flush(null, { status: HttpStatusCode.NoContent, statusText: 'No Content' });
    harness.detectChanges();
    expect(harness.routeNativeElement?.textContent).toContain(input.status);
    expect(refresh).toHaveBeenCalledTimes(input.refreshCount);
});

it('shows an unnamed sender and a localized expiry using the configured fallback language', async () => {
    const translate = TestBed.inject(TranslateService);
    const unnamedClient = 'Клиент без имени';
    translate.setTranslation('ru', {
        ATTENTION: { UNNAMED_CLIENT: unnamedClient },
        DIETOLOGIST_INVITATION: { DESCRIPTION: '{{name}}', EXPIRES: '{{date}}' },
    });
    await firstValueFrom(translate.setFallbackLang('ru'));
    const harness = await RouterTestingHarness.create(`${URL_ROOT}/${INVITATION}`);
    respondToInvitation(INVITATION, { clientEmail: null, clientFirstName: null, clientLastName: null });
    await harness.fixture.whenStable();
    harness.detectChanges();

    const host = harness.routeNativeElement;
    expect(host?.querySelector('.dietologist-invitation__description')?.textContent.trim()).toBe(unnamedClient);
    const expectedDate = new Intl.DateTimeFormat('ru-RU', { dateStyle: 'medium' }).format(new Date(INVITATION_EXPIRY));
    expect(host?.querySelector('.dietologist-invitation__meta')?.textContent.trim()).toBe(expectedDate);
    expect(button(harness, ACCEPT).disabled).toBe(false);
    expect(button(harness, DECLINE).disabled).toBe(false);
});

function invitationApi(id: string): string {
    return `${environment.apiUrls.dietologist}/invitations/${id}`;
}

function respondToInvitation(invitationId: string, overrides: Partial<DietologistInvitationForCurrentUserHttpResponse> = {}): void {
    http.expectOne({ method: 'GET', url: `${invitationApi(invitationId)}/current-user` }).flush({
        invitationId,
        clientUserId: 'fixture-client',
        clientEmail: 'client@example.test',
        clientFirstName: 'Alex',
        clientLastName: null,
        status: 'Pending',
        createdAtUtc: '2026-05-17T09:00:00.000Z',
        expiresAtUtc: INVITATION_EXPIRY,
        ...overrides,
    } satisfies DietologistInvitationForCurrentUserHttpResponse);
}

function actionRequest(invitationId: string, action: 'accept-current-user' | 'decline-current-user'): TestRequest {
    const request = http.expectOne({ method: 'POST', url: `${invitationApi(invitationId)}/${action}` });
    expect(request.request.body).toBeNull();
    return request;
}

function button(harness: RouterTestingHarness, label: string): HTMLButtonElement {
    const buttons = Array.from(harness.routeNativeElement?.querySelectorAll('button') ?? []);
    const element = buttons.find(candidate => candidate.textContent.includes(label));
    if (element === undefined) {
        throw new Error('Expected a rendered invitation action');
    }
    return element;
}
