import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting, type TestRequest } from '@angular/common/http/testing';
import { signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { EMPTY } from 'rxjs';
import { afterEach, beforeEach, expect, it, type Mock, vi } from 'vitest';

import { environment } from '../../../../environments/environment';
import { provideTranslateTesting } from '../../../../testing/translate-testing.module';
import { AuthService } from '../../../services/auth.service';
import type { UserHttpResponse } from '../../../shared/api/sdk/generated/model/user-http-response';
import { AiFoodFacade } from '../../../shared/lib/ai-food.facade';
import { UserFacade } from '../../../shared/lib/user.facade';
import { entityId } from '../../../shared/models/semantics/entity-id';
import { PrivacyConsentFacade } from '../../../shared/privacy/privacy-consent.facade';
import { PrivacyConsentPurpose } from '../../../shared/privacy/privacy-consent-purpose';
import { PRIVACY_NOTICE_VERSION } from '../../../shared/privacy/privacy-notice-version';
import { AiInputBarComponent } from './ai-input-bar';

const ACCOUNT = entityId<'user'>('11111111-2222-4333-8444-555555555555');
const PROFILE_URL = `${environment.apiUrls.users}/info`;
const CONSENT_URL = `${environment.apiUrls.users}/privacy`;
const DRAFT = 'oatmeal with blueberries';
const REMINDER_HOURS = 12;
const FOLLOW_UP_HOURS = 20;
const ACCOUNT_WITHOUT_AI_CONSENT = {
    id: ACCOUNT,
    hasPassword: true,
    isActive: true,
    isEmailConfirmed: true,
    pushNotificationsEnabled: false,
    fastingPushNotificationsEnabled: false,
    socialPushNotificationsEnabled: false,
    fastingCheckInReminderHours: REMINDER_HOURS,
    fastingCheckInFollowUpReminderHours: FOLLOW_UP_HOURS,
    aiConsentAcceptedAt: null,
} satisfies UserHttpResponse;

let fixture: ComponentFixture<AiInputBarComponent>;
let http: HttpTestingController;
let parseFoodText: Mock<AiFoodFacade['parseFoodText']>;

beforeEach(() => {
    vi.stubGlobal('matchMedia', vi.fn().mockReturnValue({ matches: false }));
    parseFoodText = vi.fn<AiFoodFacade['parseFoodText']>().mockReturnValue(EMPTY);
    TestBed.configureTestingModule({
        imports: [AiInputBarComponent],
        providers: [
            provideTranslateTesting(),
            provideHttpClient(),
            provideHttpClientTesting(),
            { provide: AuthService, useValue: { isPremium: signal(true), getUserId: (): typeof ACCOUNT => ACCOUNT } },
            { provide: AiFoodFacade, useValue: { parseFoodText } },
            { provide: PrivacyConsentFacade, useValue: { ensureHealthConsentAsync: vi.fn().mockResolvedValue(true) } },
        ],
    });
    http = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(AiInputBarComponent);
    fixture.detectChanges();
});

afterEach(() => {
    try {
        http.verify();
    } finally {
        TestBed.resetTestingModule();
        vi.unstubAllGlobals();
    }
});

function recognitionInput(): HTMLInputElement {
    const input = (fixture.nativeElement as HTMLElement).querySelector<HTMLInputElement>('.ai-input-bar__input');
    if (input === null) {
        throw new Error('Expected the rendered meal description input');
    }
    return input;
}

function submitDraft(text: string): void {
    const input = recognitionInput();
    input.value = text;
    input.dispatchEvent(new Event('input', { bubbles: true }));
    input.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true, cancelable: true }));
}

async function openConsentAsync(): Promise<HTMLElement> {
    submitDraft(DRAFT);
    const info = await vi.waitFor(() => http.expectOne({ method: 'GET', url: PROFILE_URL }));
    info.flush(ACCOUNT_WITHOUT_AI_CONSENT);
    return vi.waitFor(() => {
        TestBed.tick();
        const dialog = document.querySelector<HTMLElement>('fd-ai-consent-dialog');
        if (dialog === null) {
            throw new Error('Expected the rendered consent dialog');
        }
        return dialog;
    });
}

function consentButton(dialog: HTMLElement, label: string): HTMLButtonElement {
    const button = Array.from(dialog.querySelectorAll('button')).find(item => item.textContent.trim() === label);
    if (button === undefined) {
        throw new Error(`Expected consent dialog action ${label}`);
    }
    return button;
}

async function approveConsentAsync(dialog: HTMLElement): Promise<TestRequest> {
    const accept = consentButton(dialog, 'AI_CONSENT.ACCEPT');
    expect(accept.disabled).toBe(true);
    dialog.querySelector<HTMLInputElement>('input[type="checkbox"]')?.click();
    TestBed.tick();
    expect(accept.disabled).toBe(false);
    accept.click();
    return vi.waitFor(() => http.expectOne({ method: 'PUT', url: CONSENT_URL }));
}

it('waits for the account to record AI consent before recognizing the typed meal', async () => {
    const consent = await approveConsentAsync(await openConsentAsync());
    expect(consent.request.body).toEqual({
        purpose: PrivacyConsentPurpose.AiProcessing,
        granted: true,
        noticeVersion: PRIVACY_NOTICE_VERSION,
    });
    expect(parseFoodText).not.toHaveBeenCalled();
    expect(TestBed.inject(UserFacade).user()?.aiConsentAcceptedAt).toBeNull();

    consent.flush(null, { status: HttpStatusCode.NoContent, statusText: 'No Content' });
    await vi.waitFor(() => {
        expect(parseFoodText).toHaveBeenCalledExactlyOnceWith({ text: DRAFT });
    });
    expect(TestBed.inject(UserFacade).user()?.aiConsentAcceptedAt).toEqual(expect.any(String));
});

it('requires a fresh explicit agreement when the checkbox is cleared before Accept redraws', async () => {
    const dialog = await openConsentAsync();
    const agreement = dialog.querySelector('input[type="checkbox"]');
    if (!(agreement instanceof HTMLInputElement)) {
        throw new Error('Expected the rendered consent checkbox');
    }
    const accept = consentButton(dialog, 'AI_CONSENT.ACCEPT');
    agreement.click();
    TestBed.tick();
    expect(agreement.checked).toBe(true);
    expect(accept.disabled).toBe(false);

    // Both native activations occur before Angular updates the button's disabled state.
    agreement.click();
    expect(agreement.checked).toBe(false);
    expect(accept.disabled).toBe(false);
    accept.click();

    await fixture.whenStable();
    TestBed.tick();
    const writesWithoutAgreement = http.match({ method: 'PUT', url: CONSENT_URL });
    expect(writesWithoutAgreement).toHaveLength(0);
    expect(dialog.isConnected).toBe(true);
    expect(accept.disabled).toBe(true);
    expect(parseFoodText).not.toHaveBeenCalled();

    const consent = await approveConsentAsync(dialog);
    expect(consent.request.body).toEqual({
        purpose: PrivacyConsentPurpose.AiProcessing,
        granted: true,
        noticeVersion: PRIVACY_NOTICE_VERSION,
    });
    expect(parseFoodText).not.toHaveBeenCalled();
    consent.flush(null, { status: HttpStatusCode.NoContent, statusText: 'No Content' });
    await vi.waitFor(() => {
        expect(parseFoodText).toHaveBeenCalledExactlyOnceWith({ text: DRAFT });
    });
});

it('keeps the meal draft and sends no write or recognition when the user cancels', async () => {
    const dialog = await openConsentAsync();
    consentButton(dialog, 'AI_CONSENT.CANCEL').click();
    await vi.waitFor(() => {
        expect(document.querySelector('fd-ai-consent-dialog')).toBeNull();
    });

    expect(recognitionInput().value).toBe(DRAFT);
    http.expectNone(CONSENT_URL);
    expect(parseFoodText).not.toHaveBeenCalled();
    expect(TestBed.inject(UserFacade).user()?.aiConsentAcceptedAt).toBeNull();
});

it('reports a failed consent write and recognizes the preserved meal after the user retries', async () => {
    const denied = await approveConsentAsync(await openConsentAsync());
    denied.flush(null, { status: HttpStatusCode.ServiceUnavailable, statusText: 'Service Unavailable' });

    const host = fixture.nativeElement as HTMLElement;
    await vi.waitFor(() => {
        fixture.detectChanges();
        expect(host.querySelector('[role="alert"]')?.textContent ?? '').toContain('AI_RECOGNITION.ERROR_CONSENT');
    });
    expect(recognitionInput().value).toBe(DRAFT);
    expect(parseFoodText).not.toHaveBeenCalled();
    expect(TestBed.inject(UserFacade).user()?.aiConsentAcceptedAt).toBeNull();

    const retry = await approveConsentAsync(await openConsentAsync());
    retry.flush(null, { status: HttpStatusCode.NoContent, statusText: 'No Content' });
    await vi.waitFor(() => {
        fixture.detectChanges();
        expect(host.querySelector('[role="alert"]')).toBeNull();
        expect(parseFoodText).toHaveBeenCalledExactlyOnceWith({ text: DRAFT });
    });
});

it('keeps the saved draft clear of feedback from a previous visit to the composer', async () => {
    const staleConsent = await approveConsentAsync(await openConsentAsync());
    fixture.componentRef.setInput('active', false);
    fixture.detectChanges();
    staleConsent.flush(null, { status: HttpStatusCode.ServiceUnavailable, statusText: 'Service Unavailable' });
    await new Promise<void>(resolve => {
        setTimeout(resolve);
    });

    fixture.componentRef.setInput('active', true);
    fixture.detectChanges();
    const host = fixture.nativeElement as HTMLElement;
    expect(host.querySelector('[role="alert"]')).toBeNull();
    expect(recognitionInput().value).toBe(DRAFT);
    expect(parseFoodText).not.toHaveBeenCalled();
});
