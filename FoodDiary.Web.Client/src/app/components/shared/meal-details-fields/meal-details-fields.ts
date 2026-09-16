import { ChangeDetectionStrategy, Component, input, model } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import { DEFAULT_SATIETY_LEVEL, normalizeSatietyLevel } from '../../../shared/lib/satiety-level.utils';
import { MealSatietyFieldsComponent } from '../meal-satiety-fields/meal-satiety-fields';

@Component({
    selector: 'fd-meal-details-fields',
    imports: [TranslatePipe, MealSatietyFieldsComponent],
    templateUrl: './meal-details-fields.html',
    styleUrls: ['./meal-details-fields.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MealDetailsFieldsComponent {
    public readonly date = model.required<string>();
    public readonly time = model.required<string>();
    public readonly comment = model.required<string>();
    public readonly preMealSatietyLevel = model<number | null>(DEFAULT_SATIETY_LEVEL);
    public readonly postMealSatietyLevel = model<number | null>(DEFAULT_SATIETY_LEVEL);
    public readonly textareaRows = input(DEFAULT_SATIETY_LEVEL);
    public readonly surface = input(true);
    public readonly density = input<'compact' | 'regular'>('compact');
    public readonly satietyLayout = input<'stacked' | 'columns'>('stacked');

    protected onPreMealSatietyLevelChange(value: number | null): void {
        const normalized = normalizeSatietyLevel(value);
        this.preMealSatietyLevel.set(normalized);
    }

    protected onPostMealSatietyLevelChange(value: number | null): void {
        const normalized = normalizeSatietyLevel(value);
        this.postMealSatietyLevel.set(normalized);
    }
}
