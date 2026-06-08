import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

@Component({
    selector: 'fd-client-dashboard-notices',
    imports: [TranslatePipe],
    templateUrl: './client-dashboard-notices.html',
    styleUrl: './client-dashboard-notices.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ClientDashboardNoticesComponent {
    public readonly detailsLoading = input(false);
    public readonly sectionLoadError = input<string | null>(null);
}
