import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
    selector: 'fd-media-card',
    templateUrl: './media-card.html',
    styleUrl: './media-card.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MediaCardComponent {}
