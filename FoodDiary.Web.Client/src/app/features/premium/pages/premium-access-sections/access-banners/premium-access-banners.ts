import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import { NoticeBannerComponent } from '../../../../../components/shared/notice-banner/notice-banner';

@Component({
    selector: 'fd-premium-access-banners',
    imports: [NoticeBannerComponent, TranslatePipe],
    templateUrl: './premium-access-banners.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: {
        style: 'display: contents',
    },
})
export class PremiumAccessBannersComponent {
    public readonly checkoutReturnState = input.required<'success' | 'canceled' | null>();
    public readonly errorMessage = input.required<string | null>();

    public readonly retry = output();
}
