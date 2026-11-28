import { ChangeDetectionStrategy, Component, computed, inject, input, model } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiEmojiPickerComponent, type FdUiEmojiPickerOption } from 'fd-ui-kit';
import { DEFAULT_HUNGER_LEVELS, DEFAULT_SATIETY_LEVELS, type FdUiSatietyScaleLevel } from 'fd-ui-kit/satiety-scale/fd-ui-satiety-scale';

import { normalizeSatietyLevel } from '../../../shared/lib/satiety-level.utils';

@Component({
    selector: 'fd-meal-satiety-fields',
    imports: [TranslatePipe, FdUiEmojiPickerComponent, FdUiButtonComponent],
    templateUrl: './meal-satiety-fields.html',
    styleUrls: ['./meal-satiety-fields.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MealSatietyFieldsComponent {
    private readonly translateService = inject(TranslateService);
    private readonly languageChange = toSignal(this.translateService.onLangChange);

    public readonly preMealSatietyLevel = model<number | null>(null);
    public readonly postMealSatietyLevel = model<number | null>(null);
    public readonly labelBeforeKey = input('MEAL_DETAILS.SATIETY_BEFORE');
    public readonly labelAfterKey = input('MEAL_DETAILS.SATIETY_AFTER');
    public readonly pickerSize = input<'sm' | 'md'>('sm');
    public readonly layout = input<'stacked' | 'columns' | 'compactColumns'>('stacked');

    protected readonly hungerEmojiOptions = computed(() => this.buildEmojiOptions(DEFAULT_HUNGER_LEVELS));
    protected readonly satietyEmojiOptions = computed(() => this.buildEmojiOptions(DEFAULT_SATIETY_LEVELS));
    protected readonly preMealSatietyAriaLabel = computed(() => this.buildSatietyButtonAriaLabel('before'));
    protected readonly postMealSatietyAriaLabel = computed(() => this.buildSatietyButtonAriaLabel('after'));

    protected onSatietyLevelChange(kind: 'before' | 'after', value: number | null): void {
        const normalized = normalizeSatietyLevel(value);
        if (kind === 'before') {
            this.preMealSatietyLevel.set(normalized);
        } else {
            this.postMealSatietyLevel.set(normalized);
        }
    }

    private buildSatietyButtonAriaLabel(kind: 'before' | 'after'): string {
        this.languageChange();
        const value = kind === 'before' ? this.preMealSatietyLevel() : this.postMealSatietyLevel();
        const meta = this.getSatietyLevelMeta(kind, value);
        const labelKey = kind === 'before' ? this.labelBeforeKey() : this.labelAfterKey();
        const fieldLabel = this.translateService.instant(labelKey);
        return `${fieldLabel}. ${meta.label}. ${meta.description}`.trim();
    }

    private buildEmojiOptions(levels: readonly FdUiSatietyScaleLevel[]): Array<FdUiEmojiPickerOption<number>> {
        this.languageChange();
        return levels.map(level => {
            const label = this.translateService.instant(level.titleKey);
            const description = this.translateService.instant(level.descriptionKey);
            return {
                value: level.value,
                emoji: level.emoji,
                label,
                description,
                ariaLabel: `${label}. ${description}`,
                hint: `${label}. ${description}`,
            };
        });
    }

    private getSatietyLevelMeta(kind: 'before' | 'after', value: number | null): { label: string; description: string } {
        const normalizedValue = normalizeSatietyLevel(value);
        const levels = kind === 'before' ? DEFAULT_HUNGER_LEVELS : DEFAULT_SATIETY_LEVELS;
        const config = levels.find(level => level.value === normalizedValue);
        if (config === undefined) {
            return { label: this.translateService.instant('MEAL_DETAILS.SATIETY_UNANSWERED'), description: '' };
        }
        return {
            label: this.translateService.instant(config.titleKey),
            description: this.translateService.instant(config.descriptionKey),
        };
    }
}
