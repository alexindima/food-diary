import { ChangeDetectionStrategy, Component, computed, inject, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiCardComponent } from 'fd-ui-kit';

import { DashboardSetupFacade } from '../../lib/dashboard-setup.facade';

@Component({
    selector: 'fd-dashboard-setup-card',
    imports: [TranslatePipe, FdUiButtonComponent, FdUiCardComponent],
    providers: [DashboardSetupFacade],
    templateUrl: './dashboard-setup-card.html',
    styleUrl: './dashboard-setup-card.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardSetupCardComponent {
    public readonly profileComplete = input.required<boolean>();
    public readonly primaryAction = output();
    public readonly goalsAction = output();

    protected readonly facade = inject(DashboardSetupFacade);
    protected readonly primaryActionKey = computed(() =>
        this.profileComplete() ? 'DASHBOARD.INITIAL_SETUP.WEIGHT' : 'DASHBOARD.INITIAL_SETUP.PROFILE',
    );
}
