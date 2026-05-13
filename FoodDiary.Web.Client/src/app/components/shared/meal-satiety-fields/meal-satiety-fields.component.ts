import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, input, model, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import {
    FdUiEmojiPickerComponent,
    type FdUiEmojiPickerOption,
    type FdUiEmojiPickerValue,
} from 'fd-ui-kit/emoji-picker/fd-ui-emoji-picker.component';
import {
    DEFAULT_HUNGER_LEVELS,
    DEFAULT_SATIETY_LEVELS,
    type FdUiSatietyScaleLevel,
} from 'fd-ui-kit/satiety-scale/fd-ui-satiety-scale.component';

import { DEFAULT_SATIETY_LEVEL, normalizeSatietyLevel } from '../../../shared/lib/satiety-level.utils';

@Component({
    selector: 'fd-meal-satiety-fields',
    imports: [TranslatePipe, FdUiEmojiPickerComponent],
    templateUrl: './meal-satiety-fields.component.html',
    styleUrls: ['./meal-satiety-fields.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MealSatietyFieldsComponent {
    private readonly translateService = inject(TranslateService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly languageVersion = signal(0);

    public readonly preMealSatietyLevel = model<number | null>(DEFAULT_SATIETY_LEVEL);
    public readonly postMealSatietyLevel = model<number | null>(DEFAULT_SATIETY_LEVEL);
    public readonly labelBeforeKey = input('MEAL_DETAILS.SATIETY_BEFORE');
    public readonly labelAfterKey = input('MEAL_DETAILS.SATIETY_AFTER');
    public readonly pickerSize = input<'sm' | 'md'>('sm');
    public readonly layout = input<'stacked' | 'columns' | 'compactColumns'>('stacked');

    public hungerEmojiOptions: Array<FdUiEmojiPickerOption<number>> = this.buildEmojiOptions(DEFAULT_HUNGER_LEVELS);
    public satietyEmojiOptions: Array<FdUiEmojiPickerOption<number>> = this.buildEmojiOptions(DEFAULT_SATIETY_LEVELS);
    public readonly preMealSatietyAriaLabel = computed(() => this.buildSatietyButtonAriaLabel('before'));
    public readonly postMealSatietyAriaLabel = computed(() => this.buildSatietyButtonAriaLabel('after'));

    public constructor() {
        this.translateService.onLangChange.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.languageVersion.update(version => version + 1);
            this.hungerEmojiOptions = this.buildEmojiOptions(DEFAULT_HUNGER_LEVELS);
            this.satietyEmojiOptions = this.buildEmojiOptions(DEFAULT_SATIETY_LEVELS);
        });
    }

    public onSatietyLevelChange(kind: 'before' | 'after', value: FdUiEmojiPickerValue | null): void {
        if (typeof value !== 'number') {
            return;
        }

        const normalized = normalizeSatietyLevel(value);
        if (kind === 'before') {
            this.preMealSatietyLevel.set(normalized);
        } else {
            this.postMealSatietyLevel.set(normalized);
        }
    }

    private buildSatietyButtonAriaLabel(kind: 'before' | 'after'): string {
        this.languageVersion();
        const value = kind === 'before' ? this.preMealSatietyLevel() : this.postMealSatietyLevel();
        const meta = this.getSatietyLevelMeta(kind, value);
        const labelKey = kind === 'before' ? this.labelBeforeKey() : this.labelAfterKey();
        const fieldLabel = this.translateService.instant(labelKey);
        return `${fieldLabel}. ${meta.label}. ${meta.description}`.trim();
    }

    private buildEmojiOptions(levels: readonly FdUiSatietyScaleLevel[]): Array<FdUiEmojiPickerOption<number>> {
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
        return {
            label: this.translateService.instant(config?.titleKey ?? ''),
            description: this.translateService.instant(config?.descriptionKey ?? ''),
        };
    }
}
