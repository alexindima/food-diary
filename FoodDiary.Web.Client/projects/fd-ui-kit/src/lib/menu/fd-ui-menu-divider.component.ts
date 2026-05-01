import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
    selector: 'fd-ui-menu-divider',
    standalone: true,
    templateUrl: './fd-ui-menu-divider.component.html',
    styleUrls: ['./fd-ui-menu.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FdUiMenuDividerComponent {}
