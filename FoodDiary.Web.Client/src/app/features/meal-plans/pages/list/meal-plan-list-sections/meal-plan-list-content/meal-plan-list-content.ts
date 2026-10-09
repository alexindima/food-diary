import { ChangeDetectionStrategy, Component, inject, input, output } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon';
import { FdUiLoaderComponent } from 'fd-ui-kit/loader/fd-ui-loader';
import { map } from 'rxjs';

import { LocalizedNumberPipe } from '../../../../../../shared/i18n/localized-number.pipe';
import type { MealPlanId } from '../../../../../../shared/models/semantics/entity-id';
import { FdCardHoverDirective } from '../../../../../../shared/ui/card-hover.directive';
import type { MealPlanCardViewModel } from '../../../../lib/meal-plan-view.mapper';

@Component({
    selector: 'fd-meal-plan-list-content',
    imports: [FdUiButtonComponent, LocalizedNumberPipe, TranslatePipe, FdUiIconComponent, FdUiLoaderComponent, FdCardHoverDirective],
    templateUrl: './meal-plan-list-content.html',
    styleUrl: '../../meal-plans-list-page.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MealPlanListContentComponent {
    private readonly translate = inject(TranslateService);
    protected readonly language = toSignal(this.translate.onLangChange.pipe(map(event => event.lang)), {
        initialValue: this.translate.getCurrentLang() ?? 'en',
    });
    public readonly isLoading = input.required<boolean>();
    public readonly plans = input.required<MealPlanCardViewModel[]>();
    public readonly filtered = input(false);
    public readonly filterReset = output();
    public readonly planOpen = output<MealPlanId>();

    protected countLabelKey(type: 'DAYS' | 'RECIPES', count: number): string {
        return `MEAL_PLANS.${type}_${new Intl.PluralRules(this.language()).select(count).toUpperCase()}`;
    }
}
