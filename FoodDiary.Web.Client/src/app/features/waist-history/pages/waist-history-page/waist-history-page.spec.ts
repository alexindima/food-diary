import { signal, type WritableSignal } from '@angular/core';
import { type ComponentFixture, DeferBlockBehavior, DeferBlockState, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { FdTourService } from 'fd-tour';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { of, Subject } from 'rxjs';
import { beforeEach, describe, expect, it, type Mock, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { NavigationService } from '../../../../services/navigation.service';
import { UserService } from '../../../../shared/api/user.service';
import { ViewportService } from '../../../../shared/platform/viewport.service';
import { LocalizedTourDefinitionService } from '../../../../shared/tours/localized-tour-definition.service';
import { WaistEntriesService } from '../../api/waist-entries.service';
import { WaistGoalHistoryDialogComponent } from '../../dialogs/waist-goal-history-dialog/waist-goal-history-dialog';
import type { WaistHistoryEntriesDialogResult } from '../../dialogs/waist-history-entries-dialog/waist-history-entries-dialog';
import { WaistHistoryEntriesDialogComponent } from '../../dialogs/waist-history-entries-dialog/waist-history-entries-dialog';
import { WaistHistoryEntryDialogComponent } from '../../dialogs/waist-history-entry-dialog/waist-history-entry-dialog';
import { WaistHistoryGoalDialogComponent } from '../../dialogs/waist-history-goal-dialog/waist-history-goal-dialog';
import { WaistHistoryFacade } from '../../lib/waist-history.facade';
import { WaistHistoryPageComponent } from './waist-history-page';

const FIXTURE_YEAR = 2026;
const FIXTURE_JUNE_INDEX = 5;
const FIXTURE_JUNE_LAST_DAY = 30;
const FIXTURE_REFERENCE_MEASUREMENT = 80;
const FIXTURE_CURRENT_MEASUREMENT = 78;
const FIXTURE_TARGET_MEASUREMENT = 75;
const ENTRY = { id: 'entry', userId: 'u', date: '2026-06-20', circumferenceCm: 80 };
async function setupAsync(): Promise<{
    fixture: ComponentFixture<WaistHistoryPageComponent>;
    component: WaistHistoryPageComponent;
    facade: WaistHistoryFacade;
    closed: Subject<WaistHistoryEntriesDialogResult | undefined>;
    open: Mock;
    startTour: Mock;
    navigate: Mock;
    mobile: WritableSignal<boolean>;
}> {
    const closed = new Subject<WaistHistoryEntriesDialogResult | undefined>();
    const open = vi.fn().mockReturnValue({ afterClosed: () => closed });
    const startTour = vi.fn();
    const navigate = vi.fn();
    const mobile = signal(false);
    TestBed.configureTestingModule({
        deferBlockBehavior: DeferBlockBehavior.Manual,
        imports: [WaistHistoryPageComponent],
        providers: [
            provideTranslateTesting(),
            provideRouter([]),
            {
                provide: WaistEntriesService,
                useValue: {
                    getPageSummary: vi.fn().mockReturnValue(
                        of({
                            entries: [ENTRY],
                            summary: [],
                            heightCm: 180,
                            goal: { desiredWaistCm: 75, startWaistCm: 100, startedAtUtc: '2026-01-01' },
                            goalHistory: [],
                        }),
                    ),
                    getSummary: vi.fn().mockReturnValue(of([])),
                    remove: vi.fn().mockReturnValue(of(void 0)),
                },
            },
            { provide: UserService, useValue: {} },
            { provide: FdUiDialogService, useValue: { open } },
            { provide: FdTourService, useValue: { start: startTour } },
            { provide: LocalizedTourDefinitionService, useValue: { build: vi.fn((definition: unknown) => definition) } },
            { provide: NavigationService, useValue: { navigateToHomeAsync: navigate } },
            { provide: ViewportService, useValue: { isMobile: mobile } },
        ],
    });
    await TestBed.compileComponents();
    const fixture = TestBed.createComponent(WaistHistoryPageComponent);
    fixture.detectChanges();
    for (const block of await fixture.getDeferBlocks()) {
        await block.render(DeferBlockState.Complete);
    }
    fixture.detectChanges();
    return {
        fixture,
        component: fixture.componentInstance,
        facade: fixture.debugElement.injector.get(WaistHistoryFacade),
        closed,
        open,
        startTour,
        navigate,
        mobile,
    };
}
let context: Awaited<ReturnType<typeof setupAsync>>;
beforeEach(async () => {
    context = await setupAsync();
});
describe('Waist history page composition', () => {
    it('renders metric data with both lower cards', () => {
        const { fixture, component } = context;
        const root = fixture.nativeElement as HTMLElement;
        expect(root.querySelector('fd-waist-history-goal-card')).not.toBeNull();
        expect(root.querySelector('fd-waist-history-entries-card')).not.toBeNull();
        expect(root.textContent).toContain('80');
        expect(component['waistChange']()).toBeNull();
    });
    it('uses the same facade for entry dialog and cleans up abandoned editing', () => {
        const { component, facade, closed, open } = context;
        component['startEdit'](ENTRY);
        expect(facade.isEditing()).toBe(true);
        expect(open).toHaveBeenLastCalledWith(
            WaistHistoryEntryDialogComponent,
            expect.objectContaining({
                size: 'sm',
                autoFocus: 'fd-ui-input input',
                providers: [{ provide: WaistHistoryFacade, useValue: facade }],
            }),
        );
        closed.next(undefined);
        expect(facade.isEditing()).toBe(false);
    });
    it('opens quick add from the actual header button on desktop and mobile', () => {
        const { fixture, mobile, closed, open } = context;
        for (const isMobile of [false, true]) {
            mobile.set(isMobile);
            fixture.detectChanges();
            (fixture.nativeElement as HTMLElement)
                .querySelector<HTMLButtonElement>('[data-tour-id="waist-history-entry-action"] button')
                ?.click();
            expect(open).toHaveBeenLastCalledWith(WaistHistoryEntryDialogComponent, expect.any(Object));
            closed.next(undefined);
        }
    });
    it.each([
        ['openGoalDialog', WaistHistoryGoalDialogComponent],
        ['openGoalHistoryDialog', WaistGoalHistoryDialogComponent],
    ] as const)('passes page facade to %s', (method, dialog) => {
        const { component, facade, open } = context;
        component[method]();
        expect(open).toHaveBeenCalledWith(
            dialog,
            expect.objectContaining({ providers: [{ provide: WaistHistoryFacade, useValue: facade }] }),
        );
    });
    it.each(['edit', 'remove', undefined] as const)('handles all-records result %s', action => {
        const { component, facade, closed, open } = context;
        const remove = vi.spyOn(facade, 'deleteEntry');
        component['openEntriesDialog']();
        expect(open).toHaveBeenCalledWith(
            WaistHistoryEntriesDialogComponent,
            expect.objectContaining({
                providers: [{ provide: WaistHistoryFacade, useValue: facade }],
            }),
        );
        closed.next(action === undefined ? undefined : { action, entry: ENTRY });
        expect(remove).toHaveBeenCalledTimes(action === 'remove' ? 1 : 0);
        expect(facade.isEditing()).toBe(action === 'edit');
    });
});
describe('History page periods and KPI states', () => {
    it.each([null, 'invalid', '2026-06-20'])('shows latest measurement month safely for %s', date => {
        const { component, facade } = context;
        facade.latestEntry.set(date === null ? null : { ...ENTRY, date });
        component['showLatestMeasurement']();
        expect(facade.selectedRange()).toBe(date === '2026-06-20' ? 'custom' : 'month');
        if (date === '2026-06-20') {
            expect(facade.customRangeModel().range).toEqual({
                start: new Date(FIXTURE_YEAR, FIXTURE_JUNE_INDEX, 1),
                end: new Date(FIXTURE_YEAR, FIXTURE_JUNE_INDEX, FIXTURE_JUNE_LAST_DAY),
            });
        }
    });
    it('delegates periods and starts contextual tour', () => {
        const { component, facade, startTour } = context;
        component['changeRange']('week');
        expect(facade.selectedRange()).toBe('week');
        component['startWaistHistoryTour']();
        expect(startTour).toHaveBeenLastCalledWith(expect.any(Object), { force: true });
        component['startWaistHistoryTour'](false);
        expect(startTour).toHaveBeenLastCalledWith(expect.any(Object), { force: false });
    });
    it.each([
        [FIXTURE_REFERENCE_MEASUREMENT, FIXTURE_CURRENT_MEASUREMENT, FIXTURE_TARGET_MEASUREMENT, 'positive'],
        [FIXTURE_CURRENT_MEASUREMENT, FIXTURE_REFERENCE_MEASUREMENT, FIXTURE_TARGET_MEASUREMENT, 'negative'],
        [FIXTURE_REFERENCE_MEASUREMENT, FIXTURE_REFERENCE_MEASUREMENT, FIXTURE_TARGET_MEASUREMENT, 'neutral'],
        [FIXTURE_REFERENCE_MEASUREMENT, FIXTURE_CURRENT_MEASUREMENT, null, 'neutral'],
        [FIXTURE_REFERENCE_MEASUREMENT, FIXTURE_CURRENT_MEASUREMENT, FIXTURE_CURRENT_MEASUREMENT, 'neutral'],
    ])('interprets monthly %s to %s toward %s', (first, last, goal, tone) => {
        const { component, facade } = context;
        facade.waistGoal.set({ desiredWaistCm: goal, startWaistCm: 100, startedAtUtc: null });
        facade.rollingMonthSummaryPoints.set([
            { startDate: '2026-06-01', endDate: '2026-06-01', averageCircumferenceCm: first },
            { startDate: '2026-06-02', endDate: '2026-06-02', averageCircumferenceCm: 0 },
            { startDate: '2026-06-03', endDate: '2026-06-03', averageCircumferenceCm: last },
        ]);
        expect(component['waistChange']()).toEqual({ value: last - first, tone });
    });
    it('does not show remaining amount without current, start or goal', () => {
        const { component, facade } = context;
        facade.latestEntry.set(null);
        expect(component['waistToGoal']()).toBeNull();
        facade.latestEntry.set(ENTRY);
        facade.waistGoal.set({ desiredWaistCm: 75, startWaistCm: null, startedAtUtc: null });
        expect(component['waistToGoal']()).toBeNull();
        facade.waistGoal.set({ desiredWaistCm: null, startWaistCm: 100, startedAtUtc: null });
        expect(component['waistToGoal']()).toBeNull();
    });
    it('reports zero when start and goal coincide', () => {
        const { component, facade } = context;
        facade.waistGoal.set({ desiredWaistCm: 80, startWaistCm: 80, startedAtUtc: null });
        expect(component['waistToGoal']()).toBe(0);
    });
});
