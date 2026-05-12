import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, input, model, output, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import {
    FdUiEmojiPickerComponent,
    type FdUiEmojiPickerOption,
    type FdUiEmojiPickerValue,
} from 'fd-ui-kit/emoji-picker/fd-ui-emoji-picker.component';
import { DEFAULT_HUNGER_LEVELS, DEFAULT_SATIETY_LEVELS } from 'fd-ui-kit/satiety-scale/fd-ui-satiety-scale.component';

const DEFAULT_SATIETY_LEVEL = 3;
const MAX_SATIETY_LEVEL = 5;
const MIN_SATIETY_LEVEL = 1;
const LEGACY_SATIETY_SCALE_FACTOR = 2;

@Component({
    selector: 'fd-meal-details-fields',
    imports: [TranslatePipe, FdUiEmojiPickerComponent],
    templateUrl: './meal-details-fields.component.html',
    styleUrls: ['./meal-details-fields.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MealDetailsFieldsComponent {
    private readonly translateService = inject(TranslateService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly languageVersion = signal(0);

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

    private buildSatietyButtonAriaLabel(kind: 'before' | 'after'): string {
        this.languageVersion();
        const value = kind === 'before' ? this.preMealSatietyLevel() : this.postMealSatietyLevel();
        const meta = this.getSatietyLevelMeta(kind, value);
        const fieldLabel = this.translateService.instant(kind === 'before' ? 'MEAL_DETAILS.SATIETY_BEFORE' : 'MEAL_DETAILS.SATIETY_AFTER');
        return `${fieldLabel}. ${meta.label}. ${meta.description}`.trim();
    }

    public onSatietyLevelChange(kind: 'before' | 'after', value: FdUiEmojiPickerValue | null): void {
        if (typeof value !== 'number') {
            return;
        }

        const normalized = this.normalizeSatietyLevel(value);
        if (kind === 'before') {
            this.preMealSatietyLevel.set(normalized);
        } else {
            this.postMealSatietyLevel.set(normalized);
        }
    }

    private buildEmojiOptions(levels: typeof DEFAULT_SATIETY_LEVELS): Array<FdUiEmojiPickerOption<number>> {
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
        const normalizedValue = this.normalizeSatietyLevel(value);
        const levels = kind === 'before' ? DEFAULT_HUNGER_LEVELS : DEFAULT_SATIETY_LEVELS;
        const config = levels.find(level => level.value === normalizedValue);
        return {
            label: this.translateService.instant(config?.titleKey ?? ''),
            description: this.translateService.instant(config?.descriptionKey ?? ''),
        };
    }

    private normalizeSatietyLevel(value: number | null): number {
        if (value === null || !Number.isFinite(value) || value <= 0) {
            return DEFAULT_SATIETY_LEVEL;
        }

        if (value > MAX_SATIETY_LEVEL) {
            return Math.min(MAX_SATIETY_LEVEL, Math.max(MIN_SATIETY_LEVEL, Math.round(value / LEGACY_SATIETY_SCALE_FACTOR)));
        }

        return Math.max(MIN_SATIETY_LEVEL, value);
    }
}
