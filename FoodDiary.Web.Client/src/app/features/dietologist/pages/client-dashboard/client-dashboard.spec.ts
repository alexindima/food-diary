import type { ComponentFixture } from '@angular/core/testing';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { FdTourService } from 'fd-tour';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import type { Observable } from 'rxjs';
import { of, Subject, throwError } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { nutritionMealFixture, nutritionSnapshotFixture } from '../../../../../testing/nutrition-details.fixtures';
import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { fastingSessionFromSdk } from '../../../../shared/api/sdk/fasting-sdk.mapper';
import { DEVELOPMENT_SCREENS, type DevelopmentScreen } from '../../../../shared/development/development-tools';
import type {
    ClientSummary,
    ClientTask,
    DietologistRecommendation,
    RecommendationTemplate,
} from '../../../../shared/models/dietologist.data';
import type { Meal } from '../../../../shared/models/meal.data';
import { utcInstant } from '../../../../shared/models/semantics/date-value';
import { entityId } from '../../../../shared/models/semantics/entity-id';
import { RecommendationsFacade } from '../../../recommendations/lib/recommendations.facade';
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

describe('ClientDashboardComponent rendered period controls', () => {
    const endDay = 23;
    const firstWeekDay = 17;
    const firstTwoWeekDay = 10;
    const startPreviousMonthDay = 24;
    const aprilMonth = 3;
    const nextWeekStarts = 24;
    const nextWeekEnds = 30;
    const midday = 12;

    it.each([
        ['LAST_7', MAY_MONTH_INDEX, firstWeekDay],
        ['LAST_14', MAY_MONTH_INDEX, firstTwoWeekDay],
        ['LAST_30', aprilMonth, startPreviousMonthDay],
    ] as const)('requests the inclusive calendar period from the %s preset', async (action, month, day) => {
        vi.useFakeTimers({ toFake: ['Date'] });
        try {
            vi.setSystemTime(new Date(SELECTED_YEAR, MAY_MONTH_INDEX, endDay, midday));
            createComponent('client-1');
            dashboardButton(`DIETOLOGIST.CLIENT_DASHBOARD.DATE_FILTER.${action}`).click();
            await fixture.whenStable();

            expect(dietologistService.getClientDashboard).toHaveBeenCalledTimes(2);
            expect(getLastDashboardPeriod()).toMatchObject({
                dateFrom: new Date(SELECTED_YEAR, month, day),
                dateTo: new Date(SELECTED_YEAR, MAY_MONTH_INDEX, endDay),
            });
        } finally {
            vi.useRealTimers();
        }
    });

    it('moves the displayed seven-day period forward, then restores today as a single day', async () => {
        vi.useFakeTimers({ toFake: ['Date'] });
        try {
            vi.setSystemTime(new Date(SELECTED_YEAR, MAY_MONTH_INDEX, endDay, midday));
            createComponent('client-1');
            dashboardButton('DIETOLOGIST.CLIENT_DASHBOARD.DATE_FILTER.LAST_7').click();
            await fixture.whenStable();
            dashboardButton('DIETOLOGIST.CLIENT_DASHBOARD.DATE_FILTER.NEXT_PERIOD').click();
            await fixture.whenStable();

            expect(getLastDashboardPeriod()).toMatchObject({
                dateFrom: new Date(SELECTED_YEAR, MAY_MONTH_INDEX, nextWeekStarts),
                dateTo: new Date(SELECTED_YEAR, MAY_MONTH_INDEX, nextWeekEnds),
            });
            dashboardButton('DIETOLOGIST.CLIENT_DASHBOARD.DATE_FILTER.TODAY').click();
            await fixture.whenStable();
            expect(getLastDashboardPeriod()).toMatchObject({
                dateFrom: new Date(SELECTED_YEAR, MAY_MONTH_INDEX, endDay),
                dateTo: new Date(SELECTED_YEAR, MAY_MONTH_INDEX, endDay),
            });
        } finally {
            vi.useRealTimers();
        }
    });
});

describe('ClientDashboardComponent rendered recommendation actions', () => {
    it('uses template text and keeps it editable after an archive failure and retry', () => {
        const template: RecommendationTemplate = {
            id: entityId<'recommendation-template'>('protein-guide'),
            name: 'Protein guide',
            text: 'Add beans to your next lunch.',
            isArchived: false,
            createdAtUtc: utcInstant('2026-05-23T00:00:00Z'),
            modifiedAtUtc: null,
        };
        dietologistService.searchRecommendationTemplates.mockReturnValueOnce(of([template]));
        dietologistService.archiveRecommendationTemplate
            .mockReturnValueOnce(throwError(() => new Error('archive unavailable')))
            .mockReturnValueOnce(of(void 0));
        createComponent('client-1', true);
        const host = fixture.nativeElement as HTMLElement;
        dashboardButton(template.name).click();
        fixture.detectChanges();
        const draft = host.querySelector<HTMLTextAreaElement>('.client-dashboard__recommendation-form textarea');
        expect(draft?.value).toBe(template.text);
        expect(dietologistService.createRecommendation).not.toHaveBeenCalled();

        dashboardButton('RECOMMENDATION_TEMPLATES.ARCHIVE_SHORT').click();
        fixture.detectChanges();
        expect(host.textContent).toContain(template.name);
        expect(toastService.error).toHaveBeenCalledExactlyOnceWith('RECOMMENDATION_TEMPLATES.ARCHIVE_ERROR');
        dashboardButton('RECOMMENDATION_TEMPLATES.ARCHIVE_SHORT').click();
        fixture.detectChanges();

        expect(dietologistService.archiveRecommendationTemplate).toHaveBeenCalledTimes(2);
        expect(dietologistService.archiveRecommendationTemplate).toHaveBeenLastCalledWith(template.id);
        expect(host.textContent).toContain('RECOMMENDATION_TEMPLATES.EMPTY');
        expect(draft?.value).toBe(template.text);
        expect(draft?.disabled).toBe(false);
    });
});

describe('ClientDashboardComponent task cancellation', () => {
    it('retries task cancellation without changing other task rows', () => {
        const task: ClientTask = {
            id: entityId<'client-task'>('check-lunch'),
            dietologistUserId: entityId<'user'>('diet-1'),
            clientUserId: entityId<'user'>('client-1'),
            title: 'Check lunch',
            details: 'Track protein portions',
            dueAtUtc: null,
            status: 'Open',
            isOverdue: false,
            createdAtUtc: utcInstant('2026-05-23T00:00:00Z'),
            statusChangedAtUtc: null,
        };
        const otherTask = { ...task, id: entityId<'client-task'>('check-breakfast'), title: 'Check breakfast' };
        const pending = new Subject<ClientTask>();
        dietologistService.getTasksForClient.mockReturnValueOnce(of([task, otherTask]));
        dietologistService.cancelTask.mockReturnValueOnce(pending).mockReturnValueOnce(of({ ...task, status: 'Cancelled' }));
        createComponent('client-1');
        const host = fixture.nativeElement as HTMLElement;
        const cancel = dashboardButton('CLIENT_TASKS.CANCEL');
        cancel.click();
        expect(cancel.disabled).toBe(false);
        cancel.click();
        fixture.detectChanges();
        expect(cancel.disabled).toBe(true);
        expect(dietologistService.cancelTask).toHaveBeenCalledExactlyOnceWith(task.id);

        pending.error(new Error('temporarily unavailable'));
        fixture.detectChanges();
        expect(cancel.closest('article')?.textContent).toContain('CLIENT_TASKS.STATUS.OPEN');
        expect(toastService.error).toHaveBeenCalledExactlyOnceWith('CLIENT_TASKS.CHANGE_ERROR');
        expect(cancel.disabled).toBe(false);
        cancel.click();
        fixture.detectChanges();

        expect(host.textContent).toContain('CLIENT_TASKS.STATUS.CANCELLED');
        expect(cancel.isConnected).toBe(false);
        expect(dietologistService.cancelTask.mock.calls).toEqual([[task.id], [task.id]]);
        const remainingCancel = dashboardButton('CLIENT_TASKS.CANCEL');
        expect(remainingCancel.closest('article')?.textContent).toContain(otherTask.title);
        expect(remainingCancel.closest('article')?.textContent).toContain('CLIENT_TASKS.STATUS.OPEN');
        expect(remainingCancel.disabled).toBe(false);
    });
});

describe('Client dashboard recommendation template requests', () => {
    it('allows independent archives but sends only one request per pending template', () => {
        const first = recommendationTemplate('first', 'After training');
        const second = recommendationTemplate('second', 'Before lunch');
        const firstAttempt = new Subject<void>();
        const secondAttempt = new Subject<void>();
        dietologistService.searchRecommendationTemplates.mockReturnValueOnce(of([first, second]));
        dietologistService.archiveRecommendationTemplate
            .mockReturnValueOnce(firstAttempt)
            .mockReturnValueOnce(secondAttempt)
            .mockReturnValueOnce(of(void 0));
        createComponent('client-1', true);
        const host = fixture.nativeElement as HTMLElement;
        const firstArchive = archiveButton(host, first.name);
        const secondArchive = archiveButton(host, second.name);

        firstArchive.click();
        firstArchive.click();
        fixture.detectChanges();
        expect(dietologistService.archiveRecommendationTemplate).toHaveBeenCalledExactlyOnceWith(first.id);
        expect(firstArchive.disabled).toBe(true);
        expect(secondArchive.disabled).toBe(false);
        secondArchive.click();
        fixture.detectChanges();
        expect(secondArchive.disabled).toBe(true);

        firstAttempt.error(new Error('Archive unavailable'));
        fixture.detectChanges();
        expect(firstArchive.disabled).toBe(false);
        expect(secondArchive.disabled).toBe(true);
        expect(toastService.error).toHaveBeenCalledExactlyOnceWith('RECOMMENDATION_TEMPLATES.ARCHIVE_ERROR');

        secondAttempt.next();
        secondAttempt.complete();
        fixture.detectChanges();
        expect(secondArchive.isConnected).toBe(false);
        firstArchive.click();
        fixture.detectChanges();
        expect(firstArchive.isConnected).toBe(false);
        expect(dietologistService.archiveRecommendationTemplate.mock.calls).toEqual([[first.id], [second.id], [first.id]]);
    });

    it('retains the native form after a rejected save and places the successful retry in name order', () => {
        const older = recommendationTemplate('older', 'Zinc foods');
        const created = recommendationTemplate('created', 'A balanced breakfast');
        dietologistService.searchRecommendationTemplates.mockReturnValueOnce(of([older]));
        dietologistService.createRecommendationTemplate
            .mockReturnValueOnce(throwError(() => new Error('Template unavailable')))
            .mockReturnValueOnce(of(created));
        createComponent('client-1', true);
        const host = fixture.nativeElement as HTMLElement;
        const draft = host.querySelector('.client-dashboard__recommendation-form textarea');
        if (!(draft instanceof HTMLTextAreaElement)) {
            throw new Error('Expected the recommendation text field');
        }
        const name = templateNameField();
        draft.value = `  ${created.text}  `;
        draft.dispatchEvent(new Event('input', { bubbles: true }));
        name.value = `  ${created.name}  `;
        name.dispatchEvent(new Event('input', { bubbles: true }));
        fixture.detectChanges();

        const save = dashboardButton('RECOMMENDATION_TEMPLATES.SAVE');
        save.click();
        fixture.detectChanges();
        expect(toastService.error).toHaveBeenCalledExactlyOnceWith('RECOMMENDATION_TEMPLATES.SAVE_ERROR');
        expect(draft.value).toBe(`  ${created.text}  `);
        expect(name.value).toBe(`  ${created.name}  `);
        expect(host.querySelectorAll('.client-dashboard__template')).toHaveLength(1);

        save.click();
        fixture.detectChanges();
        expect(dietologistService.createRecommendationTemplate).toHaveBeenLastCalledWith({ name: created.name, text: created.text });
        expect(Array.from(host.querySelectorAll('.client-dashboard__template-name'), node => node.textContent.trim())).toEqual([
            created.name,
            older.name,
        ]);
        expect(draft.value).toBe(`  ${created.text}  `);
        expect(name.value).toBe('');
    });
});

function recommendationTemplate(key: string, name: string): RecommendationTemplate {
    return {
        id: entityId<'recommendation-template'>(key),
        name,
        text: `${name} suggested foods`,
        isArchived: false,
        createdAtUtc: utcInstant('2026-05-23T00:00:00Z'),
        modifiedAtUtc: null,
    };
}

function archiveButton(root: HTMLElement, name: string): HTMLButtonElement {
    const row = Array.from(root.querySelectorAll('.client-dashboard__template')).find(
        item => item.querySelector('.client-dashboard__template-name')?.textContent.trim() === name,
    );
    const button = row?.querySelector('button[aria-label^="RECOMMENDATION_TEMPLATES.ARCHIVE"]');
    if (!(button instanceof HTMLButtonElement)) {
        throw new Error(`Expected the archive button for ${name}`);
    }
    return button;
}

describe('Client dashboard recommendation template response order', () => {
    it.each(['list first', 'save first'] as const)('shows the new template once when the %s response arrives', order => {
        const older = recommendationTemplate('older', 'Zinc foods');
        const created = recommendationTemplate('created', 'A balanced breakfast');
        const initialRead = new Subject<RecommendationTemplate[]>();
        const pendingSave = new Subject<RecommendationTemplate>();
        dietologistService.searchRecommendationTemplates.mockReturnValueOnce(initialRead);
        dietologistService.createRecommendationTemplate.mockReturnValueOnce(pendingSave);
        createComponent('client-1', true);

        const host = fixture.nativeElement as HTMLElement;
        const draft = host.querySelector('.client-dashboard__recommendation-form textarea');
        if (!(draft instanceof HTMLTextAreaElement)) {
            throw new Error('Expected the recommendation text field');
        }
        const name = templateNameField();
        draft.value = created.text;
        draft.dispatchEvent(new Event('input', { bubbles: true }));
        name.value = created.name;
        name.dispatchEvent(new Event('input', { bubbles: true }));
        fixture.detectChanges();
        dashboardButton('RECOMMENDATION_TEMPLATES.SAVE').click();
        expect(dietologistService.createRecommendationTemplate).toHaveBeenCalledExactlyOnceWith({
            name: created.name,
            text: created.text,
        });
        expect(initialRead.observed).toBe(true);
        expect(pendingSave.observed).toBe(true);

        const resolveRead = (): void => {
            initialRead.next([created, older]);
            initialRead.complete();
            fixture.detectChanges();
        };
        const resolveSave = (): void => {
            pendingSave.next(created);
            pendingSave.complete();
            fixture.detectChanges();
        };
        if (order === 'list first') {
            resolveRead();
            resolveSave();
        } else {
            resolveSave();
            resolveRead();
        }

        expect(Array.from(host.querySelectorAll('.client-dashboard__template-name'), item => item.textContent.trim())).toEqual([
            created.name,
            older.name,
        ]);
        expect(name.value).toBe('');
    });
});

describe('ClientDashboardComponent rendered help and discussions', () => {
    it('opens one recommendation discussion at a time and closes the active discussion when selected again', () => {
        const createdAtUtc = utcInstant('2026-05-23T08:00:00Z');
        const first: DietologistRecommendation = {
            id: entityId<'recommendation'>('protein'),
            dietologistUserId: entityId<'user'>('diet-1'),
            dietologistFirstName: 'Alex',
            dietologistLastName: null,
            text: 'Add protein at lunch',
            isRead: false,
            createdAtUtc,
            readAtUtc: null,
        };
        const second = { ...first, id: entityId<'recommendation'>('hydration'), text: 'Add water after walking' };
        const getComments = vi.fn(() => of([]));
        TestBed.configureTestingModule({ providers: [{ provide: RecommendationsFacade, useValue: { getComments } }] });
        dietologistService.getRecommendationsForClient.mockReturnValueOnce(of([first, second]));
        createComponent('client-1', true);
        const host = fixture.nativeElement as HTMLElement;
        const controls = host.querySelectorAll<HTMLButtonElement>('.client-dashboard__recommendation-list button');

        controls[0].click();
        fixture.detectChanges();
        expect(getComments).toHaveBeenCalledExactlyOnceWith(first.id);
        expect(host.querySelectorAll('fd-recommendation-thread')).toHaveLength(1);
        expect(controls[0].getAttribute('aria-expanded')).toBe('true');

        controls[1].click();
        fixture.detectChanges();
        expect(getComments).toHaveBeenLastCalledWith(second.id);
        expect(host.querySelectorAll('fd-recommendation-thread')).toHaveLength(1);
        expect(controls[0].getAttribute('aria-expanded')).toBe('false');
        expect(controls[1].getAttribute('aria-expanded')).toBe('true');

        controls[1].click();
        fixture.detectChanges();
        expect(host.querySelector('fd-recommendation-thread')).toBeNull();
        expect(getComments).toHaveBeenCalledTimes(2);
    });

    it('opens the client dashboard tour through the header help button', () => {
        createComponent('client-1');
        const start = vi.spyOn(TestBed.inject(FdTourService), 'start').mockReturnValue(true);
        const host = fixture.nativeElement as HTMLElement;
        host.querySelector<HTMLButtonElement>('[data-tour-id="client-dashboard-tour-help"] button')?.click();

        expect(start).toHaveBeenCalledExactlyOnceWith(expect.objectContaining({ id: 'dietologist-client-dashboard' }), { force: true });
    });
});

describe('ClientDashboardComponent registered development screen', () => {
    it('keeps development actions draft-only and protects a task during native form submission', async () => {
        const response = new Subject<ClientTask>();
        dietologistService.createTask.mockReturnValueOnce(response);
        const { screen, detach } = createDevelopmentDashboard();
        const host = fixture.nativeElement as HTMLElement;
        expect(screen.kind).toBe('client-dashboard');
        expect(screen.isDirty?.()).toBe(false);

        screen.fillDraft?.({ type: 'recommendation', text: 'Eat a varied lunch' });
        fixture.detectChanges();
        expect(host.querySelector<HTMLTextAreaElement>('.client-dashboard__recommendation-form textarea')?.value).toBe(
            'Eat a varied lunch',
        );
        expect(dietologistService.createRecommendation).not.toHaveBeenCalled();
        expect(screen.isDirty?.()).toBe(true);
        screen.clearDraft?.();
        expect(screen.isDirty?.()).toBe(false);

        screen.fillDraft?.({ type: 'task', title: 'Log a meal', details: 'Include portions', dueDate: '' });
        fixture.detectChanges();
        expect(dietologistService.createTask).not.toHaveBeenCalled();
        dashboardButton('CLIENT_TASKS.FORM.CREATE').click();
        await vi.waitFor(() => {
            expect(dietologistService.createTask).toHaveBeenCalledExactlyOnceWith('client-1', {
                title: 'Log a meal',
                details: 'Include portions',
                dueAtUtc: null,
            });
        });
        expect(() => screen.clearDraft?.()).toThrow('A save is running.');
        expect(() => screen.fillDraft?.({ type: 'recommendation', text: 'Another recommendation' })).toThrow('Client is not ready.');
        response.error(new Error('task service unavailable'));
        await fixture.whenStable();
        expect(screen.read()).toMatchObject({ task: { title: 'Log a meal', details: 'Include portions' } });

        screen.clearDraft?.();
        expect(screen.isDirty?.()).toBe(false);
        fixture.destroy();
        expect(detach).toHaveBeenCalledOnce();
    });

    it('honors meal sharing and validates the current timezone and calendar order before requesting a period', async () => {
        dietologistService.getMyClients.mockReturnValueOnce(
            of([
                createClient({
                    userId: entityId<'user'>('client-1'),
                    permissions: { ...createClient().permissions, shareMeals: false, shareStatistics: true },
                }),
            ]),
        );
        const { screen } = createDevelopmentDashboard(false);
        expect(screen.read()).toMatchObject({ meals: { available: false, permissionGranted: false, items: [] } });
        const timeZone = new Intl.DateTimeFormat().resolvedOptions().timeZone;
        const dates = { dateFrom: '2026-05-16', dateTo: '2026-05-22' };

        expect(() => screen.setFilters?.({ ...dates, timeZone: '' })).toThrow('Use the current browser timezone');
        expect(() => screen.setFilters?.({ dateFrom: dates.dateTo, dateTo: dates.dateFrom, timeZone })).toThrow('ordered calendar range');
        expect(dietologistService.getClientDashboard).toHaveBeenCalledTimes(1);

        screen.setFilters?.({ ...dates, timeZone });
        await fixture.whenStable();
        expect(getLastDashboardPeriod()).toMatchObject({
            dateFrom: new Date(SELECTED_YEAR, MAY_MONTH_INDEX, PERIOD_START_DAY),
            dateTo: new Date(SELECTED_YEAR, MAY_MONTH_INDEX, SELECTED_DAY),
        });
        expect(screen.read()).toMatchObject({ ...dates, meals: { available: false, permissionGranted: false, items: [] } });
    });
});

describe('ClientDashboardComponent development period and due date', () => {
    it('keeps development period changes free of dashboard requests while loading or sharing only a profile', async () => {
        const client = createClient({ permissions: { ...createClient().permissions, shareMeals: false } });
        const directory = new Subject<ClientSummary[]>();
        dietologistService.getMyClients.mockReturnValueOnce(directory);
        const { screen } = createDevelopmentDashboard(false);
        const host = fixture.nativeElement as HTMLElement;
        const noMeals = { available: false, permissionGranted: false, items: [] };
        const timeZone = new Intl.DateTimeFormat().resolvedOptions().timeZone;
        const pendingPeriod = { dateFrom: '2026-05-16', dateTo: '2026-05-22' };

        expect(screen.read()).toMatchObject({ clientId: null, permissions: null, loading: true, meals: noMeals });
        screen.setFilters?.({ ...pendingPeriod, timeZone });
        expect(screen.read()).toMatchObject({ ...pendingPeriod, loading: true, meals: noMeals });
        expect(dietologistService.getClientDashboard).not.toHaveBeenCalled();

        directory.next([client]);
        directory.complete();
        await fixture.whenStable();
        fixture.detectChanges();
        expect(host.textContent).toContain(client.email);
        expect(screen.read()).toMatchObject({
            ...pendingPeriod,
            clientId: client.userId,
            permissions: client.permissions,
            loading: false,
            meals: noMeals,
        });

        const nextPeriod = { dateFrom: '2026-05-23', dateTo: '2026-05-29' };
        screen.setFilters?.({ ...nextPeriod, timeZone });
        await fixture.whenStable();
        expect(screen.read()).toMatchObject({ ...nextPeriod, loading: false, meals: noMeals });
        expect(dietologistService.getClientDashboard).not.toHaveBeenCalled();
    });

    it('validates the registered task due day and only submits it through the rendered form', async () => {
        dietologistService.createTask.mockReturnValueOnce(new Subject<ClientTask>());
        const { screen } = createDevelopmentDashboard();
        const draft = { type: 'task', title: 'Plan dinner', details: 'Include vegetables', dueDate: '2026-10-25' };

        expect(() => screen.fillDraft?.({ ...draft, dueDate: '2026-02-30' })).toThrow('Expected a valid calendar date');
        expect(screen.isDirty?.()).toBe(false);
        screen.fillDraft?.(draft);
        fixture.detectChanges();
        expect(screen.read()).toMatchObject({ task: { title: draft.title, details: draft.details, dueDate: draft.dueDate } });
        expect(dietologistService.createTask).not.toHaveBeenCalled();

        dashboardButton('CLIENT_TASKS.FORM.CREATE').click();
        await vi.waitFor(() => {
            expect(dietologistService.createTask).toHaveBeenCalledExactlyOnceWith('client-1', {
                title: draft.title,
                details: draft.details,
                dueAtUtc: new Date('2026-10-25T23:59:59.999').toISOString(),
            });
        });
    });
});

describe('ClientDashboardComponent pending template draft ownership', () => {
    it('keeps the draft during a native template selection before repaint and unlocks it after failure', () => {
        const pending = new Subject<RecommendationTemplate>();
        const saved = recommendationTemplate('saved', 'Existing template');
        dietologistService.searchRecommendationTemplates.mockReturnValueOnce(of([saved]));
        dietologistService.createRecommendationTemplate.mockReturnValueOnce(pending);
        const { screen } = createDevelopmentDashboard();
        const draft = { name: 'Protein guide', text: 'Add beans to your next lunch.' };
        screen.fillDraft?.({ type: 'recommendation', text: draft.text });
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;
        const text = host.querySelector<HTMLTextAreaElement>('.client-dashboard__recommendation-form textarea');
        const name = templateNameField();
        name.value = draft.name;
        name.dispatchEvent(new Event('input', { bubbles: true }));
        fixture.detectChanges();
        const selectTemplate = dashboardButton(saved.name);

        dashboardButton('RECOMMENDATION_TEMPLATES.SAVE').click();
        expect(selectTemplate.disabled).toBe(false);
        selectTemplate.click();
        fixture.detectChanges();

        expect(dietologistService.createRecommendationTemplate).toHaveBeenCalledExactlyOnceWith(draft);
        expect(selectTemplate.disabled).toBe(true);
        expect(text?.value).toBe(draft.text);

        pending.error(new Error('Template save unavailable'));
        fixture.detectChanges();
        expect(selectTemplate.disabled).toBe(false);
        selectTemplate.click();
        fixture.detectChanges();
        expect(text?.value).toBe(saved.text);
    });

    it.each(['clear', 'replace'] as const)('rejects a development %s while the native template save is pending', attempt => {
        const pending = new Subject<RecommendationTemplate>();
        dietologistService.createRecommendationTemplate.mockReturnValueOnce(pending);
        const { screen } = createDevelopmentDashboard();
        const draft = { name: 'Protein guide', text: 'Add beans to your next lunch.' };
        screen.fillDraft?.({ type: 'recommendation', text: draft.text });
        fixture.detectChanges();
        const name = templateNameField();
        name.value = draft.name;
        name.dispatchEvent(new Event('input', { bubbles: true }));
        fixture.detectChanges();
        dashboardButton('RECOMMENDATION_TEMPLATES.SAVE').click();
        fixture.detectChanges();

        expect(dietologistService.createRecommendationTemplate).toHaveBeenCalledExactlyOnceWith(draft);
        expect(name.disabled).toBe(true);
        const mutate =
            attempt === 'clear'
                ? (): void => {
                      screen.clearDraft?.();
                  }
                : (): void => {
                      screen.fillDraft?.({ type: 'recommendation', text: 'A different draft' });
                  };
        expect(mutate).toThrow(attempt === 'clear' ? 'A save is running.' : 'Client is not ready.');

        pending.error(new Error('Template save unavailable'));
        fixture.detectChanges();

        expect(name.disabled).toBe(false);
        expect(name.value).toBe(draft.name);
        expect(screen.read()).toMatchObject({ recommendation: { text: draft.text } });
    });
});

describe('ClientDashboardComponent rendered template validation', () => {
    it('focuses the required template name without sending or deleting the recommendation draft', () => {
        const { screen } = createDevelopmentDashboard();
        const text = 'Choose a high-fibre snack';
        screen.fillDraft?.({ type: 'recommendation', text });
        fixture.detectChanges();

        dashboardButton('RECOMMENDATION_TEMPLATES.SAVE').click();
        fixture.detectChanges();

        const host = fixture.nativeElement as HTMLElement;
        expect(host.querySelector('.client-dashboard__recommendation-form')?.textContent).toContain('FORM_ERRORS.REQUIRED');
        expect(document.activeElement).toBe(templateNameField());
        expect(screen.read()).toMatchObject({ recommendation: { text, templateName: '' } });
        expect(dietologistService.createRecommendationTemplate).not.toHaveBeenCalled();
        expect(dietologistService.createRecommendation).not.toHaveBeenCalled();
    });
});

describe('ClientDashboardComponent shared profile', () => {
    it.each([
        { gender: 'F', shareProfile: true },
        { gender: 'Female', shareProfile: true },
        { gender: 'Female', shareProfile: false },
    ])('shows the localized profile for $gender with sharing set to $shareProfile', ({ gender, shareProfile }) => {
        dietologistService.getMyClients.mockReturnValueOnce(
            of([createClient({ gender, permissions: { ...createClient().permissions, shareProfile } })]),
        );
        createComponent('client-1', false, 'ru');

        const host = fixture.nativeElement as HTMLElement;
        const labels = (selector: string): string[] => Array.from(host.querySelectorAll(selector), node => node.textContent.trim());
        const profileValues = ['180 см', 'Женский', 'Умеренная'];
        expect(host.querySelector('h1')?.textContent.trim()).toBe('Alex Ivanov');
        expect(labels('.client-dashboard__chip')).toEqual(shareProfile ? profileValues : []);
        expect(labels('.client-dashboard__metrics--wide .client-dashboard__metric-value')).toEqual(
            shareProfile ? ['client@example.com', ...profileValues, '-'] : [],
        );
    });
});

describe('ClientDashboardComponent private client dashboard', () => {
    it('shows the unnamed client safely and denies a recommendation draft without a sharing permission', () => {
        dietologistService.getMyClients.mockReturnValueOnce(
            of([
                createClient({
                    firstName: null,
                    lastName: null,
                    email: null,
                    permissions: { ...createClient().permissions, shareMeals: false, shareProfile: false },
                }),
            ]),
        );
        const { screen } = createDevelopmentDashboard(false);
        const host = fixture.nativeElement as HTMLElement;

        expect(host.textContent).toContain('ATTENTION.UNNAMED_CLIENT');
        const form = host.querySelector('.client-dashboard__recommendation-form');
        expect(form?.querySelector('p:not([hidden])')?.textContent).toContain(
            'DIETOLOGIST.CLIENT_DASHBOARD.RECOMMENDATIONS.PERMISSIONS_REQUIRED',
        );
        expect(form?.querySelector('button[type="submit"]')?.matches(':disabled')).toBe(true);
        expect(host.querySelector('.client-dashboard__date-filter')).toBeNull();
        expect(dietologistService.getClientDashboard).not.toHaveBeenCalled();
        expect(() => screen.fillDraft?.({ type: 'recommendation', text: 'Should remain private' })).toThrow(
            'Client permissions do not allow recommendations.',
        );
        expect(screen.isDirty?.()).toBe(false);
    });
});

describe('ClientDashboardComponent shared diary details', () => {
    it.each([
        { shareMeals: true, shareFasting: true },
        { shareMeals: true, shareFasting: false },
        { shareMeals: false, shareFasting: true },
    ])('applies meal and fasting permissions independently ($shareMeals, $shareFasting)', ({ shareMeals, shareFasting }) => {
        const recipeName = 'Garden soup';
        const meal = nutritionMealFixture({
            items: [{ id: 'recipe-item', mealId: 'meal-1', amount: 2, recipeId: 'shared-recipe', recipeName }],
        });
        const checkInAtUtc = '2026-10-28T10:00:00Z';
        const sharedFasting = fastingSessionFromSdk({
            id: 'shared-fast',
            startedAtUtc: '2026-10-28T06:00:00Z',
            initialPlannedDurationHours: 16,
            addedDurationHours: 0,
            plannedDurationHours: 16,
            protocol: 'Fast16Eat8',
            planType: 'Intermittent',
            occurrenceKind: 'FastingWindow',
            isCompleted: false,
            status: 'Active',
            symptoms: [],
            checkIns: [],
            checkInAtUtc,
            hungerLevel: 6,
            moodLevel: 3,
        });
        dietologistService.getMyClients.mockReturnValueOnce(
            of([createClient({ permissions: { ...createClient().permissions, shareMeals, shareFasting } })]),
        );
        dietologistService.getClientDashboard.mockReturnValueOnce(
            of(nutritionSnapshotFixture([meal], { currentFastingSession: sharedFasting })),
        );
        createComponent('client-1');
        const host = fixture.nativeElement as HTMLElement;
        const items = host.querySelector('fd-client-dashboard-meals-card article .client-dashboard__row-text');
        const checkIn = host.querySelector('fd-client-dashboard-fasting-card .client-dashboard__row-text');

        expect(items?.textContent.trim() ?? null).toBe(shareMeals ? `${recipeName} — 2 QUICK_MEAL.SERVINGS_OTHER` : null);
        expect(checkIn?.textContent.trim() ?? null).toBe(shareFasting ? '6 / - / 3' : null);
    });
});

describe('ClientDashboardComponent disconnect retry', () => {
    it('keeps the dashboard after a confirmed disconnect fails and exits only after a successful retry', () => {
        const confirm = new Subject<boolean>();
        dialogService.open.mockReturnValueOnce({ afterClosed: () => confirm, close: vi.fn() });
        dietologistService.disconnectClient.mockReturnValueOnce(throwError(() => new Error('Disconnect unavailable')));
        createComponent('client-1', true);
        const disconnect = dashboardButton('DIETOLOGIST.CLIENT_DASHBOARD.DISCONNECT');

        disconnect.click();
        expect(dietologistService.disconnectClient).not.toHaveBeenCalled();
        confirm.next(true);
        confirm.complete();
        fixture.detectChanges();

        expect(toastService.error).toHaveBeenCalledExactlyOnceWith('DIETOLOGIST.CLIENT_DASHBOARD.DISCONNECT_ERROR');
        expect(router.navigate).not.toHaveBeenCalled();
        expect(disconnect.isConnected).toBe(true);
        disconnect.click();
        fixture.detectChanges();

        expect(dietologistService.disconnectClient).toHaveBeenCalledTimes(2);
        expect(dietologistService.disconnectClient).toHaveBeenLastCalledWith('client-1');
        expect(router.navigate).toHaveBeenCalledExactlyOnceWith(['/dietologist']);
    });
});

function templateNameField(): HTMLInputElement {
    const host = fixture.nativeElement as HTMLElement;
    const input = host.querySelector<HTMLInputElement>('.client-dashboard__recommendation-form input');
    if (input === null) {
        throw new Error('Recommendation template name input is missing');
    }
    return input;
}

function createDevelopmentDashboard(fullAccess = true): { screen: DevelopmentScreen; detach: ReturnType<typeof vi.fn> } {
    const detach = vi.fn();
    const attach = vi.fn<(screen: DevelopmentScreen) => () => void>().mockReturnValue(detach);
    TestBed.configureTestingModule({ providers: [{ provide: DEVELOPMENT_SCREENS, useValue: { attach } }] });
    createComponent('client-1', fullAccess);
    expect(attach).toHaveBeenCalledOnce();
    return { screen: attach.mock.calls[0][0], detach };
}

function dashboardButton(label: string): HTMLButtonElement {
    const host = fixture.nativeElement as HTMLElement;
    const button = Array.from(host.querySelectorAll('button')).find(candidate => candidate.textContent.trim() === label);
    if (button === undefined) {
        throw new Error(`Dashboard button "${label}" was not found`);
    }
    return button;
}

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

describe('ClientDashboardComponent applied period ownership', () => {
    const currentDay = 23;
    const draftStartDay = 5;
    const draftEndDay = 8;
    const firstDayOfNextWeek = 24;
    const lastDayOfNextWeek = 30;
    const midday = 12;
    const draftStart = new Date(SELECTED_YEAR, MAY_MONTH_INDEX, draftStartDay);
    const draftEnd = new Date(SELECTED_YEAR, MAY_MONTH_INDEX, draftEndDay);
    const clock = new Date(SELECTED_YEAR, MAY_MONTH_INDEX, currentDay, midday);
    const originalMealCount = 2;
    const draftMealDate = '2026-05-06T12:30:00Z';

    beforeEach(() => {
        vi.useFakeTimers({ toFake: ['Date'] });
        vi.setSystemTime(clock);
    });
    afterEach(() => vi.useRealTimers());

    it('continues paging the applied period until the pending calendar selection is submitted', async () => {
        dietologistService.getClientDashboard
            .mockReturnValueOnce(of(mealPage('applied-lunch', 'Lunch', originalMealCount)))
            .mockImplementationOnce((_client: string, period: { dateFrom: Date }) =>
                of(
                    period.dateFrom.getTime() === draftStart.getTime()
                        ? mealPage('new-period-dinner', 'Dinner', originalMealCount, draftMealDate)
                        : mealPage('applied-breakfast', 'Breakfast', originalMealCount),
                ),
            )
            .mockReturnValueOnce(of(mealPage('new-period-dinner', 'Dinner', 1, draftMealDate)));
        createComponent('client-1');
        const appliedPeriod = getLastDashboardPeriod();
        expect(visibleMealTitles()).toEqual(['MEAL_TYPES.LUNCH']);

        await chooseDraftPeriodAsync();
        expect(dietologistService.getClientDashboard).toHaveBeenCalledTimes(1);
        expect(visibleMealTitles()).toEqual(['MEAL_TYPES.LUNCH']);

        dashboardButton('DIETOLOGIST.CLIENT_DASHBOARD.MEALS.LOAD_MORE').click();
        await fixture.whenStable();
        fixture.detectChanges();
        expect(visibleMealTitles()).toEqual(['MEAL_TYPES.LUNCH', 'MEAL_TYPES.BREAKFAST']);
        expect(getLastDashboardPeriod()).toMatchObject({ ...appliedPeriod, page: 2 });

        dashboardButton('DIETOLOGIST.CLIENT_DASHBOARD.DATE_FILTER.APPLY').click();
        await fixture.whenStable();
        fixture.detectChanges();
        expect(getLastDashboardPeriod()).toMatchObject({ dateFrom: draftStart, dateTo: draftEnd, page: 1 });
        expect(visibleMealTitles()).toEqual(['MEAL_TYPES.DINNER']);
    });

    it('moves the applied week forward when the date fields contain an unsubmitted draft', async () => {
        createComponent('client-1');
        await chooseDraftPeriodAsync();
        expect(dietologistService.getClientDashboard).toHaveBeenCalledTimes(1);

        dashboardButton('DIETOLOGIST.CLIENT_DASHBOARD.DATE_FILTER.NEXT_PERIOD').click();
        await fixture.whenStable();
        expect(getLastDashboardPeriod()).toMatchObject({
            dateFrom: new Date(SELECTED_YEAR, MAY_MONTH_INDEX, firstDayOfNextWeek),
            dateTo: new Date(SELECTED_YEAR, MAY_MONTH_INDEX, lastDayOfNextWeek),
            page: 1,
        });
    });
});

describe('ClientDashboardComponent calendar period navigation', () => {
    beforeEach(() => vi.useFakeTimers({ toFake: ['Date'] }));
    afterEach(() => vi.useRealTimers());

    it('shifts inclusive periods by calendar days across the spring clock change', async () => {
        const march = 2;
        const firstDayOfCurrentWeek = 2;
        const lastDayOfCurrentWeek = 8;
        const firstDayOfNextWeek = 9;
        const lastDayOfNextWeek = 15;
        const midday = 12;
        vi.setSystemTime(new Date(SELECTED_YEAR, march, lastDayOfCurrentWeek, midday));
        createComponent('client-1');
        const original = {
            dateFrom: new Date(SELECTED_YEAR, march, firstDayOfCurrentWeek),
            dateTo: new Date(SELECTED_YEAR, march, lastDayOfCurrentWeek),
            page: 1,
        };
        expect(getLastDashboardPeriod()).toMatchObject(original);

        dashboardButton('DIETOLOGIST.CLIENT_DASHBOARD.DATE_FILTER.NEXT_PERIOD').click();
        await fixture.whenStable();
        expect(getLastDashboardPeriod()).toMatchObject({
            dateFrom: new Date(SELECTED_YEAR, march, firstDayOfNextWeek),
            dateTo: new Date(SELECTED_YEAR, march, lastDayOfNextWeek),
            page: 1,
        });

        dashboardButton('DIETOLOGIST.CLIENT_DASHBOARD.DATE_FILTER.PREVIOUS_PERIOD').click();
        await fixture.whenStable();
        expect(getLastDashboardPeriod()).toMatchObject(original);
    });
});

function mealPage(id: string, mealType: string, total: number, date = '2026-05-23T12:30:00Z'): Record<string, unknown> {
    const item = {
        id: entityId<'meal'>(id),
        date: utcInstant(date),
        mealType,
        totalCalories: 400,
        totalProteins: 20,
        totalFats: 10,
        totalCarbs: 20,
        totalFiber: 5,
        totalAlcohol: 0,
        isNutritionAutoCalculated: false,
        items: [],
    } satisfies Meal;
    return { ...createDashboardSnapshot(), meals: { items: [item], total } };
}

function visibleMealTitles(): string[] {
    const root = fixture.nativeElement as HTMLElement;
    return [...root.querySelectorAll('fd-client-dashboard-meals-card article strong.client-dashboard__row-title')].map(title =>
        title.textContent.trim(),
    );
}

async function chooseDraftPeriodAsync(): Promise<void> {
    const root = fixture.nativeElement as HTMLElement;
    const triggers = root.querySelectorAll<HTMLButtonElement>('.client-dashboard__date-filter button[aria-haspopup="dialog"]');
    let index = 0;
    for (const day of ['2026-05-05', '2026-05-08']) {
        const trigger = triggers[index++];
        trigger.click();
        const dayButton = await vi.waitFor(() => {
            fixture.detectChanges();
            const button = document.querySelector<HTMLButtonElement>(`[role="dialog"] [data-date="${day}"]`);
            if (button === null) {
                throw new Error(`Expected the selectable date ${day}`);
            }
            return button;
        });
        expect(dayButton.disabled).toBe(false);
        dayButton.click();
        fixture.detectChanges();
        expect(trigger.getAttribute('aria-expanded')).toBe('false');
    }
}

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

describe('ClientDashboardComponent period refresh cancellation', () => {
    it('leaves the dashboard cleanly while a selected period is loading', async () => {
        createComponent('client-1');
        const dashboard = component['dashboard']();
        const pending = new Subject<DietologistDashboardSnapshot>();
        dietologistService.getClientDashboard.mockReturnValueOnce(pending);
        component['dateFilterModel'].set({ dateFrom: '2026-05-22', dateTo: '2026-05-22' });

        const refreshing = component['applyDateFilterAsync']();
        expect(component['detailsLoading']()).toBe(true);
        expect(pending.observed).toBe(true);

        fixture.destroy();

        await expect(refreshing).resolves.toBeUndefined();
        expect(pending.observed).toBe(false);
        expect(component['dashboard']()).toBe(dashboard);
    });
});

describe('ClientDashboardComponent task deadlines', () => {
    it.each(['   ', '\t\n', '\u00A0'])('rejects a whitespace-only task title %j without a request', async title => {
        createComponent('client-1');
        component['taskModel'].set({ title, details: 'Keep my draft', dueDate: '' });
        fixture.detectChanges();

        await component['submitTaskAsync']();
        fixture.detectChanges();

        expect(component['taskForm'].title().errors()).toContainEqual(expect.objectContaining({ kind: 'required' }));
        expect(component['taskForm'].title().touched()).toBe(true);
        expect(dietologistService.createTask).not.toHaveBeenCalled();
        expect(component['taskModel']()).toEqual({ title, details: 'Keep my draft', dueDate: '' });
        const taskButton = [...(fixture.nativeElement as HTMLElement).querySelectorAll('button')].find(button =>
            button.textContent.includes('CLIENT_TASKS.FORM.CREATE'),
        );
        expect(taskButton?.disabled).toBe(false);
    });

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
        expect(root.querySelector<HTMLButtonElement>('.client-dashboard__date-apply button')?.disabled).toBe(false);

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

describe('ClientDashboardComponent successful draft resets', () => {
    it('clears template validation after saving while preserving recommendation text', () => {
        createComponent('client-1', true);
        dietologistService.createRecommendationTemplate.mockReturnValueOnce(of({ id: 'template-1', name: 'My template' }));
        component['recommendationModel'].set({ text: 'Add protein', templateName: 'My template' });

        component['createRecommendationTemplate']();
        fixture.detectChanges();

        expect(component['recommendationModel']()).toEqual({ text: 'Add protein', templateName: '' });
        expect(component['templateNameError']()).toBeNull();
        expect(component['recommendationForm']().touched()).toBe(false);
        const host = fixture.nativeElement as HTMLElement;
        expect(host.querySelector('[role="alert"]')).toBeNull();
    });

    it('resets touched validation after a successful recommendation while retaining the template name', async () => {
        createComponent('client-1', true);
        component['recommendationModel'].set({ text: 'Add protein', templateName: 'My template' });
        component['recommendationForm']().markAsTouched();

        await component['submitRecommendationAsync']();

        expect(component['recommendations']()[0]?.id).toBe('rec-1');
        expect(component['recommendationModel']()).toEqual({ text: '', templateName: 'My template' });
        expect(component['recommendationForm']().touched()).toBe(false);
    });

    it('resets touched validation after a successful task creation', async () => {
        createComponent('client-1');
        dietologistService.createTask.mockReturnValueOnce(of({ id: 'task-1' }));
        component['taskModel'].set({ title: 'Daily task', details: 'Test details', dueDate: '' });
        component['taskForm']().markAsTouched();

        await component['submitTaskAsync']();

        expect(component['tasks']()[0]?.id).toBe('task-1');
        expect(component['taskModel']()).toEqual({ title: '', details: '', dueDate: '' });
        expect(component['taskForm']().touched()).toBe(false);
    });
});

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

describe('ClientDashboardComponent pending drafts', () => {
    it('locks recommendation text and template actions until a failed send restores the draft', async () => {
        const pending = new Subject<never>();
        dietologistService.createRecommendation.mockReturnValueOnce(pending);
        createComponent('client-1', true);
        component['recommendationModel'].set({ text: 'Keep this recommendation', templateName: 'My template' });

        const submission = component['submitRecommendationAsync']();
        fixture.detectChanges();

        const host = fixture.nativeElement as HTMLElement;
        expect(host.querySelector<HTMLTextAreaElement>('.client-dashboard__recommendation-form textarea')?.disabled).toBe(true);
        expect(component['templateInvalid']()).toBe(true);
        component['createRecommendationTemplate']();
        expect(dietologistService.createRecommendationTemplate).not.toHaveBeenCalled();

        pending.error(new Error('save failed'));
        await submission;
        fixture.detectChanges();

        expect(host.querySelector<HTMLTextAreaElement>('.client-dashboard__recommendation-form textarea')?.disabled).toBe(false);
        expect(component['recommendationModel']()).toEqual({ text: 'Keep this recommendation', templateName: 'My template' });
    });

    it('locks every task field until a failed save restores the complete draft', async () => {
        const pending = new Subject<ClientTask>();
        dietologistService.createTask.mockReturnValueOnce(pending);
        createComponent('client-1');
        const draft = { title: 'Keep this task', details: 'Keep these details', dueDate: '2026-10-25' };
        component['taskModel'].set(draft);

        const submission = component['submitTaskAsync']();
        fixture.detectChanges();

        const fields = Array.from(
            (fixture.nativeElement as HTMLElement).querySelectorAll<HTMLInputElement | HTMLTextAreaElement>(
                '[aria-labelledby="dietologist-client-tasks-title"] input, [aria-labelledby="dietologist-client-tasks-title"] textarea',
            ),
        );
        expect(fields).toHaveLength(Object.keys(draft).length);
        expect(fields.every(field => field.disabled)).toBe(true);

        pending.error(new Error('save failed'));
        await submission;
        fixture.detectChanges();

        expect(fields.every(field => !field.disabled)).toBe(true);
        expect(component['taskModel']()).toEqual(draft);
    });

    it('locks the shared recommendation draft and prevents sending while its template is saving', async () => {
        const pending = new Subject<RecommendationTemplate>();
        dietologistService.createRecommendationTemplate.mockReturnValueOnce(pending);
        createComponent('client-1', true);
        component['recommendationModel'].set({ text: 'Template content', templateName: 'Template name' });

        component['createRecommendationTemplate']();
        fixture.detectChanges();

        const fields = Array.from(
            (fixture.nativeElement as HTMLElement).querySelectorAll<HTMLInputElement | HTMLTextAreaElement>(
                '.client-dashboard__recommendation-form input, .client-dashboard__recommendation-form textarea',
            ),
        );
        expect(fields).toHaveLength(2);
        expect(fields.every(field => field.disabled)).toBe(true);
        expect(component['recommendationDisabled']()).toBe(true);
        await component['submitRecommendationAsync']();
        expect(dietologistService.createRecommendation).not.toHaveBeenCalled();

        pending.error(new Error('save failed'));
        fixture.detectChanges();

        expect(fields.every(field => !field.disabled)).toBe(true);
        expect(component['recommendationModel']()).toEqual({ text: 'Template content', templateName: 'Template name' });
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

function createComponent(clientId: string, fullAccess = false, language: 'en' | 'ru' = 'en'): void {
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
                    paramMap: of(convertToParamMap({ clientId })),
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
    translate.setTranslation('ru', {
        GENERAL: { UNITS: { CM: 'см' } },
        USER_MANAGE: { GENDER_OPTIONS: { F: 'Женский' }, ACTIVITY_LEVEL_OPTIONS: { MODERATE: 'Умеренная' } },
    });
    translate.use(language);

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
