
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, forwardRef, inject, input, output } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { FdUiAccentSurfaceComponent } from '../accent-surface/fd-ui-accent-surface.component';

export interface FdUiSatietyScaleLevel {
    value: number;
    titleKey: string;
    descriptionKey: string;
    gradient: string;
}

export const DEFAULT_SATIETY_LEVELS: FdUiSatietyScaleLevel[] = [
    {
        value: 0,
        titleKey: 'HUNGER_SCALE.LEVEL_0.TITLE',
        descriptionKey: 'HUNGER_SCALE.LEVEL_0.DESCRIPTION',
        gradient: 'linear-gradient(135deg, #e2e8f0, #cbd5f5)',
    },
    {
        value: 1,
        titleKey: 'HUNGER_SCALE.LEVEL_1.TITLE',
        descriptionKey: 'HUNGER_SCALE.LEVEL_1.DESCRIPTION',
        gradient: 'linear-gradient(135deg, #b91c1c, #ef4444)',
    },
    {
        value: 2,
        titleKey: 'HUNGER_SCALE.LEVEL_2.TITLE',
        descriptionKey: 'HUNGER_SCALE.LEVEL_2.DESCRIPTION',
        gradient: 'linear-gradient(135deg, #dc2626, #f97316)',
    },
    {
        value: 3,
        titleKey: 'HUNGER_SCALE.LEVEL_3.TITLE',
        descriptionKey: 'HUNGER_SCALE.LEVEL_3.DESCRIPTION',
        gradient: 'linear-gradient(135deg, #f97316, #fbbf24)',
    },
    {
        value: 4,
        titleKey: 'HUNGER_SCALE.LEVEL_4.TITLE',
        descriptionKey: 'HUNGER_SCALE.LEVEL_4.DESCRIPTION',
        gradient: 'linear-gradient(135deg, #fbbf24, #fde047)',
    },
    {
        value: 5,
        titleKey: 'HUNGER_SCALE.LEVEL_5.TITLE',
        descriptionKey: 'HUNGER_SCALE.LEVEL_5.DESCRIPTION',
        gradient: 'linear-gradient(135deg, #fde047, #a3e635)',
    },
    {
        value: 6,
        titleKey: 'HUNGER_SCALE.LEVEL_6.TITLE',
        descriptionKey: 'HUNGER_SCALE.LEVEL_6.DESCRIPTION',
        gradient: 'linear-gradient(135deg, #a3e635, #4ade80)',
    },
    {
        value: 7,
        titleKey: 'HUNGER_SCALE.LEVEL_7.TITLE',
        descriptionKey: 'HUNGER_SCALE.LEVEL_7.DESCRIPTION',
        gradient: 'linear-gradient(135deg, #4ade80, #2dd4bf)',
    },
    {
        value: 8,
        titleKey: 'HUNGER_SCALE.LEVEL_8.TITLE',
        descriptionKey: 'HUNGER_SCALE.LEVEL_8.DESCRIPTION',
        gradient: 'linear-gradient(135deg, #2dd4bf, #22d3ee)',
    },
    {
        value: 9,
        titleKey: 'HUNGER_SCALE.LEVEL_9.TITLE',
        descriptionKey: 'HUNGER_SCALE.LEVEL_9.DESCRIPTION',
        gradient: 'linear-gradient(135deg, #22d3ee, #0ea5e9)',
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
            useExisting: forwardRef(() => FdUiSatietyScaleComponent),
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

