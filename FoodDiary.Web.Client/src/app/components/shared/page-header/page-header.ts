import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import { HeaderActionsOverflowComponent } from '../header-actions-overflow/header-actions-overflow';

@Component({
    selector: 'fd-page-header',
    imports: [HeaderActionsOverflowComponent],
    templateUrl: './page-header.html',
    styleUrls: ['./page-header.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: {
        '[class.fd-page-header--mobile-static]': '!stickyOnMobile()',
        '[class.fd-page-header--compact-actions]': 'compactActions()',
    },
})
export class PageHeaderComponent {
    public readonly title = input.required<string>();
    public readonly subtitle = input<string>();
    public readonly stickyOnMobile = input(true);
    public readonly compactActions = input(false);
}
