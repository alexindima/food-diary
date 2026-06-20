import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon';
import { FdUiPaginationComponent } from 'fd-ui-kit/pagination/fd-ui-pagination';

import { ErrorStateComponent } from '../../../../../../components/shared/error-state/error-state';
import { MealCardComponent } from '../../../../../../components/shared/meal-card/meal-card';
import { SkeletonCardComponent } from '../../../../../../components/shared/skeleton-card/skeleton-card';
import type { Meal } from '../../../../models/meal.data';
import type { MealDateGroupView } from '../../meal-list-lib/meal-list.types';
import { MealListPlannedComponent } from '../meal-list-planned/meal-list-planned';

export type MealListEmptyState = 'empty' | 'no-results';

@Component({
    selector: 'fd-meal-list-content',
    imports: [
        TranslatePipe,
        FdUiButtonComponent,
        FdUiIconComponent,
        FdUiPaginationComponent,
        ErrorStateComponent,
        MealCardComponent,
        MealListPlannedComponent,
        SkeletonCardComponent,
    ],
    templateUrl: './meal-list-content.html',
    styleUrl: '../../meal-list.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MealListContentComponent {
    public readonly errorKey = input.required<string | null>();
    public readonly isLoading = input.required<boolean>();
    public readonly emptyState = input.required<MealListEmptyState | null>();
    public readonly plannedGroups = input.required<readonly MealDateGroupView[]>();
    public readonly isPlannedOpen = input.required<boolean>();
    public readonly groups = input.required<readonly MealDateGroupView[]>();
    public readonly totalPages = input.required<number>();
    public readonly totalItems = input.required<number>();
    public readonly currentPageIndex = input.required<number>();
    public readonly favoriteLoadingIds = input.required<ReadonlySet<string>>();

    public readonly retry = output();
    public readonly mealAdd = output();
    public readonly plannedToggle = output();
    public readonly mealOpened = output<Meal>();
    public readonly mealFavoriteToggle = output<Meal>();
    public readonly pageIndexChange = output<number>();
}
