import { ChangeDetectionStrategy, Component, ContentChild, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FdUiCardComponent, FdUiCardAppearance } from '../card/fd-ui-card.component';
import { FdUiEntityCardHeaderDirective } from './fd-ui-entity-card-header.directive';

@Component({
    selector: 'fd-ui-entity-card',
    standalone: true,
    imports: [CommonModule, FdUiCardComponent],
    templateUrl: './fd-ui-entity-card.component.html',
    styleUrls: ['./fd-ui-entity-card.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FdUiEntityCardComponent {
    @Input() public title?: string;
    @Input() public meta?: string;
    @Input() public appearance: FdUiCardAppearance = 'default';
    @Input() public imageUrl?: string | null;
    @Input() public fallbackImage = 'assets/images/vegetables.svg';

    @ContentChild(FdUiEntityCardHeaderDirective)
    public headerTemplate?: FdUiEntityCardHeaderDirective;

    private hasImageError = false;

    public get resolvedImage(): string {
        if (this.hasImageError || !this.imageUrl) {
            return this.fallbackImage;
        }

        return this.imageUrl;
    }

    public handleImageError(): void {
        if (!this.hasImageError) {
            this.hasImageError = true;
        }
    }
}
