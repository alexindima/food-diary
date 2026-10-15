import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiHintDirective, FdUiStopActivationDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon';

import type { QualityGrade } from '../../../shared/models/quality-grade.data';

export type NutrientBadgesQuality = {
    score: number;
    grade: QualityGrade;
    hintKey: string;
};

@Component({
    selector: 'fd-nutrient-badges',
    imports: [FdUiStopActivationDirective, CommonModule, TranslatePipe, FdUiHintDirective, FdUiIconComponent, FdUiButtonComponent],
    templateUrl: './nutrient-badges.html',
    styleUrls: ['./nutrient-badges.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NutrientBadgesComponent {
    public readonly proteins = input.required<number>();
    public readonly fats = input.required<number>();
    public readonly carbs = input.required<number>();
    public readonly fiber = input.required<number>();
    public readonly alcohol = input.required<number>();
    public readonly calories = input<number | null>(null);
    public readonly quality = input<NutrientBadgesQuality | null>(null);
}
