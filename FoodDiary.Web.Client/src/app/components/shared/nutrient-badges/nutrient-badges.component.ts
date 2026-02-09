import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiIconModule } from 'fd-ui-kit/material';

@Component({
    selector: 'fd-nutrient-badges',
    standalone: true,
    imports: [CommonModule, TranslatePipe, FdUiIconModule],
    templateUrl: './nutrient-badges.component.html',
    styleUrls: ['./nutrient-badges.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NutrientBadgesComponent {
    public readonly proteins = input.required<number>();
    public readonly fats = input.required<number>();
    public readonly carbs = input.required<number>();
    public readonly fiber = input.required<number>();
    public readonly alcohol = input.required<number>();
}
