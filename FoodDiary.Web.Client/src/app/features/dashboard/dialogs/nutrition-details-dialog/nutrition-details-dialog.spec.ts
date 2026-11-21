import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { firstValueFrom, type Observable, of, Subject, throwError } from 'rxjs';
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
        const empty = nutritionSnapshotFixture();
        empty.statistics.averageProteins = 0;
        const request = vi.fn().mockReturnValueOnce(pending).mockReturnValueOnce(of(empty));
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

describe('Nutrition detail totals after a concurrent diary edit', () => {
    it.each(['save', 'remove'] as const)('retries when statistics and meals straddle a %s', async operation => {
        const protein = 12.34;
        const meal = nutritionMealFixture({ isNutritionAutoCalculated: false, totalProteins: protein, manualProteins: protein });
        const stable = nutritionSnapshotFixture([meal]);
        stable.statistics.averageProteins = protein;
        stable.statistics.proteinGoal = 40;
        const inconsistent = nutritionSnapshotFixture(operation === 'save' ? [meal] : []);
        inconsistent.statistics.averageProteins = operation === 'save' ? 0 : protein;
        const request = vi.fn().mockReturnValueOnce(of(inconsistent)).mockReturnValueOnce(of(stable));
        const { fixture, host } = await setupAsync({}, request);

        expect(host.textContent).toContain('DASHBOARD.NUTRITION_DETAILS.ERROR_TITLE');
        expect(host.querySelector('.nutrition-details__summary')).toBeNull();
        expect(host.querySelector('.nutrition-details__source')).toBeNull();

        const retry = host.querySelector<HTMLButtonElement>('fd-ui-section-error-state button');
        expect(retry).not.toBeNull();
        retry?.click();
        fixture.detectChanges();

        expect(request.mock.calls[1]).toEqual(request.mock.calls[0]);
        expect(host.textContent).not.toContain('DASHBOARD.NUTRITION_DETAILS.ERROR_TITLE');
        expect(host.querySelector('fd-ui-progress-ring')?.textContent).toContain('31%');
        expect(host.querySelector('.nutrition-details__source')?.textContent).toContain('100%');
    });
});

describe('Nutrition detail sources for a day rounded to zero', () => {
    it('keeps a persisted trace contribution and displays a finite zero share', async () => {
        const traceProtein = 0.004;
        const meal = nutritionMealFixture({ isNutritionAutoCalculated: false, totalProteins: traceProtein, manualProteins: traceProtein });
        const snapshot = nutritionSnapshotFixture([meal]);
        snapshot.statistics.averageProteins = 0;
        snapshot.statistics.proteinGoal = 40;
        const { host } = await setupAsync({}, vi.fn().mockReturnValue(of(snapshot)));

        const source = host.querySelector('.nutrition-details__source');
        expect(source?.textContent).toContain('DASHBOARD.NUTRITION_DETAILS.MANUAL');
        expect(source?.querySelector('span.nutrition-details__amount')?.textContent.trim()).toBe('0%');
        expect(host.querySelector('fd-ui-progress-ring')?.textContent.trim()).toBe('0%');
    });
});

describe('Nutrition detail labels for recorded sources', () => {
    it('keeps a named source and updates its unspecified meal label when translations change', async () => {
        const meal = nutritionMealFixture({
            mealType: null,
            items: [
                {
                    id: 'product-item',
                    mealId: 'meal-1',
                    productId: 'product-1',
                    productName: 'Plain skyr',
                    productBaseAmount: 100,
                    productProteinsPerBase: 10,
                    amount: 100,
                },
            ],
        });
        const { fixture, host, close, request } = await setupAsync({}, vi.fn().mockReturnValue(of(nutritionSnapshotFixture([meal]))));
        const sourceButton = host.querySelector<HTMLButtonElement>('.nutrition-details__sources button');
        expect(sourceButton?.querySelector('.nutrition-details__name')?.textContent.trim()).toBe('Plain skyr');
        expect(sourceButton?.textContent).toContain('MEAL_CARD.MEAL_TYPES.OTHER');
        expect(sourceButton?.textContent).toContain('100%');
        expect(host.textContent).not.toContain('DASHBOARD.NUTRITION_DETAILS.UNATTRIBUTED');

        const translate = TestBed.inject(TranslateService);
        translate.setTranslation('ru', { MEAL_CARD: { MEAL_TYPES: { OTHER: 'Другой приём пищи' } } });
        await firstValueFrom(translate.use('ru'));
        fixture.detectChanges();
        expect(sourceButton?.textContent).toContain('Другой приём пищи');
        translate.setTranslation('ru', { MEAL_CARD: { MEAL_TYPES: { OTHER: 'Приём пищи без категории' } } }, true);
        fixture.detectChanges();
        expect(sourceButton?.textContent).toContain('Приём пищи без категории');
        expect(sourceButton?.textContent).toContain('Plain skyr');
        expect(request).toHaveBeenCalledOnce();
        sourceButton?.click();
        expect(close).toHaveBeenCalledExactlyOnceWith({ kind: 'meal', meal });
    });
});
