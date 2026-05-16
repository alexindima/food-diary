import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon.component';

import { NutrientBadgesComponent } from '../../nutrient-badges/nutrient-badges.component';
import type { EntityCardNormalizedQuality, EntityCardNutrition, EntityCardOwnershipIcon } from '../entity-card-lib/entity-card.types';

@Component({
    selector: 'fd-entity-card-body',
    imports: [TranslatePipe, FdUiHintDirective, FdUiButtonComponent, FdUiIconComponent, NutrientBadgesComponent],
    templateUrl: './entity-card-body.component.html',
    styleUrl: '../entity-card.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EntityCardBodyComponent {
    public readonly showFavorite = input.required<boolean>();
    public readonly isFavorite = input.required<boolean>();
    public readonly favoriteLoading = input.required<boolean>();
    public readonly favoriteIcon = input.required<string>();
    public readonly favoriteAriaLabel = input<string | null>(null);
    public readonly ownershipIcon = input<EntityCardOwnershipIcon>(null);
    public readonly title = input.required<string>();
    public readonly titleMuted = input<string | null>(null);
    public readonly description = input<string | null>(null);
    public readonly nutrition = input.required<EntityCardNutrition>();
    public readonly quality = input<EntityCardNormalizedQuality | null>(null);

    public readonly favoriteToggle = output();

    public handleFavoriteToggle(event: Event): void {
        event.stopPropagation();
        this.favoriteToggle.emit();
    }
}
