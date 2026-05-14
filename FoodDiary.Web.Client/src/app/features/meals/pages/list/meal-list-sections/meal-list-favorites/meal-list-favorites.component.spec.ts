import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { TranslateModule } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { describe, expect, it, vi } from 'vitest';

import { FavoritesSectionComponent } from '../../../../../../components/shared/favorites-section/favorites-section.component';
import type { FavoriteMeal } from '../../../../models/meal.data';
import type { FavoriteMealView } from '../../meal-list-lib/meal-list.types';
import { MealListFavoritesComponent } from './meal-list-favorites.component';

const FAVORITE_COUNT = 3;

describe('MealListFavoritesComponent', () => {
    it('should hide section when favorite list is empty', async () => {
        const { fixture } = await setupComponentAsync({ favoriteViews: [] });

        fixture.detectChanges();

        expect(fixture.debugElement.query(By.directive(FavoritesSectionComponent))).toBeNull();
    });

    it('should pass section inputs and emit section actions', async () => {
        const { component, fixture } = await setupComponentAsync({
            count: FAVORITE_COUNT,
            isOpen: true,
            showLoadMore: true,
            isLoadingMore: false,
        });
        const toggleSpy = vi.fn();
        const loadMoreSpy = vi.fn();
        component.toggleRequested.subscribe(toggleSpy);
        component.loadMore.subscribe(loadMoreSpy);

        fixture.detectChanges();
        const section = fixture.debugElement.query(By.directive(FavoritesSectionComponent)).componentInstance as FavoritesSectionComponent;
        section.toggleRequested.emit();
        section.loadMore.emit();

        expect(section.count()).toBe(FAVORITE_COUNT);
        expect(section.isOpen()).toBe(true);
        expect(section.showLoadMore()).toBe(true);
        expect(section.isLoadingMore()).toBe(false);
        expect(toggleSpy).toHaveBeenCalledOnce();
        expect(loadMoreSpy).toHaveBeenCalledOnce();
    });

    it('should render favorite name and fallback translation key', async () => {
        const namedFavorite = createFavoriteView({ displayName: 'Lunch box' });
        const fallbackFavorite = createFavoriteView({
            favorite: createFavorite({ id: 'favorite-2', mealId: 'meal-2' }),
            displayName: null,
            displayNameKey: 'CONSUMPTION_LIST.FAVORITE_UNNAMED',
        });
        const { fixture } = await setupComponentAsync({ favoriteViews: [namedFavorite, fallbackFavorite], isOpen: true });

        fixture.detectChanges();

        expect(getFixtureText(fixture)).toContain('Lunch box');
        expect(getFixtureText(fixture)).toContain('CONSUMPTION_LIST.FAVORITE_UNNAMED');
    });

    it('should emit repeat and remove actions with selected favorite', async () => {
        const favorite = createFavorite();
        const { component, fixture } = await setupComponentAsync({ favoriteViews: [createFavoriteView({ favorite })], isOpen: true });
        const repeatedSpy = vi.fn();
        const removedSpy = vi.fn();
        component.favoriteRepeated.subscribe(repeatedSpy);
        component.favoriteRemoved.subscribe(removedSpy);

        fixture.detectChanges();
        const buttons = fixture.debugElement.queryAll(By.directive(FdUiButtonComponent));
        buttons[0].triggerEventHandler('click');
        buttons[1].triggerEventHandler('click');

        expect(repeatedSpy).toHaveBeenCalledWith(favorite);
        expect(removedSpy).toHaveBeenCalledWith(favorite);
    });
});

async function setupComponentAsync(
    overrides: Partial<{
        count: number;
        favoriteViews: readonly FavoriteMealView[];
        isLoadingMore: boolean;
        isOpen: boolean;
        showLoadMore: boolean;
    }> = {},
): Promise<{
    component: MealListFavoritesComponent;
    fixture: ComponentFixture<MealListFavoritesComponent>;
}> {
    await TestBed.resetTestingModule()
        .configureTestingModule({
            imports: [MealListFavoritesComponent, TranslateModule.forRoot()],
        })
        .compileComponents();

    const fixture = TestBed.createComponent(MealListFavoritesComponent);
    fixture.componentRef.setInput('favoriteViews', overrides.favoriteViews ?? [createFavoriteView()]);
    fixture.componentRef.setInput('count', overrides.count ?? 1);
    fixture.componentRef.setInput('isOpen', overrides.isOpen ?? false);
    fixture.componentRef.setInput('showLoadMore', overrides.showLoadMore ?? false);
    fixture.componentRef.setInput('isLoadingMore', overrides.isLoadingMore ?? false);

    return {
        component: fixture.componentInstance,
        fixture,
    };
}

function getFixtureText(fixture: ComponentFixture<MealListFavoritesComponent>): string {
    const host = fixture.nativeElement as HTMLElement;
    return host.textContent;
}

function createFavoriteView(overrides: Partial<FavoriteMealView> = {}): FavoriteMealView {
    return {
        favorite: createFavorite(),
        displayName: 'Favorite meal',
        displayNameKey: 'CONSUMPTION_LIST.FAVORITE_UNNAMED',
        ...overrides,
    };
}

function createFavorite(overrides: Partial<FavoriteMeal> = {}): FavoriteMeal {
    return {
        id: 'favorite-1',
        mealId: 'meal-1',
        name: null,
        createdAtUtc: '2026-05-14T00:00:00Z',
        mealDate: '2026-05-14T12:00:00Z',
        mealType: 'LUNCH',
        totalCalories: 500,
        totalProteins: 30,
        totalFats: 20,
        totalCarbs: 50,
        itemCount: 2,
        ...overrides,
    };
}
