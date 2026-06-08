import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';

import type { ClientMealView } from '../client-dashboard-lib/client-dashboard.mapper';

@Component({
    selector: 'fd-client-dashboard-meals-card',
    imports: [DatePipe, TranslatePipe, FdUiCardComponent],
    templateUrl: './client-dashboard-meals-card.html',
    styleUrl: './client-dashboard-meals-card.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ClientDashboardMealsCardComponent {
    public readonly meals = input<readonly ClientMealView[]>([]);
    public readonly total = input(0);
    public readonly showEmptyState = input(false);
}
