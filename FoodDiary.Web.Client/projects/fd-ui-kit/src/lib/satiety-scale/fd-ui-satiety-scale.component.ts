import { ChangeDetectionStrategy, ChangeDetectorRef, Component, inject, input, output } from '@angular/core';
import { type ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';

import { FdUiAccentSurfaceComponent } from '../accent-surface/fd-ui-accent-surface.component';

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
    standalone: true,
    imports: [TranslateModule, FdUiAccentSurfaceComponent],
    templateUrl: './fd-ui-satiety-scale.component.html',
    styleUrls: ['./fd-ui-satiety-scale.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [
        {
            provide: NG_VALUE_ACCESSOR,
            useExisting: FdUiSatietyScaleComponent,
            multi: true,
        },
    ],
})
export class FdUiSatietyScaleComponent implements ControlValueAccessor {
    private readonly cdr = inject(ChangeDetectorRef);

    public readonly label = input<string>();
    public readonly hint = input<string>();
    public readonly error = input<string | null>();
    public readonly required = input(false);
    public readonly layout = input<'grid' | 'vertical'>('grid');
    public readonly levels = input<FdUiSatietyScaleLevel[]>(DEFAULT_SATIETY_LEVELS);
    public readonly levelSelected = output<number>();

    protected value: number | null = null;
    protected disabled = false;

    private onChange: (value: number | null) => void = () => undefined;
    private onTouched: () => void = () => undefined;

    public writeValue(value: number | null): void {
        this.value = value;
        this.cdr.markForCheck();
    }

    public registerOnChange(fn: (value: number | null) => void): void {
        this.onChange = fn;
    }

    public registerOnTouched(fn: () => void): void {
        this.onTouched = fn;
    }

    public setDisabledState(isDisabled: boolean): void {
        this.disabled = isDisabled;
        this.cdr.markForCheck();
    }

    protected selectLevel(level: number): void {
        if (this.disabled) {
            return;
        }
        this.value = level;
        this.onChange(level);
        this.onTouched();
        this.levelSelected.emit(level);
    }

    protected handleBlur(): void {
        this.onTouched();
    }

    protected isSelected(level: number): boolean {
        return this.value === level;
    }
}
