import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
    selector: 'fd-page-header',
    templateUrl: './page-header.component.html',
    styleUrls: ['./page-header.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: {
        '[class.fd-page-header--mobile-static]': '!stickyOnMobile()',
    },
})
export class PageHeaderComponent {
    public readonly title = input.required<string>();
    public readonly subtitle = input<string>();
    public readonly stickyOnMobile = input(true);
}
