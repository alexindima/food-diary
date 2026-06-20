import { signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiPaginationComponent } from 'fd-ui-kit/pagination/fd-ui-pagination';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../../testing/translate-testing.module';
import { ErrorStateComponent } from '../../../../../../components/shared/error-state/error-state';
import { MealCardComponent } from '../../../../../../components/shared/meal-card/meal-card';
import { AuthService } from '../../../../../../services/auth.service';
import type { Meal } from '../../../../models/meal.data';
import type { MealDateGroupView } from '../../meal-list-lib/meal-list.types';
import { MealListContentComponent } from './meal-list-content';

const PAGE_INDEX = 2;
const TOTAL_ITEMS = 25;
const TOTAL_PAGES = 3;
const PAGE_SIZE = 10;

describe('MealListContentComponent', () => {
    it('should render error state and emit retry', async () => {
        const { component, fixture } = await setupComponentAsync({ errorKey: 'ERRORS.LOAD_FAILED_TITLE' });
        const retrySpy = vi.fn();
        component['retry'].subscribe(retrySpy);

        fixture.detectChanges();
        const errorState = fixture.debugElement.query(By.directive(ErrorStateComponent)).componentInstance as ErrorStateComponent;
        errorState.retry.emit();

        expect(errorState.titleKey()).toBe('ERRORS.LOAD_FAILED_TITLE');
        expect(retrySpy).toHaveBeenCalledOnce();
    });

    it('should render empty state and emit add request', async () => {
        const { component, fixture } = await setupComponentAsync({ emptyState: 'empty' });
        const mealAddSpy = vi.fn();
        component['mealAdd'].subscribe(mealAddSpy);

        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;
        const button = host.querySelector<HTMLElement>('fd-ui-button');
        button?.click();

        expect(getFixtureText(fixture)).toContain('CONSUMPTION_LIST.EMPTY_TITLE');
        expect(mealAddSpy).toHaveBeenCalledOnce();
    });

    it('should render no-results state without add action', async () => {
        const { fixture } = await setupComponentAsync({ emptyState: 'no-results' });

        fixture.detectChanges();

        expect(getFixtureText(fixture)).toContain('CONSUMPTION_LIST.NO_RESULTS_TITLE');
        expect(getFixtureText(fixture)).not.toContain('CONSUMPTION_LIST.ADD_FIRST_CONSUMPTION_BUTTON');
    });

    it('should pass meal loading state and emit card events with selected meal', async () => {
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

        expect(mealCard.favoriteLoading()).toBe(true);
        expect(openedSpy).toHaveBeenCalledWith(meal);
    });

    it('should emit favorite toggle from card when meal is not loading', async () => {
        const meal = createMeal();
        const { component, fixture } = await setupComponentAsync({
            groups: [createGroup([meal])],
            favoriteLoadingIds: new Set(),
        });
        const favoriteToggleSpy = vi.fn();
        component['mealFavoriteToggle'].subscribe(favoriteToggleSpy);

        fixture.detectChanges();
        const mealCard = fixture.debugElement.query(By.directive(MealCardComponent)).componentInstance as MealCardComponent;
        mealCard.favoriteToggle.emit();

        expect(favoriteToggleSpy).toHaveBeenCalledWith(meal);
    });

    it('should emit page changes when pagination is visible', async () => {
        const { component, fixture } = await setupComponentAsync({ totalPages: TOTAL_PAGES, totalItems: TOTAL_ITEMS });
        const pageIndexChangeSpy = vi.fn();
        component['pageIndexChange'].subscribe(pageIndexChangeSpy);

        fixture.detectChanges();
        const pagination = fixture.debugElement.query(By.directive(FdUiPaginationComponent)).componentInstance as FdUiPaginationComponent;
        pagination['goToPage'](PAGE_INDEX);

        expect(pagination['length']()).toBe(TOTAL_ITEMS);
        expect(pagination['pageSize']()).toBe(PAGE_SIZE);
        expect(pageIndexChangeSpy).toHaveBeenCalledWith(PAGE_INDEX);
    });

    it('should hide pagination when only one page exists', async () => {
        const { fixture } = await setupComponentAsync({ totalPages: 1 });

        fixture.detectChanges();

        expect(fixture.debugElement.query(By.directive(FdUiPaginationComponent))).toBeNull();
    });
});

async function setupComponentAsync(
    overrides: Partial<{
        currentPageIndex: number;
        emptyState: 'empty' | 'no-results' | null;
        errorKey: string | null;
        favoriteLoadingIds: ReadonlySet<string>;
        groups: readonly MealDateGroupView[];
        isPlannedOpen: boolean;
        isLoading: boolean;
        plannedGroups: readonly MealDateGroupView[];
        totalItems: number;
        totalPages: number;
    }> = {},
): Promise<{
    component: MealListContentComponent;
    fixture: ComponentFixture<MealListContentComponent>;
}> {
    await TestBed.resetTestingModule()
        .configureTestingModule({
            imports: [MealListContentComponent],
            providers: [
                provideTranslateTesting(),
                { provide: AuthService, useValue: { isAuthenticated: signal(true) } },
                { provide: FdUiDialogService, useValue: { open: vi.fn() } },
            ],
        })
        .compileComponents();

    const fixture = TestBed.createComponent(MealListContentComponent);
    setComponentInputs(fixture, overrides);

    return {
        component: fixture.componentInstance,
        fixture,
    };
}

function setComponentInputs(
    fixture: ComponentFixture<MealListContentComponent>,
    overrides: Partial<{
        currentPageIndex: number;
        emptyState: 'empty' | 'no-results' | null;
        errorKey: string | null;
        favoriteLoadingIds: ReadonlySet<string>;
        groups: readonly MealDateGroupView[];
        isPlannedOpen: boolean;
        isLoading: boolean;
        plannedGroups: readonly MealDateGroupView[];
        totalItems: number;
        totalPages: number;
    }>,
): void {
    const inputs = {
        currentPageIndex: 0,
        emptyState: null,
        errorKey: null,
        favoriteLoadingIds: new Set<string>(),
        groups: [],
        isLoading: false,
        isPlannedOpen: false,
        plannedGroups: [],
        totalItems: 0,
        totalPages: 1,
        ...overrides,
    };

    fixture.componentRef.setInput('errorKey', inputs.errorKey);
    fixture.componentRef.setInput('isLoading', inputs.isLoading);
    fixture.componentRef.setInput('emptyState', inputs.emptyState);
    fixture.componentRef.setInput('plannedGroups', inputs.plannedGroups);
    fixture.componentRef.setInput('isPlannedOpen', inputs.isPlannedOpen);
    fixture.componentRef.setInput('groups', inputs.groups);
    fixture.componentRef.setInput('totalPages', inputs.totalPages);
    fixture.componentRef.setInput('totalItems', inputs.totalItems);
    fixture.componentRef.setInput('currentPageIndex', inputs.currentPageIndex);
    fixture.componentRef.setInput('favoriteLoadingIds', inputs.favoriteLoadingIds);
}

function getFixtureText(fixture: ComponentFixture<MealListContentComponent>): string {
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
