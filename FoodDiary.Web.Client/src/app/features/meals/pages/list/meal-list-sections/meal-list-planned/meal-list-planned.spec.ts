import { signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../../testing/translate-testing.module';
import { MealCardComponent } from '../../../../../../components/shared/meal-card/meal-card';
import { AuthService } from '../../../../../../services/auth.service';
import type { Meal } from '../../../../models/meal.data';
import type { MealDateGroupView } from '../../meal-list-lib/meal-list.types';
import { MealListPlannedComponent } from './meal-list-planned';

describe('MealListPlannedComponent', () => {
    it('should render planned meal cards with date-aware titles and forward events', async () => {
        const meal = createMeal();
        const { component, fixture } = await setupComponentAsync({
            groups: [createGroup([meal])],
            favoriteLoadingIds: new Set([meal.id]),
        });
        const openedSpy = vi.fn();
        component['mealOpened'].subscribe(openedSpy);

        fixture.detectChanges();
        const mealCard = fixture.debugElement.query(By.directive(MealCardComponent)).componentInstance as MealCardComponent;
        mealCard.open.emit();

        expect(getFixtureText(fixture)).toContain('CONSUMPTION_LIST.PLANNED_TITLE');
        expect(getFixtureText(fixture)).toContain('(1)');
        expect(mealCard.showDate()).toBe(true);
        expect(mealCard.favoriteLoading()).toBe(true);
        expect(openedSpy).toHaveBeenCalledWith(meal);
    });
});

async function setupComponentAsync(
    overrides: Partial<{
        favoriteLoadingIds: ReadonlySet<string>;
        groups: readonly MealDateGroupView[];
        isOpen: boolean;
    }> = {},
): Promise<{
    component: MealListPlannedComponent;
    fixture: ComponentFixture<MealListPlannedComponent>;
}> {
    await TestBed.resetTestingModule()
        .configureTestingModule({
            imports: [MealListPlannedComponent],
            providers: [
                provideTranslateTesting(),
                { provide: AuthService, useValue: { isAuthenticated: signal(true) } },
                { provide: FdUiDialogService, useValue: { open: vi.fn() } },
            ],
        })
        .compileComponents();

    const fixture = TestBed.createComponent(MealListPlannedComponent);
    fixture.componentRef.setInput('groups', overrides.groups ?? []);
    fixture.componentRef.setInput('isOpen', overrides.isOpen ?? true);
    fixture.componentRef.setInput('favoriteLoadingIds', overrides.favoriteLoadingIds ?? new Set<string>());

    return {
        component: fixture.componentInstance,
        fixture,
    };
}

function getFixtureText(fixture: ComponentFixture<MealListPlannedComponent>): string {
    const host = fixture.nativeElement as HTMLElement;
    return host.textContent;
}

function createGroup(items: Meal[]): MealDateGroupView {
    return {
        date: new Date('2026-05-14T00:00:00Z'),
        dateLabel: 'May 14',
        items,
    };
}

function createMeal(overrides: Partial<Meal> = {}): Meal {
    return {
        id: 'meal-1',
        date: '2026-05-14T12:00:00Z',
        mealType: 'LUNCH',
        comment: null,
        imageUrl: null,
        imageAssetId: null,
        totalCalories: 500,
        totalProteins: 30,
        totalFats: 20,
        totalCarbs: 50,
        totalFiber: 5,
        totalAlcohol: 0,
        isNutritionAutoCalculated: true,
        preMealSatietyLevel: null,
        postMealSatietyLevel: null,
        items: [],
        aiSessions: [],
        ...overrides,
    };
}
