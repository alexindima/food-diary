import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { type Observable, of, Subject, throwError } from 'rxjs';
import { describe, expect, it, type Mock, vi } from 'vitest';

import { nutritionMealFixture, nutritionSnapshotFixture } from '../../../../../testing/nutrition-details.fixtures';
import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import type { DashboardSnapshot } from '../../../../shared/models/dashboard.data';
import { DashboardService } from '../../api/dashboard.service';
import type { NutritionDetailsData } from '../../models/nutrition-details.types';
import { NutritionDetailsDialogComponent } from './nutrition-details-dialog';

const INITIAL_SOURCE_COUNT = 3;
const ALL_SOURCE_COUNT = 4;
const SOURCE_VALUES = { first: 40, second: 30, third: 20, fourth: 10 };
type DialogTestContext = {
    fixture: ComponentFixture<NutritionDetailsDialogComponent>;
    host: HTMLElement;
    request: Mock<() => Observable<DashboardSnapshot>>;
    close: Mock;
};

async function setupAsync(
    data: Partial<NutritionDetailsData> = {},
    request: Mock<() => Observable<DashboardSnapshot>> = vi
        .fn<() => Observable<DashboardSnapshot>>()
        .mockReturnValue(of(nutritionSnapshotFixture())),
): Promise<DialogTestContext> {
    const close = vi.fn();
    await TestBed.configureTestingModule({
        imports: [NutritionDetailsDialogComponent],
        providers: [
            provideTranslateTesting(),
            { provide: FD_UI_DIALOG_DATA, useValue: { date: new Date('2026-10-28T00:00:00'), metric: 'protein', ...data } },
            { provide: DashboardService, useValue: { getSnapshotSilentlyStrict: request } },
            { provide: FdUiDialogRef, useValue: { close } },
        ],
    }).compileComponents();
    const fixture = TestBed.createComponent(NutritionDetailsDialogComponent);
    fixture.detectChanges();
    return { fixture, host: fixture.nativeElement as HTMLElement, request, close };
}

describe('Nutrition details dialog', () => {
    it('shows loading, retries a failed full-day request, then renders an empty day', async () => {
        const pending = new Subject<DashboardSnapshot>();
        const request = vi.fn().mockReturnValueOnce(pending).mockReturnValueOnce(of(nutritionSnapshotFixture()));
        const { fixture, host } = await setupAsync({}, request);
        expect(host.textContent).toContain('DASHBOARD.NUTRITION_DETAILS.LOADING');
        pending.error(new Error('offline'));
        fixture.detectChanges();
        expect(host.textContent).toContain('DASHBOARD.NUTRITION_DETAILS.ERROR_TITLE');
        expect(host.querySelector('.nutrition-details__summary')).toBeNull();
        const retry = host.querySelector<HTMLButtonElement>('fd-ui-section-error-state button');
        expect(retry).not.toBeNull();
        retry?.click();
        fixture.detectChanges();
        expect(request).toHaveBeenCalledTimes(2);
        expect(host.textContent).toContain('DASHBOARD.NUTRITION_DETAILS.EMPTY');
    });

    it('shows real percentage above 100, expands sources, and closes with the selected full meal', async () => {
        const meals = Object.values(SOURCE_VALUES).map((value, index) =>
            nutritionMealFixture({ id: `meal-${index}`, isNutritionAutoCalculated: false, totalProteins: value, manualProteins: value }),
        );
        const snapshot = nutritionSnapshotFixture(meals);
        snapshot.statistics.averageProteins = 100;
        snapshot.statistics.proteinGoal = 80;
        const { fixture, host, close } = await setupAsync({}, vi.fn().mockReturnValue(of(snapshot)));
        expect(host.textContent).toContain('125%');
        expect(host.textContent).toContain('DASHBOARD.NUTRITION_DETAILS.MANUAL');
        expect(host.querySelectorAll('.nutrition-details__source')).toHaveLength(INITIAL_SOURCE_COUNT);
        const showAll = [...host.querySelectorAll<HTMLButtonElement>('button')].find(button =>
            button.textContent.includes('DASHBOARD.NUTRITION_DETAILS.SHOW_ALL'),
        );
        showAll?.click();
        fixture.detectChanges();
        expect(host.querySelectorAll('.nutrition-details__source')).toHaveLength(ALL_SOURCE_COUNT);
        host.querySelector<HTMLButtonElement>('.nutrition-details__sources button')?.click();
        expect(close).toHaveBeenCalledExactlyOnceWith({ kind: 'meal', meal: meals[0] });
        expect(host.textContent).not.toContain('DASHBOARD.NUTRITION_DETAILS.UNATTRIBUTED_HINT');
    });

    it('does not fabricate a percentage for an absent goal and offers the existing goals and diary flows', async () => {
        const { host, close } = await setupAsync({ metric: 'fiber' });
        expect(host.querySelector('fd-ui-progress-ring')).toBeNull();
        expect(host.textContent).toContain('DASHBOARD.NUTRITION_DETAILS.NO_GOAL');
        const buttons = [...host.querySelectorAll<HTMLButtonElement>('button')];
        buttons.find(button => button.getAttribute('aria-label') === 'DASHBOARD.NUTRITION_DETAILS.EDIT_GOALS')?.click();
        expect(close).toHaveBeenLastCalledWith({ kind: 'goals' });
        buttons.find(button => button.textContent.includes('DASHBOARD.NUTRITION_DETAILS.OPEN_DIARY'))?.click();
        expect(close).toHaveBeenLastCalledWith({ kind: 'diary' });
    });

    it('never presents a failed request as a zero day', async () => {
        const { host } = await setupAsync({}, vi.fn().mockReturnValue(throwError(() => new Error('offline'))));
        expect(host.querySelector('.nutrition-details__summary')).toBeNull();
        expect(host.textContent).toContain('DASHBOARD.NUTRITION_DETAILS.ERROR_TITLE');
    });
});
