import type { ComponentFixture } from '@angular/core/testing';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import type { Observable } from 'rxjs';
import { firstValueFrom, of, Subject, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import type { AttentionSignal, BulkRecommendationResult } from '../../../../shared/models/dietologist.data';
import { utcInstant } from '../../../../shared/models/semantics/date-value';
import { entityId, optionalEntityId } from '../../../../shared/models/semantics/entity-id';
import { BulkRecommendationReviewComponent } from '../../dialogs/bulk-recommendation-review/bulk-recommendation-review';
import { DietologistFacade } from '../../lib/dietologist.facade';
import { createClient } from './dietologist-clients-lib/dietologist-clients.test-data';
import { DietologistClientsPageComponent } from './dietologist-clients-page';

let fixture: ComponentFixture<DietologistClientsPageComponent>;
let component: DietologistClientsPageComponent;
let dietologistService: {
    getMyClients: ReturnType<typeof vi.fn>;
    getAttentionSignals: ReturnType<typeof vi.fn>;
    setAttentionSignalState: ReturnType<typeof vi.fn>;
    bulkCreateRecommendations: ReturnType<typeof vi.fn>;
};
let router: { navigate: ReturnType<typeof vi.fn> };
let dialogService: { open: ReturnType<typeof vi.fn> };

beforeEach(() => {
    dietologistService = {
        getMyClients: vi.fn(() => of([createClient()])),
        getAttentionSignals: vi.fn(() => of([])),
        setAttentionSignalState: vi.fn(() => of(undefined)),
        bulkCreateRecommendations: vi.fn(),
    };
    router = {
        navigate: vi.fn().mockResolvedValue(true),
    };
    dialogService = { open: vi.fn(() => ({ afterClosed: (): Observable<boolean> => of(false) })) };
});

describe('Dietologist bulk recommendation validation reset', () => {
    it('resets touched validation after sending successfully so the cleared draft has no visible error', () => {
        prepareBulkRecommendation();
        component['bulkForm'].text().markAsTouched();
        dietologistService.bulkCreateRecommendations.mockReturnValueOnce(
            of({
                idempotencyKey: 'sent-key',
                recipients: [bulkRecipient('client-1', true), bulkRecipient('client-2', true)],
            }),
        );

        component['sendBulkRecommendation']();
        fixture.detectChanges();

        expect(component['bulkModel']().text).toBe('');
        expect(component['bulkForm'].text().touched()).toBe(false);
        expect(component['selectedClientIds']().size).toBe(0);
        expect(
            (fixture.nativeElement as HTMLElement).querySelector('[aria-labelledby="bulk-recommendations-title"] [role="alert"]'),
        ).toBeNull();
        expect((fixture.nativeElement as HTMLElement).querySelector('textarea')?.getAttribute('aria-invalid')).not.toBe('true');
    });
});

describe('Dietologist bulk recommendation recovery', () => {
    it.each(['   ', '\t\n', '\u00A0'])('shows required validation for whitespace-only text %j', text => {
        prepareBulkRecommendation();
        component['bulkModel'].set({ text });
        component['bulkForm'].text().markAsTouched();
        fixture.detectChanges();

        expect(component['bulkForm'].text().errors()).toEqual(expect.arrayContaining([expect.objectContaining({ kind: 'required' })]));
        expect((fixture.nativeElement as HTMLElement).querySelector('textarea')?.getAttribute('aria-invalid')).toBe('true');
        expect(
            (fixture.nativeElement as HTMLElement).querySelector('[aria-labelledby="bulk-recommendations-title"] [role="alert"]')
                ?.textContent,
        ).toContain('FORM_ERRORS.REQUIRED');
        expect(
            (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>(
                '[aria-labelledby="bulk-recommendations-title"] form button',
            )?.disabled,
        ).toBe(false);
        component['sendBulkRecommendation']();
        expect(dialogService.open).not.toHaveBeenCalled();
        expect(dietologistService.bulkCreateRecommendations).not.toHaveBeenCalled();
    });

    it.each([true, false])('keeps failed recipients and text after partial result (first succeeded: %s)', firstSucceeded => {
        prepareBulkRecommendation();
        const result: BulkRecommendationResult = {
            idempotencyKey: 'first-key',
            recipients: [bulkRecipient('client-1', firstSucceeded), bulkRecipient('client-2', false)],
        };
        dietologistService.bulkCreateRecommendations.mockReturnValueOnce(of(result)).mockReturnValueOnce(
            of({
                ...result,
                recipients: result.recipients
                    .filter(recipient => !recipient.succeeded)
                    .map(recipient => bulkRecipient(recipient.clientUserId, true)),
            }),
        );

        component['sendBulkRecommendation']();
        expect(component['bulkModel']().text).toBe('QA recommendation');
        expect([...component['selectedClientIds']()]).toEqual(firstSucceeded ? ['client-2'] : ['client-1', 'client-2']);
        const firstKey = dietologistService.bulkCreateRecommendations.mock.calls[0][2] as string;
        component['sendBulkRecommendation']();
        expect(dietologistService.bulkCreateRecommendations.mock.calls[1][0]).toEqual(
            firstSucceeded ? ['client-2'] : ['client-1', 'client-2'],
        );
        expect(dietologistService.bulkCreateRecommendations.mock.calls[1][2]).not.toBe(firstKey);
        expect(component['bulkModel']().text).toBe('');
        expect(component['selectedClientIds']().size).toBe(0);
    });

    it('locks the draft while pending and preserves the request key for retry after an unknown failure', () => {
        prepareBulkRecommendation();
        const request = new Subject<BulkRecommendationResult>();
        dietologistService.bulkCreateRecommendations.mockReturnValueOnce(request).mockReturnValueOnce(
            of({
                idempotencyKey: 'retry-key',
                recipients: [bulkRecipient('client-1', true), bulkRecipient('client-2', true)],
            }),
        );
        component['sendBulkRecommendation']();
        fixture.detectChanges();
        expect((fixture.nativeElement as HTMLElement).querySelector('textarea')?.disabled).toBe(true);
        expect(
            Array.from((fixture.nativeElement as HTMLElement).querySelectorAll<HTMLInputElement>('input[type="checkbox"]')).every(
                checkbox => checkbox.disabled,
            ),
        ).toBe(true);
        component['sendBulkRecommendation']();
        expect(dietologistService.bulkCreateRecommendations).toHaveBeenCalledTimes(1);

        request.error(new Error('response lost'));
        fixture.detectChanges();
        expect((fixture.nativeElement as HTMLElement).querySelector('textarea')?.disabled).toBe(false);
        expect(component['bulkModel']().text).toBe('QA recommendation');
        expect(component['selectedClientIds']().size).toBe(2);
        component['sendBulkRecommendation']();
        expect(dietologistService.bulkCreateRecommendations.mock.calls[1]).toEqual(
            dietologistService.bulkCreateRecommendations.mock.calls[0],
        );
    });
});

describe('Dietologist bulk retry request identity', () => {
    it.each(['text', 'recipients', 'selection-order', 'whitespace'] as const)('handles %s changes after an unknown failure', change => {
        prepareBulkRecommendation();
        dietologistService.bulkCreateRecommendations
            .mockReturnValueOnce(throwError(() => new Error('response lost')))
            .mockReturnValueOnce(of({ idempotencyKey: 'retry-key', recipients: [bulkRecipient('client-1', true)] }));
        component['sendBulkRecommendation']();
        const firstKey = dietologistService.bulkCreateRecommendations.mock.calls[0][2] as string;

        if (change === 'text') {
            component['bulkModel'].set({ text: 'QA revised recommendation' });
        } else if (change === 'recipients') {
            component['toggleClientSelection'](entityId<'user'>('client-2'), false);
        } else if (change === 'selection-order') {
            component['toggleClientSelection'](entityId<'user'>('client-1'), false);
            component['toggleClientSelection'](entityId<'user'>('client-1'), true);
        } else {
            component['bulkModel'].set({ text: '  QA recommendation  ' });
        }
        component['sendBulkRecommendation']();
        const secondKey = dietologistService.bulkCreateRecommendations.mock.calls[1][2] as string;
        if (change === 'text' || change === 'recipients') {
            expect(secondKey).not.toBe(firstKey);
        } else {
            expect(secondKey).toBe(firstKey);
        }
        expect(dietologistService.bulkCreateRecommendations.mock.calls[1][1]).toBe(
            change === 'text' ? 'QA revised recommendation' : 'QA recommendation',
        );
    });
});

function bulkRecipient(clientUserId: string, succeeded: boolean): BulkRecommendationResult['recipients'][number] {
    return {
        clientUserId: entityId<'user'>(clientUserId),
        succeeded,
        recommendationId: optionalEntityId<'recommendation'>(succeeded ? 'recommendation-id' : null),
        wasAlreadyProcessed: false,
        errorCode: succeeded ? null : 'QA.Error',
    };
}

function prepareBulkRecommendation(): void {
    dietologistService.getMyClients.mockReturnValueOnce(of([createClient(), createClient({ userId: entityId<'user'>('client-2') })]));
    dialogService.open.mockReturnValue({ afterClosed: (): Observable<boolean> => of(true) });
    createComponent();
    component['toggleClientSelection'](entityId<'user'>('client-1'), true);
    component['toggleClientSelection'](entityId<'user'>('client-2'), true);
    component['bulkModel'].set({ text: 'QA recommendation' });
}

describe('Dietologist bulk recommendation preview', () => {
    it('reviews recipient identities and sends the exact snapshot despite changes behind the dialog', () => {
        prepareBulkRecommendation();
        const confirmation = new Subject<boolean>();
        dialogService.open.mockReturnValue({ afterClosed: () => confirmation });
        dietologistService.bulkCreateRecommendations.mockReturnValue(
            of({
                idempotencyKey: 'key',
                recipients: [bulkRecipient('client-1', true), bulkRecipient('client-2', true)],
            }),
        );
        component['sendBulkRecommendation']();
        component['sendBulkRecommendation']();
        expect(dialogService.open).toHaveBeenCalledOnce();
        expect(dialogService.open).toHaveBeenCalledWith(BulkRecommendationReviewComponent, {
            preset: 'form',
            data: {
                text: 'QA recommendation',
                recipients: [
                    { id: 'client-1', label: 'Alex Ivanov', email: 'client@example.com' },
                    { id: 'client-2', label: 'Alex Ivanov', email: 'client@example.com' },
                ],
            },
        });
        expect(dietologistService.bulkCreateRecommendations).not.toHaveBeenCalled();
        component['bulkModel'].set({ text: 'Changed draft' });
        component['selectedClientIds'].set(new Set());
        confirmation.next(true);
        confirmation.complete();
        expect(dietologistService.bulkCreateRecommendations).toHaveBeenCalledWith(
            ['client-1', 'client-2'],
            'QA recommendation',
            expect.any(String),
        );
    });

    it('keeps the draft and sends nothing after cancellation', () => {
        prepareBulkRecommendation();
        dialogService.open.mockReturnValue({ afterClosed: () => of(false) });
        component['sendBulkRecommendation']();
        expect(dietologistService.bulkCreateRecommendations).not.toHaveBeenCalled();
        expect(component['bulkModel']().text).toBe('QA recommendation');
        expect(component['selectedClientIds']().size).toBe(2);
    });
});

describe('Dietologist attention action pending state', () => {
    it.each(['success', 'error'] as const)('blocks conflicting attention actions while pending and recovers after %s', outcome => {
        const request = new Subject<void>();
        dietologistService.setAttentionSignalState.mockReturnValueOnce(request);
        createComponent();
        const attentionSignal: AttentionSignal = {
            id: entityId<'attention-signal'>('signal-pending'),
            clientUserId: entityId<'user'>('client-1'),
            clientDisplayName: 'QA client',
            type: 'MaterialWeightChange',
            severity: 'High',
            reason: 'MaterialWeightChange',
            detectedAtUtc: utcInstant('2026-10-02T12:00:00Z'),
            snoozedUntilUtc: null,
        };
        component['attentionSignals'].set([attentionSignal]);

        component['acknowledgeSignal'](attentionSignal);
        fixture.detectChanges();
        const buttons = Array.from((fixture.nativeElement as HTMLElement).querySelectorAll('article button'));
        expect(buttons).toHaveLength(2);
        expect(buttons.every(button => (button as HTMLButtonElement).disabled)).toBe(true);
        component['snoozeSignal'](attentionSignal);
        component['acknowledgeSignal'](attentionSignal);
        expect(dietologistService.setAttentionSignalState).toHaveBeenCalledTimes(1);

        if (outcome === 'success') {
            request.next();
            request.complete();
            expect(component['attentionSignals']()).toEqual([]);
        } else {
            request.error(new Error('failed'));
            fixture.detectChanges();
            expect(component['attentionSignals']()).toEqual([attentionSignal]);
            expect(buttons.every(button => !(button as HTMLButtonElement).disabled)).toBe(true);
            component['snoozeSignal'](attentionSignal);
            expect(dietologistService.setAttentionSignalState).toHaveBeenCalledTimes(2);
            expect(component['attentionSignals']()).toEqual([]);
        }
        expect(component['pendingAttentionIds']().size).toBe(0);
    });
});

describe('DietologistClientsPageComponent', () => {
    it('updates the rendered attention date when the application language changes', async () => {
        createComponent();
        component['attentionSignals'].set([
            {
                id: entityId<'attention-signal'>('signal-1'),
                clientUserId: entityId<'user'>('client-1'),
                clientDisplayName: 'QA client',
                type: 'MaterialWeightChange',
                severity: 'High',
                reason: 'MaterialWeightChange',
                detectedAtUtc: utcInstant('2026-10-02T12:00:00Z'),
                snoozedUntilUtc: null,
            },
        ]);
        const translate = TestBed.inject(TranslateService);

        await firstValueFrom(translate.use('ru'));
        fixture.detectChanges();
        expect((fixture.nativeElement as HTMLElement).querySelector('time')?.textContent).toContain('окт.');

        await firstValueFrom(translate.use('en'));
        fixture.detectChanges();
        expect((fixture.nativeElement as HTMLElement).querySelector('time')?.textContent).toContain('Oct');
        expect((fixture.nativeElement as HTMLElement).querySelector('time')?.getAttribute('datetime')).toBe('2026-10-02T12:00:00Z');
    });

    it('loads client cards on creation', () => {
        createComponent();

        expect(component['loading']()).toBe(false);
        expect(component['clientItems']()[0].title).toBe('Alex Ivanov');
    });

    it('shows a distinct error state when loading clients fails', () => {
        dietologistService.getMyClients.mockReturnValueOnce(throwError(() => new Error('failed')));

        createComponent();

        expect(component['loading']()).toBe(false);
        expect(component['loadError']()).toBe(true);
        expect(component['clientItems']()).toEqual([]);
        expect((fixture.nativeElement as HTMLElement).querySelector('[role="alert"]')).not.toBeNull();
    });

    it('retries loading clients after an error', () => {
        dietologistService.getMyClients
            .mockReturnValueOnce(throwError(() => new Error('failed')))
            .mockReturnValueOnce(of([createClient({ userId: entityId<'user'>('client-retry') })]));
        createComponent();

        component['retryLoad']();
        fixture.detectChanges();

        expect(dietologistService.getMyClients).toHaveBeenCalledTimes(2);
        expect(component['loadError']()).toBe(false);
        expect(component['clientItems']()[0].client.userId).toBe('client-retry');
    });

    it('navigates to selected client dashboard', () => {
        createComponent();

        component['openClient'](createClient({ userId: entityId<'user'>('client-2') }));

        expect(router.navigate).toHaveBeenCalledWith(['/dietologist', 'clients', 'client-2']);
    });

    it('loads attention signals with configured thresholds', () => {
        createComponent();

        expect(dietologistService.getAttentionSignals).toHaveBeenCalledWith({
            inactivityDays: 3,
            calorieDeviationPercent: 25,
            sustainedDays: 3,
            weightChangePercent: 3,
            lookbackDays: 14,
        });
    });
});

describe('Dietologist attention settings validation', () => {
    it.each(['success', 'error'] as const)('keeps the latest settings result after a stale %s', outcome => {
        const initialRequest = new Subject<AttentionSignal[]>();
        const latestRequest = new Subject<AttentionSignal[]>();
        dietologistService.getAttentionSignals.mockReturnValueOnce(initialRequest).mockReturnValueOnce(latestRequest);
        createComponent();
        component['updateAttentionSetting']('inactivityDays', '2');
        component['applyAttentionSettings']();
        const latestSignal: AttentionSignal = {
            id: entityId<'attention-signal'>('latest-signal'),
            clientUserId: entityId<'user'>('client-1'),
            clientDisplayName: 'Latest client signal',
            type: 'DiaryInactivity',
            severity: 'High',
            reason: 'NoRecentDiaryEntries',
            detectedAtUtc: utcInstant('2026-10-02T12:00:00Z'),
            snoozedUntilUtc: null,
        };
        latestRequest.next([latestSignal]);
        latestRequest.complete();

        if (outcome === 'success') {
            initialRequest.next([{ ...latestSignal, clientDisplayName: 'Stale client signal' }]);
            initialRequest.complete();
        } else {
            initialRequest.error(new Error('Stale request failed'));
        }
        fixture.detectChanges();

        expect(component['attentionSignals']()).toEqual([latestSignal]);
        expect(component['attentionLoading']()).toBe(false);
        expect(component['attentionError']()).toBe(false);
        const host = fixture.nativeElement as HTMLElement;
        expect(host.textContent).toContain('Latest client signal');
        expect(host.textContent).not.toContain('Stale client signal');
    });

    it.each(['', '0', '1.5', '31'])('does not request signals for invalid inactivity value %s', value => {
        createComponent();
        dietologistService.getAttentionSignals.mockClear();
        component['updateAttentionSetting']('inactivityDays', value);
        component['applyAttentionSettings']();

        expect(component['invalidAttentionSettings']().has('inactivityDays')).toBe(true);
        expect(dietologistService.getAttentionSignals).not.toHaveBeenCalled();
    });

    it('requires all invalid settings to be corrected before requesting signals', () => {
        createComponent();
        dietologistService.getAttentionSignals.mockClear();
        component['updateAttentionSetting']('calorieDeviationPercent', '101');
        component['updateAttentionSetting']('weightChangePercent', '0.4');
        component['updateAttentionSetting']('calorieDeviationPercent', '100');
        component['applyAttentionSettings']();
        expect(dietologistService.getAttentionSignals).not.toHaveBeenCalled();

        component['updateAttentionSetting']('weightChangePercent', '0.5');
        component['applyAttentionSettings']();
        expect(component['invalidAttentionSettings']().size).toBe(0);
        expect(dietologistService.getAttentionSignals).toHaveBeenCalledWith(
            expect.objectContaining({ calorieDeviationPercent: 100, weightChangePercent: 0.5 }),
        );
    });
});

function createComponent(): void {
    TestBed.configureTestingModule({
        imports: [DietologistClientsPageComponent],
        providers: [
            provideTranslateTesting(),
            { provide: DietologistFacade, useValue: dietologistService },
            { provide: Router, useValue: router },
            {
                provide: FdUiDialogService,
                useValue: dialogService,
            },
            { provide: FdUiToastService, useValue: { success: vi.fn(), error: vi.fn() } },
        ],
    });

    fixture = TestBed.createComponent(DietologistClientsPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
}
