import { ChangeDetectionStrategy, Component, HostBinding, input } from '@angular/core';

@Component({
    selector: 'fd-page-header',
    templateUrl: './page-header.component.html',
    styleUrls: ['./page-header.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PageHeaderComponent {
    public readonly title = input.required<string>();
    public readonly subtitle = input<string>();
    public readonly stickyOnMobile = input(true);

    @HostBinding('class.fd-page-header--mobile-static')
    protected get mobileStaticClass(): boolean {
        return !this.stickyOnMobile();
    }
}
