import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import { FavoritesSectionComponent } from '../../../../../../components/shared/favorites-section/favorites-section';
import { MealCardComponent } from '../../../../../../components/shared/meal-card/meal-card';
import type { Meal } from '../../../../models/meal.data';
import type { MealDateGroupView } from '../../meal-list-lib/meal-list.types';

@Component({
    selector: 'fd-meal-list-planned',
    imports: [TranslatePipe, FavoritesSectionComponent, MealCardComponent],
    templateUrl: './meal-list-planned.html',
    styleUrl: '../../meal-list.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MealListPlannedComponent {
    public readonly groups = input.required<readonly MealDateGroupView[]>();
    public readonly isOpen = input.required<boolean>();
    public readonly favoriteLoadingIds = input.required<ReadonlySet<string>>();

    public readonly toggleRequested = output();
    public readonly mealOpened = output<Meal>();
    public readonly mealFavoriteToggle = output<Meal>();

    protected readonly plannedItemCount = computed(() => this.groups().reduce((count, group) => count + group.items.length, 0));
}
