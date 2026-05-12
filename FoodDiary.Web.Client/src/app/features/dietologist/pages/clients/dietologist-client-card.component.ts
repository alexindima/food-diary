import { NgOptimizedImage } from '@angular/common';
import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';

import type { ClientSummary } from '../../models/dietologist.data';
import type { ClientCardViewModel } from './dietologist-clients-page.component';

@Component({
    selector: 'fd-dietologist-client-card',
    imports: [NgOptimizedImage, FdUiCardComponent],
    templateUrl: './dietologist-client-card.component.html',
    styleUrl: './dietologist-clients-page.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DietologistClientCardComponent {
    public readonly item = input.required<ClientCardViewModel>();

    public readonly clientOpen = output<ClientSummary>();
}
