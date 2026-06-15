import { ChangeDetectionStrategy, ChangeDetectorRef, Component, effect, inject, input, model, output } from '@angular/core';
import type { FormValueControl } from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';

import { FdUiAccentSurfaceComponent } from '../accent-surface/fd-ui-accent-surface';

export type FdUiSatietyScaleLevel = {
    value: number;
    emoji: string;
    titleKey: string;
    descriptionKey: string;
    gradient: string;
};

export const DEFAULT_HUNGER_LEVELS: FdUiSatietyScaleLevel[] = [
    {
        value: 1,
        emoji: '😫',
        titleKey: 'HUNGER_BEFORE_SCALE.LEVEL_1.TITLE',
        descriptionKey: 'HUNGER_BEFORE_SCALE.LEVEL_1.DESCRIPTION',
        gradient: 'linear-gradient(135deg, var(--fd-color-red-700), var(--fd-color-danger))',
    },
    {
        value: 2,
        emoji: '🥺',
        titleKey: 'HUNGER_BEFORE_SCALE.LEVEL_2.TITLE',
        descriptionKey: 'HUNGER_BEFORE_SCALE.LEVEL_2.DESCRIPTION',
        gradient: 'linear-gradient(135deg, var(--fd-color-red-600), var(--fd-color-orange-500))',
    },
    {
        value: 3,
        emoji: '😐',
        titleKey: 'HUNGER_BEFORE_SCALE.LEVEL_3.TITLE',
        descriptionKey: 'HUNGER_BEFORE_SCALE.LEVEL_3.DESCRIPTION',
        gradient: 'linear-gradient(135deg, var(--fd-color-orange-500), var(--fd-color-yellow-300))',
    },
    {
        value: 4,
        emoji: '🙂',
        titleKey: 'HUNGER_BEFORE_SCALE.LEVEL_4.TITLE',
        descriptionKey: 'HUNGER_BEFORE_SCALE.LEVEL_4.DESCRIPTION',
        gradient:
            'linear-gradient(135deg, color-mix(in srgb, var(--fd-color-green-500) 55%, var(--fd-color-yellow-300)), color-mix(in srgb, var(--fd-color-green-500) 82%, var(--fd-color-white)))',
    },
    {
        value: 5,
        emoji: '😌',
        titleKey: 'HUNGER_BEFORE_SCALE.LEVEL_5.TITLE',
        descriptionKey: 'HUNGER_BEFORE_SCALE.LEVEL_5.DESCRIPTION',
        gradient:
            'linear-gradient(135deg, color-mix(in srgb, var(--fd-color-sky-500) 60%, var(--fd-color-teal-500)), var(--fd-color-sky-500))',
    },
];

export const DEFAULT_SATIETY_LEVELS: FdUiSatietyScaleLevel[] = [
    {
        value: 1,
        emoji: '😟',
        titleKey: 'SATIETY_AFTER_SCALE.LEVEL_1.TITLE',
        descriptionKey: 'SATIETY_AFTER_SCALE.LEVEL_1.DESCRIPTION',
        gradient: 'linear-gradient(135deg, var(--fd-color-red-700), var(--fd-color-danger))',
    },
    {
        value: 2,
        emoji: '😕',
        titleKey: 'SATIETY_AFTER_SCALE.LEVEL_2.TITLE',
        descriptionKey: 'SATIETY_AFTER_SCALE.LEVEL_2.DESCRIPTION',
        gradient: 'linear-gradient(135deg, var(--fd-color-red-600), var(--fd-color-orange-500))',
    },
    {
        value: 3,
        emoji: '😊',
        titleKey: 'SATIETY_AFTER_SCALE.LEVEL_3.TITLE',
        descriptionKey: 'SATIETY_AFTER_SCALE.LEVEL_3.DESCRIPTION',
        gradient: 'linear-gradient(135deg, var(--fd-color-orange-500), var(--fd-color-yellow-300))',
    },
    {
        value: 4,
        emoji: '😋',
        titleKey: 'SATIETY_AFTER_SCALE.LEVEL_4.TITLE',
        descriptionKey: 'SATIETY_AFTER_SCALE.LEVEL_4.DESCRIPTION',
        gradient:
            'linear-gradient(135deg, color-mix(in srgb, var(--fd-color-green-500) 55%, var(--fd-color-yellow-300)), color-mix(in srgb, var(--fd-color-green-500) 82%, var(--fd-color-white)))',
    },
    {
        value: 5,
        emoji: '🥴',
        titleKey: 'SATIETY_AFTER_SCALE.LEVEL_5.TITLE',
        descriptionKey: 'SATIETY_AFTER_SCALE.LEVEL_5.DESCRIPTION',
        gradient:
            'linear-gradient(135deg, color-mix(in srgb, var(--fd-color-sky-500) 60%, var(--fd-color-teal-500)), var(--fd-color-sky-500))',
    },
];

@Component({
    selector: 'fd-ui-satiety-scale',
    imports: [TranslatePipe, FdUiAccentSurfaceComponent],
    templateUrl: './fd-ui-satiety-scale.html',
    styleUrls: ['./fd-ui-satiety-scale.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FdUiSatietyScaleComponent implements FormValueControl<number | null> {
    private readonly cdr = inject(ChangeDetectorRef);

    public readonly label = input<string>();
    public readonly hint = input<string>();
    public readonly error = input<string | null>();
    public readonly required = input(false);
    public readonly layout = input<'grid' | 'vertical'>('grid');
    public readonly levels = input<FdUiSatietyScaleLevel[]>(DEFAULT_SATIETY_LEVELS);
    public readonly value = model<number | null>(null);
    public readonly touched = model(false);
    public readonly disabled = input(false);
    public readonly levelSelected = output<number>();

    protected selectedValue: number | null = null;

    public constructor() {
        effect(() => {
            this.selectedValue = this.value();
            this.cdr.markForCheck();
        });
    }

    protected selectLevel(level: number): void {
        if (this.disabled()) {
            return;
        }
        this.selectedValue = level;
        this.value.set(level);
        this.touched.set(true);
        this.levelSelected.emit(level);
    }

    protected touchControl(): void {
        this.touched.set(true);
    }

    protected isSelected(level: number): boolean {
        return this.selectedValue === level;
    }
}
