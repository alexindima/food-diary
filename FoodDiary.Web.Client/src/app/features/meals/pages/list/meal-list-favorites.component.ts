import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';

import { FavoritesSectionComponent } from '../../../../components/shared/favorites-section/favorites-section.component';
import type { FavoriteMeal } from '../../models/meal.data';
import type { FavoriteMealView } from './meal-list.component';

@Component({
    selector: 'fd-meal-list-favorites',
    imports: [DecimalPipe, TranslatePipe, FdUiHintDirective, FdUiButtonComponent, FavoritesSectionComponent],
    templateUrl: './meal-list-favorites.component.html',
    styleUrl: './meal-list.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MealListFavoritesComponent {
    public readonly favoriteViews = input.required<readonly FavoriteMealView[]>();
    public readonly count = input.required<number>();
    public readonly isOpen = input.required<boolean>();
    public readonly showLoadMore = input.required<boolean>();
    public readonly isLoadingMore = input.required<boolean>();

    public readonly toggleRequested = output();
    public readonly loadMore = output();
    public readonly favoriteRepeated = output<FavoriteMeal>();
    public readonly favoriteRemoved = output<FavoriteMeal>();
}
