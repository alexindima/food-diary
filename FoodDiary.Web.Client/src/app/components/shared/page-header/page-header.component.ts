import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
    selector: 'fd-page-header',
    templateUrl: './page-header.component.html',
    styleUrls: ['./page-header.component.scss'],
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
