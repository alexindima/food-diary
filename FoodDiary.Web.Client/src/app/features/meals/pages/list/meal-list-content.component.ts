import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon.component';
import { FdUiPaginationComponent } from 'fd-ui-kit/pagination/fd-ui-pagination.component';

import { ErrorStateComponent } from '../../../../components/shared/error-state/error-state.component';
import { MealCardComponent } from '../../../../components/shared/meal-card/meal-card.component';
import { SkeletonCardComponent } from '../../../../components/shared/skeleton-card/skeleton-card.component';
import type { Meal } from '../../models/meal.data';
import type { MealDateGroupView } from './meal-list.types';

@Component({
    selector: 'fd-meal-list-content',
    imports: [
        TranslatePipe,
        FdUiButtonComponent,
        FdUiIconComponent,
        FdUiPaginationComponent,
        ErrorStateComponent,
        MealCardComponent,
        SkeletonCardComponent,
    ],
    templateUrl: './meal-list-content.component.html',
    styleUrl: './meal-list.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MealListContentComponent {
    public readonly errorKey = input.required<string | null>();
    public readonly isLoading = input.required<boolean>();
    public readonly isEmptyState = input.required<boolean>();
    public readonly isNoResultsState = input.required<boolean>();
    public readonly groups = input.required<readonly MealDateGroupView[]>();
    public readonly totalPages = input.required<number>();
    public readonly totalItems = input.required<number>();
    public readonly currentPageIndex = input.required<number>();
    public readonly favoriteLoadingIds = input.required<ReadonlySet<string>>();

    public readonly retry = output();
    public readonly mealAdd = output();
    public readonly mealOpened = output<Meal>();
    public readonly mealFavoriteToggle = output<Meal>();
    public readonly pageIndexChange = output<number>();
}
