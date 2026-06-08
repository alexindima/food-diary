import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
    selector: 'fd-client-dashboard-header',
    imports: [],
    templateUrl: './client-dashboard-header.html',
    styleUrl: './client-dashboard-header.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ClientDashboardHeaderComponent {
    public readonly show = input(false);
    public readonly title = input('');
    public readonly email = input('');
    public readonly chips = input<readonly string[]>([]);
}
