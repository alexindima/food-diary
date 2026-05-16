import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
    selector: 'fd-media-card',
    templateUrl: './media-card.component.html',
    styleUrl: './media-card.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MediaCardComponent {}
