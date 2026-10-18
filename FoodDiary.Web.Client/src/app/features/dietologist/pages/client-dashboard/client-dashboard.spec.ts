import type { ComponentFixture } from '@angular/core/testing';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import type { Observable } from 'rxjs';
import { of, Subject, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import type { ClientTask, RecommendationTemplate } from '../../../../shared/models/dietologist.data';
import { utcInstant } from '../../../../shared/models/semantics/date-value';
import { entityId } from '../../../../shared/models/semantics/entity-id';
import { DietologistFacade } from '../../lib/dietologist.facade';
import type { DietologistDashboardSnapshot } from '../../lib/dietologist-dashboard.data';
import { createClient } from '../clients/dietologist-clients-lib/dietologist-clients.test-data';
import { ClientDashboardComponent } from './client-dashboard';

let fixture: ComponentFixture<ClientDashboardComponent>;
let component: ClientDashboardComponent;
const EXPECTED_METRIC_TILE_COUNT = 4;
const PAGED_MEAL_TOTAL = 3;
const EXPECTED_DAILY_CALORIE_TARGET = 1800;
const SELECTED_YEAR = 2026;
const SELECTED_DAY = 22;
const PERIOD_START_DAY = 16;
const MAY_MONTH_INDEX = 4;
const OVERLONG_TEMPLATE_NAME_LENGTH = 121;
const DEADLINE_HOUR = 23;
const DEADLINE_MINUTE = 59;
const DEADLINE_SECOND = 59;
const DEADLINE_MILLISECOND = 999;
let dietologistService: {
    getMyClients: ReturnType<typeof vi.fn>;
    getClientDashboard: ReturnType<typeof vi.fn>;
    getClientGoals: ReturnType<typeof vi.fn>;
    getRecommendationsForClient: ReturnType<typeof vi.fn>;
    getTasksForClient: ReturnType<typeof vi.fn>;
    createTask: ReturnType<typeof vi.fn>;
    cancelTask: ReturnType<typeof vi.fn>;
    searchRecommendationTemplates: ReturnType<typeof vi.fn>;
    createRecommendationTemplate: ReturnType<typeof vi.fn>;
    archiveRecommendationTemplate: ReturnType<typeof vi.fn>;
    createRecommendation: ReturnType<typeof vi.fn>;
    disconnectClient: ReturnType<typeof vi.fn>;
};
let router: { navigate: ReturnType<typeof vi.fn> };
let toastService: { success: ReturnType<typeof vi.fn>; error: ReturnType<typeof vi.fn>; info: ReturnType<typeof vi.fn> };
let dialogService: { open: ReturnType<typeof vi.fn> };

beforeEach(() => {
    dietologistService = {
        getMyClients: vi.fn(() => of([createClient({ userId: entityId<'user'>('client-1') })])),
        getClientDashboard: vi.fn(() => of(createDashboardSnapshot())),
        getClientGoals: vi.fn(() => of({ id: 'client-1', email: 'client@example.com', dailyCalorieTarget: 1800 })),
        getRecommendationsForClient: vi.fn(() => of([])),
        getTasksForClient: vi.fn(() => of([])),
        createTask: vi.fn(),
        cancelTask: vi.fn(),
        searchRecommendationTemplates: vi.fn(() => of([])),
        createRecommendationTemplate: vi.fn(),
        archiveRecommendationTemplate: vi.fn(),
        createRecommendation: vi.fn(() =>
            of({
                id: entityId<'recommendation'>('rec-1'),
                dietologistUserId: entityId<'user'>('diet-1'),
                dietologistFirstName: null,
                dietologistLastName: null,
                text: 'Add protein',
                isRead: false,
                createdAtUtc: utcInstant('2026-05-23T00:00:00Z'),
                readAtUtc: null,
            }),
        ),
        disconnectClient: vi.fn(() => of(void 0)),
    };
    router = {
        navigate: vi.fn().mockResolvedValue(true),
    };
    toastService = {
        success: vi.fn(),
        error: vi.fn(),
        info: vi.fn(),
    };
    dialogService = {
        open: vi.fn(() => ({ afterClosed: (): Observable<boolean> => of(true) })),
    };
});

describe('ClientDashboardComponent', () => {
    registerLoadingTests();
    registerDateValidationTests();
    registerActionTests();
});

describe('ClientDashboardComponent remaining meals', () => {
    it('loads remaining rows once, deduplicates IDs and preserves the period statistics', async () => {
        const { initial, next } = prepareMealPaging();
        const pending = new Subject<DietologistDashboardSnapshot>();
        dietologistService.getClientDashboard.mockReturnValueOnce(pending);
        const host = fixture.nativeElement as HTMLElement;
        const button = host.querySelector<HTMLButtonElement>('fd-client-dashboard-meals-card button');
        if (button === null) {
            throw new Error('Expected remaining meals action');
        }

        button.click();
        button.click();
        fixture.detectChanges();

        expect(dietologistService.getClientDashboard).toHaveBeenCalledTimes(2);
        expect(dietologistService.getClientDashboard).toHaveBeenLastCalledWith('client-1', expect.objectContaining({ page: 2 }));
        expect(button.disabled).toBe(true);
        expect(component['dashboard']()?.meals.items).toEqual(initial.meals.items);
        expect(component['readDevelopmentContext']()).toMatchObject({
            meals: {
                available: true,
                total: PAGED_MEAL_TOTAL,
                loadedCount: 1,
                loadedPage: 1,
                hasNext: true,
                loadingMore: true,
            },
        });

        pending.next({ ...next(['meal-1', 'meal-2', 'meal-3']), statistics: { ...initial.statistics, totalCalories: 9999 } });
        pending.complete();
        await vi.waitFor(() => {
            expect(component['loadingMoreMeals']()).toBe(false);
        });
        fixture.detectChanges();

        expect(component['dashboard']()?.meals.items.map(meal => meal.id)).toEqual(['meal-1', 'meal-2', 'meal-3']);
        expect(component['dashboard']()?.statistics.totalCalories).toBe(initial.statistics.totalCalories);
        expect(host.querySelectorAll('fd-client-dashboard-meals-card article')).toHaveLength(PAGED_MEAL_TOTAL);
        expect(host.querySelector('fd-client-dashboard-meals-card button')).toBeNull();
        expect(component['readDevelopmentContext']()).toMatchObject({
            meals: {
                loadedCount: PAGED_MEAL_TOTAL,
                loadedPage: 2,
                hasNext: false,
                loadingMore: false,
                items: [{ id: 'meal-1' }, { id: 'meal-2' }, { id: 'meal-3' }],
            },
        });
    });

    it('keeps loaded rows after a failure and retries the same next page', async () => {
        const { initial, next } = prepareMealPaging();
        dietologistService.getClientDashboard
            .mockReturnValueOnce(throwError(() => new Error('Connection failed')))
            .mockReturnValueOnce(of(next(['meal-2', 'meal-3'])));

        await component['loadMoreMealsAsync']();
        fixture.detectChanges();

        expect(component['dashboard']()).toEqual(initial);
        expect(component['moreMealsError']()).toBe(true);
        expect(component['readDevelopmentContext']()).toMatchObject({ meals: { loadedPage: 1, loadedCount: 1, loadMoreError: true } });
        expect((fixture.nativeElement as HTMLElement).querySelector('fd-client-dashboard-meals-card [role="alert"]')).not.toBeNull();

        await component['loadMoreMealsAsync']();

        const requests = dietologistService.getClientDashboard.mock.calls as Array<Parameters<DietologistFacade['getClientDashboard']>>;
        expect(requests.slice(1).map(([, query]) => query.page)).toEqual([2, 2]);
        expect(component['dashboard']()?.meals.items).toHaveLength(PAGED_MEAL_TOTAL);
        expect(component['moreMealsError']()).toBe(false);
    });
});

describe('ClientDashboardComponent meal page transitions', () => {
    it.each(['success', 'failure'])('ignores late page %s after changing the period', async outcome => {
        const { next } = prepareMealPaging();
        const pending = new Subject<DietologistDashboardSnapshot>();
        dietologistService.getClientDashboard.mockReturnValueOnce(pending).mockReturnValueOnce(of(next(['new-period-meal'])));
        const older = component['loadMoreMealsAsync']();

        component['showPreviousPeriod']();
        await vi.waitFor(() => {
            expect(component['dashboard']()?.meals.items[0].id).toBe('new-period-meal');
        });

        if (outcome === 'success') {
            pending.next(next(['old-period-meal']));
            pending.complete();
        } else {
            pending.error(new Error('Late failure'));
        }
        await older;

        expect(component['dashboard']()?.meals.items.map(meal => meal.id)).toEqual(['new-period-meal']);
        expect(component['moreMealsError']()).toBe(false);
        expect(component['loadingMoreMeals']()).toBe(false);
        dietologistService.getClientDashboard.mockReturnValueOnce(of(next(['new-period-second-meal'])));
        await component['loadMoreMealsAsync']();
        expect(dietologistService.getClientDashboard).toHaveBeenLastCalledWith('client-1', expect.objectContaining({ page: 2 }));
    });

    it('cancels a pending page cleanly when leaving the dashboard', async () => {
        prepareMealPaging();
        const pending = new Subject<DietologistDashboardSnapshot>();
        dietologistService.getClientDashboard.mockReturnValueOnce(pending);
        const loading = component['loadMoreMealsAsync']();

        fixture.destroy();

        await expect(loading).resolves.toBeUndefined();
        expect(pending.observed).toBe(false);
    });

    it('hides meal rows when sharing is disabled or the selected period is reloading', async () => {
        const { next } = prepareMealPaging();
        component['client'].update(client =>
            client === null ? null : { ...client, permissions: { ...client.permissions, shareMeals: false } },
        );
        expect(component['readDevelopmentContext']()).toMatchObject({
            meals: {
                permissionGranted: false,
                available: false,
                items: [],
                total: null,
                loadedCount: 0,
                loadedPage: null,
            },
        });
        component['client'].update(client =>
            client === null ? null : { ...client, permissions: { ...client.permissions, shareMeals: true } },
        );
        const pending = new Subject<DietologistDashboardSnapshot>();
        dietologistService.getClientDashboard.mockReturnValueOnce(pending);
        component['showPreviousPeriod']();
        expect(component['readDevelopmentContext']()).toMatchObject({ meals: { loading: true, available: false, items: [], total: null } });
        pending.next(next(['new-period-meal']));
        pending.complete();
        await vi.waitFor(() => {
            expect(component['detailsLoading']()).toBe(false);
        });
        expect(component['readDevelopmentContext']()).toMatchObject({
            meals: {
                loading: false,
                available: true,
                loadedPage: 1,
                items: [{ id: 'new-period-meal' }],
            },
        });
    });
});

function prepareMealPaging(): {
    initial: DietologistDashboardSnapshot;
    next: (ids: string[]) => DietologistDashboardSnapshot;
} {
    createComponent('client-1');
    const snapshot = component['dashboard']();
    if (snapshot === null) {
        throw new Error('Expected initial dashboard');
    }
    const meal = snapshot.meals.items[0];
    const initial = { ...snapshot, meals: { items: [meal], total: PAGED_MEAL_TOTAL } };
    component['dashboard'].set(initial);
    fixture.detectChanges();
    return {
        initial,
        next: ids => ({ ...snapshot, meals: { items: ids.map(id => ({ ...meal, id: entityId<'meal'>(id) })), total: PAGED_MEAL_TOTAL } }),
    };
}

describe('ClientDashboardComponent task deadlines', () => {
    it.each(['2026-10-02', '2026-03-29', '2026-10-25'])('keeps the whole selected due day available: %s', async dueDate => {
        createComponent('client-1');
        dietologistService.createTask.mockReturnValue(of({ id: 'task-1' }));
        component['taskModel'].set({ title: 'Daily task', details: '', dueDate });

        await component['submitTaskAsync']();

        const [year, month, day] = dueDate.split('-').map(Number);
        expect(dietologistService.createTask).toHaveBeenCalledWith('client-1', {
            title: 'Daily task',
            details: null,
            dueAtUtc: utcInstant(
                new Date(year, month - 1, day, DEADLINE_HOUR, DEADLINE_MINUTE, DEADLINE_SECOND, DEADLINE_MILLISECOND).toISOString(),
            ),
        });
    });

    it('preserves an optional absent deadline', async () => {
        createComponent('client-1');
        dietologistService.createTask.mockReturnValue(of({ id: 'task-1' }));
        component['taskModel'].set({ title: 'Anytime task', details: '', dueDate: '' });

        await component['submitTaskAsync']();

        expect(dietologistService.createTask).toHaveBeenCalledWith('client-1', {
            title: 'Anytime task',
            details: null,
            dueAtUtc: null,
        });
    });
});

describe('ClientDashboardComponent initial dashboard races', () => {
    it.each(['success', 'error'])('keeps the selected period after stale initial %s', async outcome => {
        const { initialRequest, periodRequest, latestSnapshot, reload } = createPendingInitialDashboardRequests();
        periodRequest.next(latestSnapshot);
        periodRequest.complete();
        await reload;

        if (outcome === 'error') {
            initialRequest.error(new Error('stale initial failure'));
        } else {
            initialRequest.next(createDashboardSnapshot());
            initialRequest.complete();
        }

        expect(component['dashboard']()?.date).toBe(latestSnapshot.date);
        expect(component['detailsLoading']()).toBe(false);
        expect(component['sectionLoadError']()).toBeNull();
        expect(component['goals']()?.dailyCalorieTarget).toBe(EXPECTED_DAILY_CALORIE_TARGET);
    });

    it('keeps the current period loading when the initial request finishes first', async () => {
        const { initialRequest, periodRequest, latestSnapshot, reload } = createPendingInitialDashboardRequests();
        initialRequest.next(createDashboardSnapshot());
        initialRequest.complete();

        expect(component['dashboard']()).toBeNull();
        expect(component['detailsLoading']()).toBe(true);

        periodRequest.next(latestSnapshot);
        periodRequest.complete();
        await reload;

        expect(component['dashboard']()?.date).toBe(latestSnapshot.date);
        expect(component['detailsLoading']()).toBe(false);
    });
});

describe('ClientDashboardComponent writes during initial loading', () => {
    it.each([false, true])('preserves local writes and loaded history, including duplicate IDs: %s', async duplicate => {
        const initialRequest = new Subject<Record<string, unknown>>();
        const createdTask: ClientTask = {
            id: entityId<'client-task'>('new-task'),
            dietologistUserId: entityId<'user'>('diet-1'),
            clientUserId: entityId<'user'>('client-1'),
            title: 'New task',
            details: null,
            dueAtUtc: null,
            status: 'Open',
            isOverdue: false,
            createdAtUtc: utcInstant('2026-05-23T00:00:00Z'),
            statusChangedAtUtc: null,
        };
        const cancelledTask: ClientTask = { ...createdTask, status: 'Cancelled' };
        const template: RecommendationTemplate = {
            id: entityId<'recommendation-template'>('new-template'),
            name: 'New template',
            text: 'Add protein',
            isArchived: false,
            createdAtUtc: createdTask.createdAtUtc,
            modifiedAtUtc: null,
        };
        const recommendation = {
            id: entityId<'recommendation'>('rec-1'),
            dietologistUserId: createdTask.dietologistUserId,
            dietologistFirstName: null,
            dietologistLastName: null,
            text: 'Add protein',
            isRead: false,
            createdAtUtc: createdTask.createdAtUtc,
            readAtUtc: null,
        };
        dietologistService.getClientDashboard.mockReturnValueOnce(initialRequest);
        dietologistService.getTasksForClient.mockReturnValueOnce(
            of([{ ...createdTask, id: entityId<'client-task'>('old-task') }, ...(duplicate ? [createdTask] : [])]),
        );
        dietologistService.getRecommendationsForClient.mockReturnValueOnce(
            of([{ ...recommendation, id: entityId<'recommendation'>('old-rec') }, ...(duplicate ? [recommendation] : [])]),
        );
        dietologistService.searchRecommendationTemplates.mockReturnValueOnce(
            of([{ ...template, id: entityId<'recommendation-template'>('old-template') }, ...(duplicate ? [template] : [])]),
        );
        dietologistService.createTask.mockReturnValueOnce(of(createdTask));
        dietologistService.cancelTask.mockReturnValueOnce(of(cancelledTask));
        dietologistService.createRecommendationTemplate.mockReturnValueOnce(of(template));
        createComponent('client-1', true);
        component['taskModel'].set({ title: 'New task', details: '', dueDate: '' });
        await component['submitTaskAsync']();
        component['cancelTask'](createdTask);
        component['recommendationModel'].set({ text: 'Add protein', templateName: 'New template' });
        component['createRecommendationTemplate']();
        await component['submitRecommendationAsync']();

        initialRequest.next(createDashboardSnapshot());
        initialRequest.complete();

        expect(component['tasks']()).toEqual([cancelledTask, expect.objectContaining({ id: 'old-task' })]);
        expect(component['recommendations']()).toEqual([recommendation, expect.objectContaining({ id: 'old-rec' })]);
        expect(component['recommendationTemplates']()).toEqual([template, expect.objectContaining({ id: 'old-template' })]);
        expect(component['detailsLoading']()).toBe(false);
    });
});

function createPendingInitialDashboardRequests(): {
    initialRequest: Subject<Record<string, unknown>>;
    periodRequest: Subject<Record<string, unknown>>;
    latestSnapshot: Record<string, unknown> & { date: string };
    reload: Promise<void>;
} {
    const initialRequest = new Subject<Record<string, unknown>>();
    const periodRequest = new Subject<Record<string, unknown>>();
    dietologistService.getClientDashboard.mockReturnValueOnce(initialRequest).mockReturnValueOnce(periodRequest);
    dietologistService.getMyClients.mockReturnValueOnce(
        of([createClient({ userId: entityId<'user'>('client-1'), permissions: { ...createClient().permissions, shareGoals: true } })]),
    );
    createComponent('client-1');
    component['dateFilterModel'].set({ dateFrom: '2026-05-22', dateTo: '2026-05-22' });
    const reload = component['applyDateFilterAsync']();
    return { initialRequest, periodRequest, latestSnapshot: { ...createDashboardSnapshot(), date: '2026-05-22T00:00:00Z' }, reload };
}

function registerLoadingTests(): void {
    it('loads selected client by route id', () => {
        createComponent('client-1');

        expect(component['loading']()).toBe(false);
        expect(component['clientTitle']()).toBe('Alex Ivanov');
        expect(component['profileChips']()).toContain('180 cm');
        expect(component['visibleSections']()).toHaveLength(2);
    });

    it('sets empty client when route id does not match', () => {
        createComponent('missing-client');

        expect(component['loading']()).toBe(false);
        expect(component['client']()).toBeNull();
        expect((fixture.nativeElement as HTMLElement).querySelector('h1')?.textContent).toContain('DIETOLOGIST.CLIENT_DASHBOARD.TITLE');
    });

    it('stops loading on request error', () => {
        dietologistService.getMyClients.mockReturnValueOnce(throwError(() => new Error('failed')));

        createComponent('client-1');

        expect(component['loading']()).toBe(false);
        expect(component['client']()).toBeNull();
    });

    it('navigates back to clients list', () => {
        createComponent('client-1');

        component['goBack']();

        expect(router.navigate).toHaveBeenCalledWith(['/dietologist']);
    });

    it('loads shared dashboard and goals when permissions allow it', () => {
        dietologistService.getMyClients.mockReturnValueOnce(
            of([
                createClient({
                    userId: entityId<'user'>('client-1'),
                    permissions: { ...createClient().permissions, shareStatistics: true, shareGoals: true },
                }),
            ]),
        );

        createComponent('client-1');

        expect(dietologistService.getClientDashboard).toHaveBeenCalledWith('client-1', expect.objectContaining({ trendDays: 14 }));
        expect(dietologistService.getClientGoals).toHaveBeenCalledWith('client-1');
        expect(component['nutritionTiles']()).toHaveLength(EXPECTED_METRIC_TILE_COUNT);
        expect(component['goalTiles']()).toHaveLength(EXPECTED_METRIC_TILE_COUNT);
    });

    it('hides period filter when only profile and goals are shared', () => {
        dietologistService.getMyClients.mockReturnValueOnce(
            of([
                createClient({
                    userId: entityId<'user'>('client-1'),
                    permissions: {
                        ...createClient().permissions,
                        shareMeals: false,
                        shareGoals: true,
                    },
                }),
            ]),
        );

        createComponent('client-1');

        expect(component['hasAnyPermission']()).toBe(true);
        expect(component['hasPeriodFilterPermission']()).toBe(false);
        expect(dietologistService.getClientDashboard).not.toHaveBeenCalled();
        expect(dietologistService.getClientGoals).toHaveBeenCalledWith('client-1');
    });

    it('loads meal details when meal sharing is enabled', () => {
        createComponent('client-1');

        expect(dietologistService.getClientDashboard).toHaveBeenCalledWith('client-1', expect.objectContaining({ trendDays: 14 }));
        expect(component['nutritionTiles']()).toEqual([]);
        expect(component['mealItems']()[0]).toEqual(
            expect.objectContaining({ id: 'meal-1', title: 'MEAL_TYPES.LUNCH', calories: '640 kcal' }),
        );
        expect(component['bodyTiles']().map(tile => tile.value)).toEqual(['1']);
    });
}

function registerDateValidationTests(): void {
    it('shows a reversed date range error without reloading and recovers when corrected', () => {
        createComponent('client-1');
        component['dateFilterModel'].set({ dateFrom: '2026-05-23', dateTo: '2026-05-22' });
        fixture.detectChanges();

        component['applyDateFilter']();

        expect(component['dateFilterForm']().invalid()).toBe(true);
        expect(component['dateFilterRangeInvalid']()).toBe(true);
        expect(dietologistService.getClientDashboard).toHaveBeenCalledTimes(1);
        const root = fixture.nativeElement as HTMLElement;
        expect(root.querySelector('.fd-ui-date-input__error')?.textContent).toContain(
            'DIETOLOGIST.CLIENT_DASHBOARD.DATE_FILTER.INVALID_RANGE',
        );
        expect(root.querySelector<HTMLButtonElement>('.client-dashboard__date-apply button')?.disabled).toBe(true);

        component['dateFilterModel'].set({ dateFrom: '2026-05-22', dateTo: '2026-05-23' });
        fixture.detectChanges();
        component['applyDateFilter']();

        expect(component['dateFilterForm']().invalid()).toBe(false);
        expect(component['dateFilterRangeInvalid']()).toBe(false);
        expect(root.querySelector('.fd-ui-date-input__error')).toBeNull();
        expect(dietologistService.getClientDashboard).toHaveBeenCalledTimes(2);
    });

    it.each([
        ['2026-05-22', '2026-05-22'],
        ['2028-02-29', '2028-02-29'],
        ['2026-12-31', '2027-01-01'],
        ['2026-03-07', '2026-03-09'],
    ])('accepts inclusive calendar report boundaries %s to %s', (dateFrom, dateTo) => {
        createComponent('client-1');
        component['dateFilterModel'].set({ dateFrom, dateTo });

        expect(component['dateFilterForm']().invalid()).toBe(false);
        component['applyDateFilter']();

        expect(component['selectedDateFrom']()).toBe(dateFrom);
        expect(component['selectedDateTo']()).toBe(dateTo);
        expect(dietologistService.getClientDashboard).toHaveBeenCalledTimes(2);
    });
}

function registerActionTests(): void {
    it('prevents native date filter submit when applying period', async () => {
        createComponent('client-1');

        component['dateFilterModel'].set({ dateFrom: '2026-05-16', dateTo: '2026-05-22' });
        fixture.detectChanges();

        const form = (fixture.nativeElement as HTMLElement).querySelector('.client-dashboard__date-filter');
        expect(form).not.toBeNull();

        const submitEvent = new Event('submit', { bubbles: true, cancelable: true });
        const wasNotCancelled = form?.dispatchEvent(submitEvent);
        await fixture.whenStable();

        expect(wasNotCancelled).toBe(false);
        expect(submitEvent.defaultPrevented).toBe(true);
        expect(dietologistService.getClientDashboard).toHaveBeenCalledTimes(2);
    });

    it('reloads dashboard for selected period', () => {
        createComponent('client-1');

        component['dateFilterModel'].set({ dateFrom: '2026-05-16', dateTo: '2026-05-22' });
        component['applyDateFilter']();

        expect(dietologistService.getClientDashboard).toHaveBeenCalledTimes(2);
        expect(getLastDashboardPeriod()).toEqual(
            expect.objectContaining({
                dateFrom: new Date(SELECTED_YEAR, MAY_MONTH_INDEX, PERIOD_START_DAY),
                dateTo: new Date(SELECTED_YEAR, MAY_MONTH_INDEX, SELECTED_DAY),
            }),
        );
    });

    it('moves dashboard dates with period navigation', () => {
        createComponent('client-1');

        component['dateFilterModel'].set({ dateFrom: '2026-05-17', dateTo: '2026-05-23' });
        component['applyDateFilter']();
        component['showPreviousPeriod']();

        expect(component['selectedDateFrom']()).toBe('2026-05-10');
        expect(component['selectedDateTo']()).toBe('2026-05-16');
    });

    it('keeps the latest dashboard response when period requests finish out of order', verifyLatestDashboardResponseWinsAsync);

    it('shows section load warning when optional details fail', () => {
        dietologistService.getClientDashboard.mockReturnValueOnce(throwError(() => new Error('failed')));

        createComponent('client-1');

        expect(component['sectionLoadError']()).toBe('DIETOLOGIST.CLIENT_DASHBOARD.PARTIAL_LOAD_ERROR');
        expect(component['dashboard']()).toBeNull();
    });

    it('sends recommendation and prepends it to the list', async () => {
        createComponent('client-1', true);

        component['recommendationModel'].set({ text: 'Add protein', templateName: '' });
        component['submitRecommendation']();

        expect(dietologistService.createRecommendation).toHaveBeenCalledWith('client-1', { text: 'Add protein' });
        await vi.waitFor(() => {
            expect(component['recommendations']()[0]?.id).toBe('rec-1');
        });
        expect(toastService.success).toHaveBeenCalled();
    });

    it('prevents native recommendation submit when sending recommendation', async () => {
        createComponent('client-1', true);

        component['recommendationModel'].set({ text: 'Add protein', templateName: '' });
        fixture.detectChanges();

        const form = (fixture.nativeElement as HTMLElement).querySelector('.client-dashboard__recommendation-form');
        expect(form).not.toBeNull();

        const submitEvent = new Event('submit', { bubbles: true, cancelable: true });
        const wasNotCancelled = form?.dispatchEvent(submitEvent);
        await fixture.whenStable();

        expect(wasNotCancelled).toBe(false);
        expect(submitEvent.defaultPrevented).toBe(true);
        expect(dietologistService.createRecommendation).toHaveBeenCalledWith('client-1', { text: 'Add protein' });
    });

    it('disconnects selected client after confirmation', () => {
        createComponent('client-1');

        component['disconnectClient']();

        expect(dialogService.open).toHaveBeenCalled();
        expect(dietologistService.disconnectClient).toHaveBeenCalledWith('client-1');
        expect(router.navigate).toHaveBeenCalledWith(['/dietologist']);
    });

    it('keeps selected client when disconnect confirmation is cancelled', () => {
        dialogService.open.mockReturnValueOnce({ afterClosed: () => of(false) });
        createComponent('client-1');

        component['disconnectClient']();

        expect(dietologistService.disconnectClient).not.toHaveBeenCalled();
        expect(router.navigate).not.toHaveBeenCalledWith(['/dietologist']);
    });
}

describe('ClientDashboardComponent template limits', () => {
    it.each([
        { nameLength: 120, textLength: 2000, invalid: false },
        { nameLength: 121, textLength: 2000, invalid: true },
        { nameLength: 120, textLength: 2001, invalid: true },
    ])('validates $nameLength name characters and $textLength text characters', ({ nameLength, textLength, invalid }) => {
        createComponent('client-1');
        component['recommendationModel'].set({ text: 'x'.repeat(textLength), templateName: 'x'.repeat(nameLength) });
        fixture.detectChanges();

        expect(component['templateInvalid']()).toBe(invalid);
        if (invalid) {
            component['createRecommendationTemplate']();
            expect(dietologistService.createRecommendationTemplate).not.toHaveBeenCalled();
        }
    });

    it('submits a recommendation independently of the optional template name limit', async () => {
        createComponent('client-1', true);
        component['recommendationModel'].set({ text: 'Add protein', templateName: 'x'.repeat(OVERLONG_TEMPLATE_NAME_LENGTH) });
        fixture.detectChanges();
        const form = (fixture.nativeElement as HTMLElement).querySelector('.client-dashboard__recommendation-form');
        form?.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
        await fixture.whenStable();

        expect(dietologistService.createRecommendation).toHaveBeenCalledWith('client-1', { text: 'Add protein' });
    });
});

describe('ClientDashboardComponent recommendation permissions', () => {
    it.each([
        'shareProfile',
        'shareMeals',
        'shareStatistics',
        'shareWeight',
        'shareWaist',
        'shareGoals',
        'shareHydration',
        'shareFasting',
    ] as const)('allows recommendations when %s is private and other data is shared', async permission => {
        createComponent('client-1', true);
        component['client'].update(client =>
            client === null
                ? null
                : {
                      ...client,
                      permissions: { ...client.permissions, [permission]: false },
                  },
        );
        component['recommendationModel'].set({ text: 'Add protein', templateName: '' });
        fixture.detectChanges();

        const host = fixture.nativeElement as HTMLElement;
        const submit = host.querySelector<HTMLButtonElement>('.client-dashboard__recommendation-form button[type="submit"]');
        expect(submit?.disabled).toBe(false);
        expect(host.querySelector<HTMLParagraphElement>('.client-dashboard__recommendation-form p')?.hidden).toBe(true);
        await component['submitRecommendationAsync']();
        expect(dietologistService.createRecommendation).toHaveBeenCalledWith('client-1', { text: 'Add protein' });
    });
});

function createComponent(clientId: string, fullAccess = false): void {
    if (fullAccess) {
        dietologistService.getMyClients.mockReturnValueOnce(
            of([
                createClient({
                    userId: entityId<'user'>(clientId),
                    permissions: {
                        shareProfile: true,
                        shareMeals: true,
                        shareStatistics: true,
                        shareWeight: true,
                        shareWaist: true,
                        shareGoals: true,
                        shareHydration: true,
                        shareFasting: true,
                    },
                }),
            ]),
        );
    }
    TestBed.configureTestingModule({
        imports: [ClientDashboardComponent],
        providers: [
            provideTranslateTesting(),
            { provide: DietologistFacade, useValue: dietologistService },
            { provide: Router, useValue: router },
            { provide: FdUiToastService, useValue: toastService },
            { provide: FdUiDialogService, useValue: dialogService },
            {
                provide: ActivatedRoute,
                useValue: {
                    snapshot: {
                        paramMap: convertToParamMap({ clientId }),
                        queryParamMap: convertToParamMap({}),
                    },
                },
            },
        ],
    });

    const translate = TestBed.inject(TranslateService);
    translate.setTranslation('en', {
        GENERAL: {
            UNITS: { CM: 'cm', KG: 'kg', LB: 'lb', IN: 'in', FT: 'ft', G: 'g', ML: 'ml', KCAL: 'kcal', H: 'h' },
            NUTRIENTS: { PROTEIN: 'Protein', FAT: 'Fats', CARB: 'Carbs' },
        },
    });
    translate.use('en');

    fixture = TestBed.createComponent(ClientDashboardComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
}

async function verifyLatestDashboardResponseWinsAsync(): Promise<void> {
    const olderRequest = new Subject<Record<string, unknown>>();
    const latestRequest = new Subject<Record<string, unknown>>();
    dietologistService.getClientDashboard
        .mockReturnValueOnce(of(createDashboardSnapshot()))
        .mockReturnValueOnce(olderRequest)
        .mockReturnValueOnce(latestRequest);
    createComponent('client-1');

    component['dateFilterModel'].set({ dateFrom: '2026-05-16', dateTo: '2026-05-22' });
    component['applyDateFilter']();
    component['showPreviousPeriod']();

    const latestSnapshot = { ...createDashboardSnapshot(), date: '2026-05-16T00:00:00Z' };
    latestRequest.next(latestSnapshot);
    latestRequest.complete();
    await vi.waitFor(() => {
        expect(component['dashboard']()?.date).toBe(latestSnapshot.date);
        expect(component['detailsLoading']()).toBe(false);
    });

    olderRequest.next({ ...createDashboardSnapshot(), date: '2026-05-22T00:00:00Z' });
    olderRequest.complete();
    await vi.waitFor(() => {
        expect(component['dashboard']()?.date).toBe(latestSnapshot.date);
    });
}

function createDashboardSnapshot(): Record<string, unknown> {
    return {
        date: '2026-05-23T00:00:00Z',
        dateTo: '2026-05-23T00:00:00Z',
        dailyGoal: 2000,
        weeklyCalorieGoal: 14000,
        statistics: {
            totalCalories: 1200,
            averageProteins: 80,
            averageFats: 45,
            averageCarbs: 150,
            averageFiber: 20,
        },
        weeklyCalories: [],
        weight: {
            latest: { date: '2026-05-23T00:00:00Z', weight: 72 },
            previous: { date: '2026-05-22T00:00:00Z', weight: 73 },
            desired: 70,
        },
        waist: {
            latest: { date: '2026-05-23T00:00:00Z', circumference: 82 },
            previous: { date: '2026-05-22T00:00:00Z', circumference: 83 },
            desired: 80,
        },
        meals: {
            items: [
                {
                    id: entityId<'meal'>('meal-1'),
                    date: utcInstant('2026-05-23T12:30:00Z'),
                    mealType: 'Lunch',
                    comment: null,
                    totalCalories: 640,
                    totalProteins: 42,
                    totalFats: 22,
                    totalCarbs: 64,
                    totalFiber: 8,
                    totalAlcohol: 0,
                    isNutritionAutoCalculated: true,
                    items: [{ id: 'item-1', mealId: 'meal-1', amount: 150, sourceType: 'Product', product: { name: 'Chicken' } }],
                },
            ],
            total: 1,
        },
        hydration: { dateUtc: '2026-05-23T00:00:00Z', totalMl: 1200, goalMl: 2000 },
        currentFastingSession: {
            id: entityId<'fasting-session'>('fast-1'),
            startedAtUtc: utcInstant('2026-05-23T00:00:00Z'),
            endedAtUtc: null,
            initialPlannedDurationHours: 16,
            addedDurationHours: 0,
            plannedDurationHours: 16,
            protocol: 'Fast16Eat8',
            planType: 'Intermittent',
            occurrenceKind: 'FastingWindow',
            cyclicFastDays: null,
            cyclicEatDays: null,
            cyclicEatDayFastHours: null,
            cyclicEatDayEatingWindowHours: null,
            cyclicPhaseDayNumber: null,
            cyclicPhaseDayTotal: null,
            isCompleted: false,
            status: 'Active',
            notes: null,
            checkInAtUtc: null,
            hungerLevel: null,
            energyLevel: null,
            moodLevel: null,
            symptoms: [],
            checkInNotes: null,
            checkIns: [],
        },
    };
}

function getLastDashboardPeriod(): { dateFrom: Date; dateTo?: Date } | undefined {
    const calls = dietologistService.getClientDashboard.mock.calls;
    const lastCall = calls.at(-1) as [string, { dateFrom: Date; dateTo?: Date }] | undefined;
    return lastCall?.[1];
}
