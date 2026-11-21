import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';
import { FdUiFormErrorComponent } from 'fd-ui-kit/form-error/fd-ui-form-error';

import { injectCurrentLanguage } from '../../../../../shared/i18n/inject-current-language';
import { LocalizedDatePipe } from '../../../../../shared/i18n/localized-date.pipe';
import type { ClientMealView } from '../client-dashboard-lib/client-dashboard.mapper';

@Component({
    selector: 'fd-client-dashboard-meals-card',
    imports: [LocalizedDatePipe, TranslatePipe, FdUiButtonComponent, FdUiCardComponent, FdUiFormErrorComponent],
    templateUrl: './client-dashboard-meals-card.html',
    styleUrl: './client-dashboard-meals-card.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ClientDashboardMealsCardComponent {
    protected readonly locale = injectCurrentLanguage();

    public readonly meals = input<readonly ClientMealView[]>([]);
    public readonly total = input(0);
    public readonly showEmptyState = input(false);
    public readonly loading = input(false);
    public readonly loadError = input(false);
    public readonly loadMore = output();
    protected readonly countLabelKey = computed(
        () => `DIETOLOGIST.CLIENT_DASHBOARD.MEALS.COUNT_${new Intl.PluralRules(this.locale()).select(this.total()).toUpperCase()}`,
    );
}
