import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import type { ClientSummary } from '../../../../../shared/models/dietologist.data';
import { DietologistClientCardComponent } from '../dietologist-client-card/dietologist-client-card.component';
import type { ClientCardViewModel } from '../dietologist-clients-lib/dietologist-clients.types';

@Component({
    selector: 'fd-dietologist-clients-list',
    imports: [TranslatePipe, DietologistClientCardComponent],
    templateUrl: './dietologist-clients-list.component.html',
    styleUrl: '../dietologist-clients-page.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DietologistClientsListComponent {
    public readonly loading = input.required<boolean>();
    public readonly items = input.required<ClientCardViewModel[]>();

    public readonly clientOpen = output<ClientSummary>();
}
