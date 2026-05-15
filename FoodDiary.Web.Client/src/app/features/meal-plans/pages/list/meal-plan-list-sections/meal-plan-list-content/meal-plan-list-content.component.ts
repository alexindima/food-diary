import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon.component';
import { FdUiLoaderComponent } from 'fd-ui-kit/loader/fd-ui-loader.component';

import { FdCardHoverDirective } from '../../../../../../directives/card-hover.directive';
import type { MealPlanCardViewModel } from '../../../../lib/meal-plan-view.mapper';

@Component({
    selector: 'fd-meal-plan-list-content',
    imports: [DecimalPipe, TranslatePipe, FdUiIconComponent, FdUiLoaderComponent, FdCardHoverDirective],
    templateUrl: './meal-plan-list-content.component.html',
    styleUrl: '../../meal-plans-list-page.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MealPlanListContentComponent {
    public readonly isLoading = input.required<boolean>();
    public readonly plans = input.required<MealPlanCardViewModel[]>();
    public readonly planOpen = output<string>();
}
