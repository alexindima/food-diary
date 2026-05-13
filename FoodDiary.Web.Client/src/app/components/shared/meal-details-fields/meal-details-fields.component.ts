import { ChangeDetectionStrategy, Component, input, model, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import { DEFAULT_SATIETY_LEVEL, normalizeSatietyLevel } from '../../../shared/lib/satiety-level.utils';
import { MealSatietyFieldsComponent } from '../meal-satiety-fields/meal-satiety-fields.component';

@Component({
    selector: 'fd-meal-details-fields',
    imports: [TranslatePipe, MealSatietyFieldsComponent],
    templateUrl: './meal-details-fields.component.html',
    styleUrls: ['./meal-details-fields.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MealDetailsFieldsComponent {
    public readonly date = input.required<string>();
    public readonly time = input.required<string>();
    public readonly comment = input.required<string>();
    public readonly preMealSatietyLevel = model<number | null>(DEFAULT_SATIETY_LEVEL);
    public readonly postMealSatietyLevel = model<number | null>(DEFAULT_SATIETY_LEVEL);
    public readonly textareaRows = input(DEFAULT_SATIETY_LEVEL);
    public readonly surface = input(true);
    public readonly density = input<'compact' | 'regular'>('compact');
    public readonly satietyLayout = input<'stacked' | 'columns'>('stacked');

    public readonly dateChange = output<string>();
    public readonly timeChange = output<string>();
    public readonly commentChange = output<string>();

    public onPreMealSatietyLevelChange(value: number | null): void {
        const normalized = normalizeSatietyLevel(value);
        this.preMealSatietyLevel.set(normalized);
    }

    public onPostMealSatietyLevelChange(value: number | null): void {
        const normalized = normalizeSatietyLevel(value);
        this.postMealSatietyLevel.set(normalized);
    }
}
