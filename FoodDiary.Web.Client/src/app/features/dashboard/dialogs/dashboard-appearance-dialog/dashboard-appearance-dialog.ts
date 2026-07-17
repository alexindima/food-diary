import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog';

import { DashboardAppearanceFacade } from './dashboard-appearance.facade';

export type { DashboardAppearanceDialogData } from './dashboard-appearance.facade';

@Component({
    selector: 'fd-dashboard-appearance-dialog',
    templateUrl: './dashboard-appearance-dialog.html',
    styleUrl: './dashboard-appearance-dialog.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [TranslatePipe, FdUiDialogComponent],
    providers: [DashboardAppearanceFacade],
})
export class DashboardAppearanceDialogComponent {
    protected readonly appearance = inject(DashboardAppearanceFacade);
}
