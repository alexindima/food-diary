import { ChangeDetectionStrategy, Component } from '@angular/core';
import { FdCardHoverDirective } from '../../../../directives/card-hover.directive';

@Component({
    selector: 'fd-dashboard-card-shell',
    standalone: true,
    imports: [FdCardHoverDirective],
    templateUrl: './dashboard-card-shell.component.html',
    styleUrl: './dashboard-card-shell.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardCardShellComponent {}
