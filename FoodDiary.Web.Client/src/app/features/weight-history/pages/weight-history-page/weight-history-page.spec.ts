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
import { WeightEntriesService } from '../../api/weight-entries.service';
import { WeightGoalHistoryDialogComponent } from '../../dialogs/weight-goal-history-dialog/weight-goal-history-dialog';
import type { WeightHistoryEntriesDialogResult } from '../../dialogs/weight-history-entries-dialog/weight-history-entries-dialog';
import { WeightHistoryEntriesDialogComponent } from '../../dialogs/weight-history-entries-dialog/weight-history-entries-dialog';
import { WeightHistoryEntryDialogComponent } from '../../dialogs/weight-history-entry-dialog/weight-history-entry-dialog';
import { WeightHistoryGoalDialogComponent } from '../../dialogs/weight-history-goal-dialog/weight-history-goal-dialog';
import { WeightHistoryFacade } from '../../lib/weight-history.facade';
import { WeightHistoryPageComponent } from './weight-history-page';

const FIXTURE_YEAR = 2026;
const FIXTURE_JUNE_INDEX = 5;
const FIXTURE_JUNE_LAST_DAY = 30;
const FIXTURE_REFERENCE_MEASUREMENT = 80;
const FIXTURE_CURRENT_MEASUREMENT = 78;
const FIXTURE_TARGET_MEASUREMENT = 75;
const ENTRY = { id: 'entry', userId: 'u', date: '2026-06-20', weightKg: 80 };
async function setupAsync(): Promise<{
    fixture: ComponentFixture<WeightHistoryPageComponent>;
    component: WeightHistoryPageComponent;
    facade: WeightHistoryFacade;
    closed: Subject<WeightHistoryEntriesDialogResult | undefined>;
    open: Mock;
    startTour: Mock;
    navigate: Mock;
    mobile: WritableSignal<boolean>;
}> {
    const closed = new Subject<WeightHistoryEntriesDialogResult | undefined>();
    const open = vi.fn().mockReturnValue({ afterClosed: () => closed });
    const startTour = vi.fn();
    const navigate = vi.fn();
    const mobile = signal(false);
    TestBed.configureTestingModule({
        deferBlockBehavior: DeferBlockBehavior.Manual,
        imports: [WeightHistoryPageComponent],
        providers: [
            provideTranslateTesting(),
            provideRouter([]),
            {
                provide: WeightEntriesService,
                useValue: {
                    getPageSummary: vi.fn().mockReturnValue(
                        of({
                            entries: [ENTRY],
                            summary: [],
                            heightCm: 180,
                            goal: { desiredWeightKg: 75, startWeightKg: 100, startedAtUtc: '2026-01-01' },
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
    const fixture = TestBed.createComponent(WeightHistoryPageComponent);
    fixture.detectChanges();
    for (const block of await fixture.getDeferBlocks()) {
        await block.render(DeferBlockState.Complete);
    }
    fixture.detectChanges();
    return {
        fixture,
        component: fixture.componentInstance,
        facade: fixture.debugElement.injector.get(WeightHistoryFacade),
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
describe('Weight history page composition', () => {
    it('renders metric data with both lower cards', () => {
        const { fixture, component } = context;
        const root = fixture.nativeElement as HTMLElement;
        expect(root.querySelector('fd-weight-history-goal-card')).not.toBeNull();
        expect(root.querySelector('fd-weight-history-entries-card')).not.toBeNull();
        expect(root.textContent).toContain('80');
        expect(component['weightChange']()).toBeNull();
    });
    it('uses the same facade for entry dialog and cleans up abandoned editing', () => {
        const { component, facade, closed, open } = context;
        component['startEdit'](ENTRY);
        expect(facade.isEditing()).toBe(true);
        expect(open).toHaveBeenLastCalledWith(
            WeightHistoryEntryDialogComponent,
            expect.objectContaining({ providers: [{ provide: WeightHistoryFacade, useValue: facade }] }),
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
                .querySelector<HTMLButtonElement>('[data-tour-id="weight-history-entry-action"] button')
                ?.click();
            expect(open).toHaveBeenLastCalledWith(WeightHistoryEntryDialogComponent, expect.any(Object));
            closed.next(undefined);
        }
    });
    it.each([
        ['openGoalDialog', WeightHistoryGoalDialogComponent],
        ['openGoalHistoryDialog', WeightGoalHistoryDialogComponent],
    ] as const)('passes page facade to %s', (method, dialog) => {
        const { component, facade, open } = context;
        component[method]();
        expect(open).toHaveBeenCalledWith(
            dialog,
            expect.objectContaining({ providers: [{ provide: WeightHistoryFacade, useValue: facade }] }),
        );
    });
    it.each(['edit', 'remove', undefined] as const)('handles all-records result %s', action => {
        const { component, facade, closed, open } = context;
        const remove = vi.spyOn(facade, 'deleteEntry');
        component['openEntriesDialog']();
        expect(open).toHaveBeenCalledWith(
            WeightHistoryEntriesDialogComponent,
            expect.objectContaining({
                data: expect.objectContaining<Record<string, unknown>>({ entries: [ENTRY], desiredWeightKg: 75 }) as unknown,
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
        component['startWeightHistoryTour']();
        expect(startTour).toHaveBeenLastCalledWith(expect.any(Object), { force: true });
        component['startWeightHistoryTour'](false);
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
        facade.weightGoal.set({ desiredWeightKg: goal, startWeightKg: 100, startedAtUtc: null });
        facade.rollingMonthSummaryPoints.set([
            { startDate: '2026-06-01', endDate: '2026-06-01', averageWeightKg: first },
            { startDate: '2026-06-02', endDate: '2026-06-02', averageWeightKg: 0 },
            { startDate: '2026-06-03', endDate: '2026-06-03', averageWeightKg: last },
        ]);
        expect(component['weightChange']()).toEqual({ value: last - first, tone });
    });
    it('does not show remaining amount without current, start or goal', () => {
        const { component, facade } = context;
        facade.latestEntry.set(null);
        expect(component['weightToGoal']()).toBeNull();
        facade.latestEntry.set(ENTRY);
        facade.weightGoal.set({ desiredWeightKg: 75, startWeightKg: null, startedAtUtc: null });
        expect(component['weightToGoal']()).toBeNull();
        facade.weightGoal.set({ desiredWeightKg: null, startWeightKg: 100, startedAtUtc: null });
        expect(component['weightToGoal']()).toBeNull();
    });
    it('navigates back', () => {
        const { component, navigate } = context;
        component['navigateBack']();
        expect(navigate).toHaveBeenCalledOnce();
    });
});
