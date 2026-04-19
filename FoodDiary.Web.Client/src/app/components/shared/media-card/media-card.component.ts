import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

export type MediaCardAppearance = 'raised' | 'plain';
export type MediaCardDensity = 'default' | 'compact';

@Component({
    selector: 'fd-media-card',
    standalone: true,
    templateUrl: './media-card.component.html',
    styleUrl: './media-card.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MediaCardComponent {
    public readonly appearance = input<MediaCardAppearance>('raised');
    public readonly density = input<MediaCardDensity>('default');

    protected readonly hostClass = computed(() => `fd-media-card--${this.appearance()} fd-media-card--density-${this.density()}`);
}
