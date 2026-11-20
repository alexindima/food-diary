import { ChangeDetectionStrategy, Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { FdTourService } from 'fd-tour';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { WebmcpScreenRegistry } from 'fd-webmcp';
import { of, Subject, throwError } from 'rxjs';
import { describe, expect, it, type Mock, vi } from 'vitest';

import { provideTranslateTesting } from '../../../testing/translate-testing.module';
import { DEVELOPMENT_SCREENS } from '../../shared/development/development-tools';
import type { AttentionSignal, ClientSummary } from '../../shared/models/dietologist.data';
import { utcInstant } from '../../shared/models/semantics/date-value';
import { entityId } from '../../shared/models/semantics/entity-id';
import { DietologistService } from './api/dietologist.service';
import { dietologistRoutes } from './dietologist.routes';
import { createClient } from './pages/clients/dietologist-clients-lib/dietologist-clients.test-data';

const DIRECTORY = '/dietologist';
const RETURN_PAGE = '/other';
const FIRST_CLIENT = createClient({
    userId: entityId<'user'>('fixture-alex'),
    permissions: { ...createClient().permissions, shareMeals: false },
});
const SECOND_CLIENT = createClient({
    userId: entityId<'user'>('fixture-mira'),
    firstName: 'Mira',
    permissions: FIRST_CLIENT.permissions,
});
const INACTIVITY_DAYS = 5;

const ACKNOWLEDGED: AttentionSignal = {
    id: entityId<'attention-signal'>('fixture-acknowledged'),
    clientUserId: FIRST_CLIENT.userId,
    clientDisplayName: 'Acknowledge this client',
    type: 'DiaryInactivity',
    severity: 'Medium',
    reason: 'NoRecentDiaryEntries',
    detectedAtUtc: utcInstant('2026-05-10T10:10:00Z'),
    snoozedUntilUtc: null,
};
const OTHER_SIGNAL: AttentionSignal = {
    ...ACKNOWLEDGED,
    id: entityId<'attention-signal'>('fixture-other-signal'),
    clientUserId: SECOND_CLIENT.userId,
    clientDisplayName: 'Other pending attention',
};
const NEW_SIGNAL: AttentionSignal = {
    ...OTHER_SIGNAL,
    id: entityId<'attention-signal'>('fixture-new-signal'),
    type: 'MaterialWeightChange',
    reason: 'MaterialWeightChange',
    clientDisplayName: 'Newly loaded attention',
};

@Component({ template: '<h1>Other test destination</h1>', changeDetection: ChangeDetectionStrategy.OnPush })
class OtherDestinationComponent {}

type DirectoryMethod = 'getMyClients' | 'getAttentionSignals' | 'setAttentionSignalState' | 'bulkCreateRecommendations';
type DirectoryApi = { [Method in DirectoryMethod]: Mock<DietologistService[Method]> };
type DirectoryFixture = { api: DirectoryApi; registry: WebmcpScreenRegistry; showSuccess: Mock<FdUiToastService['success']> };

function configureDirectory(): DirectoryFixture {
    const registry = new WebmcpScreenRegistry();
    const showSuccess = vi.fn<FdUiToastService['success']>();
    const api: DirectoryApi = {
        getMyClients: vi.fn<DietologistService['getMyClients']>().mockReturnValue(of([FIRST_CLIENT, SECOND_CLIENT])),
        getAttentionSignals: vi.fn<DietologistService['getAttentionSignals']>().mockReturnValue(of([])),
        setAttentionSignalState: vi.fn<DietologistService['setAttentionSignalState']>(),
        bulkCreateRecommendations: vi.fn<DietologistService['bulkCreateRecommendations']>(),
    };
    TestBed.configureTestingModule({
        providers: [
            provideRouter([
                { path: 'dietologist', children: dietologistRoutes },
                { path: 'other', component: OtherDestinationComponent },
            ]),
            provideTranslateTesting(),
            { provide: DEVELOPMENT_SCREENS, useValue: registry },
            {
                provide: DietologistService,
                useValue: {
                    ...api,
                    getTasksForClient: vi.fn<DietologistService['getTasksForClient']>().mockReturnValue(of([])),
                    getRecommendationsForClient: vi.fn<DietologistService['getRecommendationsForClient']>().mockReturnValue(of([])),
                    searchRecommendationTemplates: vi.fn<DietologistService['searchRecommendationTemplates']>().mockReturnValue(of([])),
                },
            },
            { provide: FdUiToastService, useValue: { success: showSuccess, error: vi.fn() } },
        ],
    });
    return { api, registry, showSuccess };
}

function root(harness: RouterTestingHarness): HTMLElement {
    harness.detectChanges();
    const outlet = harness.routeNativeElement;
    if (outlet === null) {
        throw new Error('Expected the dietologist route to be rendered');
    }
    return outlet;
}

function button(parent: ParentNode, label: string): HTMLButtonElement {
    const control = [...parent.querySelectorAll('button')].find(
        candidate => candidate.textContent.trim() === label || candidate.getAttribute('aria-label') === label,
    );
    if (control === undefined) {
        throw new Error(`Expected the directory action ${label}`);
    }
    return control;
}

function attention(host: HTMLElement): HTMLElement {
    const section = host.querySelector<HTMLElement>('section[aria-labelledby="attention-title"]');
    if (section === null) {
        throw new Error('Expected the attention panel');
    }
    return section;
}

function fillDraft(harness: RouterTestingHarness, text: string): void {
    const control = root(harness).querySelector('textarea');
    if (control === null) {
        throw new Error('Expected the bulk recommendation draft');
    }
    control.value = text;
    control.dispatchEvent(new Event('input', { bubbles: true }));
    harness.detectChanges();
}

function setInactivityFilter(harness: RouterTestingHarness): void {
    const panel = attention(root(harness));
    const summary = panel.querySelector('summary');
    const input = panel.querySelector<HTMLInputElement>('#attention-inactivity');
    if (summary === null || input === null) {
        throw new Error('Expected configurable attention thresholds');
    }
    summary.click();
    input.value = String(INACTIVITY_DAYS);
    input.dispatchEvent(new Event('change', { bubbles: true }));
    button(panel, 'ATTENTION.APPLY').click();
}

function clientRow(harness: RouterTestingHarness, client: ClientSummary): Element | null {
    return root(harness).querySelector(`fd-dietologist-clients-list fd-ui-card[aria-label="${client.firstName} ${client.lastName}"]`);
}

describe('Dietologist registered directory', () => {
    it('publishes only listed clients, gates an unsaved draft, and opens the selected real dashboard', async () => {
        const { api, registry } = configureDirectory();
        const harness = await RouterTestingHarness.create(DIRECTORY);
        expect(registry.read()).toMatchObject({
            kind: 'dietologist-clients',
            dirty: false,
            actions: { openEntityAsync: true, fillDraft: false },
            data: {
                loading: false,
                items: [
                    { id: FIRST_CLIENT.userId, permissions: FIRST_CLIENT.permissions },
                    { id: SECOND_CLIENT.userId, permissions: SECOND_CLIENT.permissions },
                ],
            },
        });
        expect(clientRow(harness, FIRST_CLIENT)).not.toBeNull();
        expect(clientRow(harness, SECOND_CLIENT)).not.toBeNull();
        const open = registry.current()?.openEntityAsync;
        if (open === undefined) {
            throw new Error('Expected the published client navigation action');
        }
        await expect(open('unknown-client')).rejects.toThrow('Client ID is not in the current list');
        expect(TestBed.inject(Router).url).toBe(DIRECTORY);
        fillDraft(harness, 'A local recommendation draft');
        expect(registry.read()).toMatchObject({ dirty: true });
        expect(() => {
            registry.prepareNavigation();
        }).toThrow('Unsaved draft exists');
        expect(api.bulkCreateRecommendations).not.toHaveBeenCalled();

        fillDraft(harness, '');
        registry.prepareNavigation();
        await open(SECOND_CLIENT.userId);
        expect(TestBed.inject(Router).url).toBe(`${DIRECTORY}/clients/${SECOND_CLIENT.userId}`);
        expect(root(harness).querySelector('fd-client-dashboard-header')).not.toBeNull();
        expect(registry.read()).toMatchObject({
            kind: 'client-dashboard',
            data: { clientId: SECOND_CLIENT.userId, permissions: SECOND_CLIENT.permissions },
        });
        await harness.navigateByUrl(RETURN_PAGE);
        expect(registry.read()).toBeNull();
    });

    it('launches the directory tour with the actual toolbar button', async () => {
        configureDirectory();
        const harness = await RouterTestingHarness.create(DIRECTORY);
        const start = vi.spyOn(TestBed.inject(FdTourService), 'start').mockReturnValue(true);
        button(root(harness), 'DIETOLOGIST.CLIENTS.TOUR.START_TOUR').click();
        expect(start).toHaveBeenCalledExactlyOnceWith(expect.objectContaining({ id: 'dietologist-clients' }), { force: true });
    });
});

describe('Dietologist attention panel recovery', () => {
    it('retries a failed attention request without reloading the usable client directory', async () => {
        const { api } = configureDirectory();
        api.getAttentionSignals
            .mockReturnValueOnce(throwError(() => new Error('Attention service unavailable')))
            .mockReturnValueOnce(of([OTHER_SIGNAL]));
        const harness = await RouterTestingHarness.create(DIRECTORY);
        const panel = attention(root(harness));
        expect(panel.querySelector('[role="alert"]:not([hidden])')?.textContent).toContain('ATTENTION.LOAD_ERROR');
        expect(clientRow(harness, FIRST_CLIENT)).not.toBeNull();

        button(panel, 'DIETOLOGIST.CLIENTS.RETRY').click();
        expect(attention(root(harness)).querySelector('[role="alert"]:not([hidden])')).toBeNull();
        expect(panel.textContent).toContain(OTHER_SIGNAL.clientDisplayName);
        expect(api.getAttentionSignals).toHaveBeenCalledTimes(2);
        expect(api.getAttentionSignals.mock.calls[0]).toEqual(api.getAttentionSignals.mock.calls[1]);
        expect(api.getMyClients).toHaveBeenCalledOnce();
        expect(clientRow(harness, FIRST_CLIENT)).not.toBeNull();
    });

    it('keeps an acknowledged signal out of an attention result requested before the change completed', async () => {
        const { api, showSuccess } = configureDirectory();
        const acknowledgement = new Subject<void>();
        const staleRead = new Subject<AttentionSignal[]>();
        const freshRead = new Subject<AttentionSignal[]>();
        api.getAttentionSignals
            .mockReturnValueOnce(of([ACKNOWLEDGED, OTHER_SIGNAL]))
            .mockReturnValueOnce(staleRead)
            .mockReturnValue(freshRead);
        api.setAttentionSignalState.mockReturnValueOnce(acknowledgement);
        const harness = await RouterTestingHarness.create(DIRECTORY);
        const panel = attention(root(harness));
        const row = [...panel.querySelectorAll('article')].find(article => article.textContent.includes('Acknowledge this client'));
        if (row === undefined) {
            throw new Error('Expected the first attention signal');
        }
        button(row, 'ATTENTION.ACKNOWLEDGE').click();
        expect(api.setAttentionSignalState).toHaveBeenCalledExactlyOnceWith(
            expect.objectContaining({ id: ACKNOWLEDGED.id, clientUserId: ACKNOWLEDGED.clientUserId }),
            'Acknowledge',
            null,
        );
        setInactivityFilter(harness);
        expect(staleRead.observed).toBe(true);
        expect(api.getAttentionSignals).toHaveBeenLastCalledWith(expect.objectContaining({ inactivityDays: INACTIVITY_DAYS }));

        acknowledgement.next();
        acknowledgement.complete();
        expect(showSuccess).toHaveBeenCalledExactlyOnceWith('ATTENTION.ACKNOWLEDGE_SUCCESS');
        staleRead.next([ACKNOWLEDGED, OTHER_SIGNAL]);
        staleRead.complete();
        expect(attention(root(harness)).textContent).not.toContain(ACKNOWLEDGED.clientDisplayName);
        expect(freshRead.observed).toBe(true);

        freshRead.next([OTHER_SIGNAL, NEW_SIGNAL]);
        freshRead.complete();
        expect(attention(root(harness)).textContent).toContain(OTHER_SIGNAL.clientDisplayName);
        expect(panel.textContent).toContain(NEW_SIGNAL.clientDisplayName);
        expect(panel.textContent).not.toContain(ACKNOWLEDGED.clientDisplayName);
        expect(api.getAttentionSignals.mock.calls[1]).toEqual(api.getAttentionSignals.mock.calls[2]);
    });
});
