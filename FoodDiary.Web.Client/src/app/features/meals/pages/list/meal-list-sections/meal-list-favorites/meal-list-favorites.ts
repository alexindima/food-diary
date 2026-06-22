import { ChangeDetectionStrategy, Component, inject, input, output } from '@angular/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';

import { FavoritesSectionComponent } from '../../../../../../components/shared/favorites-section/favorites-section';
import type { MealCardItem } from '../../../../../../components/shared/meal-card/meal-card';
import { MealCardComponent } from '../../../../../../components/shared/meal-card/meal-card';
import type { FavoriteMeal } from '../../../../models/meal.data';
import type { FavoriteMealView } from '../../meal-list-lib/meal-list.types';

@Component({
    selector: 'fd-meal-list-favorites',
    imports: [TranslatePipe, FavoritesSectionComponent, MealCardComponent],
    templateUrl: './meal-list-favorites.html',
    styleUrl: '../../meal-list.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MealListFavoritesComponent {
    private readonly translateService = inject(TranslateService);

    public readonly favoriteViews = input.required<readonly FavoriteMealView[]>();
    public readonly count = input.required<number>();
    public readonly isOpen = input.required<boolean>();
    public readonly showLoadMore = input.required<boolean>();
    public readonly isLoadingMore = input.required<boolean>();
    public readonly favoriteLoadingIds = input.required<ReadonlySet<string>>();

    public readonly toggleRequested = output();
    public readonly loadMore = output();
    public readonly favoriteRepeated = output<FavoriteMeal>();
    public readonly favoriteRemoved = output<FavoriteMeal>();

    protected toMealCardItem(favoriteView: FavoriteMealView): MealCardItem {
        const favorite = favoriteView.favorite;

        return {
            id: favorite.mealId,
            date: favorite.mealDate,
            mealType: favorite.mealType,
            comment: favoriteView.displayName ?? this.translateService.instant(favoriteView.displayNameKey),
            imageUrl: null,
            totalCalories: favorite.totalCalories,
            totalProteins: favorite.totalProteins,
            totalFats: favorite.totalFats,
            totalCarbs: favorite.totalCarbs,
            totalFiber: 0,
            totalAlcohol: 0,
            isFavorite: true,
            favoriteMealId: favorite.id,
            items: Array.from({ length: favorite.itemCount }, () => ({})),
            aiSessions: null,
        };
    }
}
