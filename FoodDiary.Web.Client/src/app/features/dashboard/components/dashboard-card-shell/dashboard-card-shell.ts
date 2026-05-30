import { ChangeDetectionStrategy, Component } from '@angular/core';

import { FdCardHoverDirective } from '../../../../directives/card-hover.directive';

@Component({
    selector: 'fd-dashboard-card-shell',
    imports: [FdCardHoverDirective],
    templateUrl: './dashboard-card-shell.html',
    styleUrl: './dashboard-card-shell.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardCardShellComponent {}
