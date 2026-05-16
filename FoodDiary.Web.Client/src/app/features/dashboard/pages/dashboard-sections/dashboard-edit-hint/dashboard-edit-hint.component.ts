import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import { NoticeBannerComponent } from '../../../../../components/shared/notice-banner/notice-banner.component';

@Component({
    selector: 'fd-dashboard-edit-hint',
    imports: [NoticeBannerComponent, TranslatePipe],
    templateUrl: './dashboard-edit-hint.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: {
        style: 'display: contents',
    },
})
export class DashboardEditHintComponent {
    public readonly actionLabel = input.required<string | null>();
    public readonly save = output();
}
