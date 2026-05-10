import { CommonModule, NgOptimizedImage } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, contentChild, input, signal } from '@angular/core';

import { type FdUiCardAppearance, FdUiCardComponent } from '../card/fd-ui-card.component';
import { FdUiEntityCardHeaderDirective } from './fd-ui-entity-card-header.directive';

@Component({
    selector: 'fd-ui-entity-card',
    standalone: true,
    imports: [NgOptimizedImage, CommonModule, FdUiCardComponent],
    templateUrl: './fd-ui-entity-card.component.html',
    styleUrls: ['./fd-ui-entity-card.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FdUiEntityCardComponent {
    public readonly title = input<string>();
    public readonly meta = input<string>();
    public readonly appearance = input<FdUiCardAppearance>('default');
    public readonly imageUrl = input<string | null>();
    public readonly fallbackImage = input('assets/images/vegetables.svg');

    public readonly headerTemplate = contentChild(FdUiEntityCardHeaderDirective);

    private readonly hasImageError = signal(false);

    protected readonly resolvedImage = computed(() => {
        const imageUrl = this.imageUrl();
        if (this.hasImageError() || !imageUrl) {
            return this.fallbackImage();
        }

        return imageUrl;
    });

    public handleImageError(): void {
        if (!this.hasImageError()) {
            this.hasImageError.set(true);
        }
    }
}
