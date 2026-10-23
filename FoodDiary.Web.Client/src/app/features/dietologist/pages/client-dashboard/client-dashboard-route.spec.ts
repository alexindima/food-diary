import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { of, Subject, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import type { ClientTask, DietologistRecommendation } from '../../../../shared/models/dietologist.data';
import { utcInstant } from '../../../../shared/models/semantics/date-value';
import { entityId } from '../../../../shared/models/semantics/entity-id';
import { DietologistFacade } from '../../lib/dietologist.facade';
import type { DietologistDashboardSnapshot } from '../../lib/dietologist-dashboard.data';
import { createClient } from '../clients/dietologist-clients-lib/dietologist-clients.test-data';
import { ClientDashboardComponent } from './client-dashboard';

const api = {
    getMyClients: vi.fn(),
    getClientDashboard: vi.fn(),
    getClientGoals: vi.fn(),
    getRecommendationsForClient: vi.fn(),
    getTasksForClient: vi.fn(),
    searchRecommendationTemplates: vi.fn(),
    createRecommendation: vi.fn(),
    createTask: vi.fn(),
    disconnectClient: vi.fn(),
};
const toast = { success: vi.fn(), error: vi.fn(), info: vi.fn() };
const dialog = { open: vi.fn() };
const CLIENT_A_CALORIES = 100;
const CLIENT_B_CALORIES = 200;
@Component({ template: '' })
class ClientListStubComponent {}
const permissions = {
    shareProfile: true,
    shareMeals: true,
    shareStatistics: true,
    shareWeight: true,
    shareWaist: true,
    shareGoals: true,
    shareHydration: true,
    shareFasting: true,
};

beforeEach(async () => {
    vi.clearAllMocks();
    api.getMyClients.mockReturnValue(
        of([
            createClient({ userId: entityId<'user'>('A'), firstName: 'Client A', permissions }),
            createClient({ userId: entityId<'user'>('B'), firstName: 'Client B', permissions }),
        ]),
    );
    api.getClientDashboard.mockImplementation((id: string) => of(snapshot(id === 'A' ? CLIENT_A_CALORIES : CLIENT_B_CALORIES)));
    api.getClientGoals.mockImplementation((id: string) => of({ id }));
    api.getRecommendationsForClient.mockImplementation((id: string) => of([recommendation(id)]));
    api.getTasksForClient.mockReturnValue(of([]));
    api.searchRecommendationTemplates.mockReturnValue(of([]));
    api.disconnectClient.mockReturnValue(of(void 0));
    await TestBed.configureTestingModule({
        providers: [
            provideRouter([
                { path: 'dietologist/clients/:clientId', component: ClientDashboardComponent },
                { path: 'dietologist', component: ClientListStubComponent },
            ]),
            provideTranslateTesting(),
            { provide: DietologistFacade, useValue: api },
            { provide: FdUiToastService, useValue: toast },
            { provide: FdUiDialogService, useValue: dialog },
        ],
    })
        .overrideComponent(ClientDashboardComponent, { set: { template: '' } })
        .compileComponents();
});

describe('ClientDashboardComponent reused route identity', () => {
    it('switches A to B in the same component, cancels old details and clears dependent drafts', async () => {
        const pendingA = new Subject<DietologistRecommendation[]>();
        api.getRecommendationsForClient.mockReturnValueOnce(pendingA);
        const harness = await RouterTestingHarness.create();
        const component = await harness.navigateByUrl('/dietologist/clients/A?recommendationId=A', ClientDashboardComponent);
        component['recommendationModel'].set({ text: 'A draft', templateName: 'A template' });
        component['taskModel'].set({ title: 'A task', details: 'A details', dueDate: '2026-10-22' });

        const reused = await harness.navigateByUrl('/dietologist/clients/B?recommendationId=B', ClientDashboardComponent);

        expect(reused).toBe(component);
        expect(pendingA.observed).toBe(false);
        expect(component['client']()?.userId).toBe('B');
        expect(component['dashboard']()?.statistics.totalCalories).toBe(CLIENT_B_CALORIES);
        expect(component['goals']()?.id).toBe('B');
        expect(component['recommendations']().map(item => item.id)).toEqual(['B']);
        expect(component['openDiscussionId']()).toBe('B');
        expect(component['recommendationModel']()).toEqual({ text: '', templateName: '' });
        expect(component['taskModel']()).toEqual({ title: '', details: '', dueDate: '' });
        pendingA.next([recommendation('A')]);
        pendingA.complete();
        expect(component['recommendations']().map(item => item.id)).toEqual(['B']);
    });

    it('updates discussion on query-only navigation without reloading or clearing the same-client draft', async () => {
        api.getRecommendationsForClient.mockReturnValueOnce(of([recommendation('first'), recommendation('second')]));
        const harness = await RouterTestingHarness.create();
        const component = await harness.navigateByUrl('/dietologist/clients/A?recommendationId=first', ClientDashboardComponent);
        component['recommendationModel'].set({ text: 'Keep same-client draft', templateName: '' });
        const reused = await harness.navigateByUrl('/dietologist/clients/A?recommendationId=second', ClientDashboardComponent);
        expect(reused).toBe(component);
        expect(component['openDiscussionId']()).toBe('second');
        expect(api.getMyClients).toHaveBeenCalledOnce();
        expect(component['recommendationModel']().text).toBe('Keep same-client draft');
        await harness.navigateByUrl('/dietologist/clients/A', ClientDashboardComponent);
        expect(component['openDiscussionId']()).toBeNull();
    });

    it('can load another client after a failed route load', async () => {
        api.getMyClients.mockReturnValueOnce(throwError(() => new Error('offline')));
        const harness = await RouterTestingHarness.create();
        const component = await harness.navigateByUrl('/dietologist/clients/A', ClientDashboardComponent);
        expect(component['error']()).not.toBeNull();
        await harness.navigateByUrl('/dietologist/clients/B', ClientDashboardComponent);
        expect(component['client']()?.userId).toBe('B');
        expect(component['error']()).toBeNull();
    });

    it('clears the previous client when the next route ID is not accessible', async () => {
        const harness = await RouterTestingHarness.create();
        const component = await harness.navigateByUrl('/dietologist/clients/A', ClientDashboardComponent);
        await harness.navigateByUrl('/dietologist/clients/missing', ClientDashboardComponent);
        expect(component['client']()).toBeNull();
        expect(component['dashboard']()).toBeNull();
        expect(component['goals']()).toBeNull();
        expect(component['recommendations']()).toEqual([]);
        expect(component['tasks']()).toEqual([]);
        expect(component['loading']()).toBe(false);
    });
});

describe('ClientDashboardComponent pending client lifetime', () => {
    it.each(['recommendation', 'task'] as const)('does not apply an old pending %s result or unlock a new owner save', async action => {
        const pending = new Subject<DietologistRecommendation | ClientTask>();
        api.createRecommendation.mockReturnValue(pending);
        api.createTask.mockReturnValue(pending);
        const harness = await RouterTestingHarness.create();
        const component = await harness.navigateByUrl('/dietologist/clients/A', ClientDashboardComponent);
        component['recommendationModel'].set({ text: 'A recommendation', templateName: '' });
        component['taskModel'].set({ title: 'A task', details: '', dueDate: '' });
        const submitted = action === 'recommendation' ? component['submitRecommendationAsync']() : component['submitTaskAsync']();
        await harness.navigateByUrl('/dietologist/clients/B', ClientDashboardComponent);
        component['recommendationModel'].set({ text: 'B draft', templateName: '' });
        component['taskModel'].set({ title: 'B task', details: '', dueDate: '' });
        const cancelled = !pending.observed;
        pending.next(action === 'recommendation' ? recommendation('late-A') : lateTask());
        pending.complete();
        await submitted;
        expect(cancelled).toBe(true);
        expect(component['recommendationModel']().text).toBe('B draft');
        expect(component['taskModel']().title).toBe('B task');
        expect(component['recommendations']().map(item => item.id)).toEqual(['B']);
        expect(toast.success).not.toHaveBeenCalled();
        expect(toast.error).not.toHaveBeenCalled();
    });

    it('ignores an old client disconnect confirmation after navigating to another client', async () => {
        const confirmed = new Subject<boolean>();
        dialog.open.mockReturnValue({ afterClosed: () => confirmed, close: vi.fn() });
        const harness = await RouterTestingHarness.create();
        const component = await harness.navigateByUrl('/dietologist/clients/A', ClientDashboardComponent);
        component['disconnectClient']();
        await harness.navigateByUrl('/dietologist/clients/B', ClientDashboardComponent);
        confirmed.next(true);
        expect(api.disconnectClient).not.toHaveBeenCalled();
        expect(component['client']()?.userId).toBe('B');
    });
});

function recommendation(id: string): DietologistRecommendation {
    return {
        id: entityId<'recommendation'>(id),
        dietologistUserId: entityId<'user'>('doctor'),
        dietologistFirstName: null,
        dietologistLastName: null,
        text: id,
        isRead: false,
        createdAtUtc: utcInstant('2026-10-22T00:00:00Z'),
        readAtUtc: null,
    };
}

function lateTask(): ClientTask {
    return {
        id: entityId<'client-task'>('late-A'),
        dietologistUserId: entityId<'user'>('doctor'),
        clientUserId: entityId<'user'>('A'),
        title: 'A task',
        details: null,
        dueAtUtc: null,
        status: 'Open',
        isOverdue: false,
        createdAtUtc: utcInstant('2026-10-22T00:00:00Z'),
        statusChangedAtUtc: null,
    };
}

function snapshot(calories: number): DietologistDashboardSnapshot {
    return {
        date: '2026-10-22',
        dateTo: '2026-10-22',
        dailyGoal: 2000,
        weeklyCalorieGoal: 14000,
        statistics: { totalCalories: calories, averageProteins: 0, averageFats: 0, averageCarbs: 0, averageFiber: 0 },
        weeklyCalories: [],
        weight: { latest: null, previous: null, desiredWeightKg: null },
        waist: { latest: null, previous: null, desiredWaistCm: null },
        meals: { items: [], total: 0 },
    };
}
