import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import type { ClientSummary } from '../../../../shared/models/dietologist.data';
import { DietologistClientCardComponent } from './dietologist-client-card.component';
import type { ClientCardViewModel } from './dietologist-clients.types';

@Component({
    selector: 'fd-dietologist-clients-list',
    imports: [DietologistClientCardComponent],
    templateUrl: './dietologist-clients-list.component.html',
    styleUrl: './dietologist-clients-page.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DietologistClientsListComponent {
    public readonly loading = input.required<boolean>();
    public readonly items = input.required<ClientCardViewModel[]>();

    public readonly clientOpen = output<ClientSummary>();
}
