import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon';
import { FdUiLoaderComponent } from 'fd-ui-kit/loader/fd-ui-loader';

import { FdCardHoverDirective } from '../../../../../../shared/ui/card-hover.directive';
import type { MealPlanCardViewModel } from '../../../../lib/meal-plan-view.mapper';

@Component({
    selector: 'fd-meal-plan-list-content',
    imports: [FdUiButtonComponent, DecimalPipe, TranslatePipe, FdUiIconComponent, FdUiLoaderComponent, FdCardHoverDirective],
    templateUrl: './meal-plan-list-content.html',
    styleUrl: '../../meal-plans-list-page.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MealPlanListContentComponent {
    public readonly isLoading = input.required<boolean>();
    public readonly plans = input.required<MealPlanCardViewModel[]>();
    public readonly filtered = input(false);
    public readonly filterReset = output();
    public readonly planOpen = output<string>();
}
