import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, input, model, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';

export type FdUiSegmentedToggleOption = {
    label?: string;
    labelKey?: string;
    value: string;
};

export type FdUiSegmentedToggleAppearance = 'default' | 'soft';
export type FdUiSegmentedToggleSize = 'sm' | 'md';

@Component({
    selector: 'fd-ui-segmented-toggle',
    imports: [CommonModule],
    templateUrl: './fd-ui-segmented-toggle.html',
    styleUrls: ['./fd-ui-segmented-toggle.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: {
        '[class.fd-ui-segmented-toggle-host--full-width]': 'fullWidth()',
        '[class.fd-ui-segmented-toggle-host--fit-content]': 'fitContent()',
    },
})
export class FdUiSegmentedToggleComponent {
    private readonly translate = inject(TranslateService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly languageVersion = signal(0);

    public readonly options = input<FdUiSegmentedToggleOption[]>([]);
    public readonly selectedValue = model.required<string>();
    public readonly ariaLabel = input<string | null>(null);
    public readonly appearance = input<FdUiSegmentedToggleAppearance>('default');
    public readonly size = input<FdUiSegmentedToggleSize>('md');
    public readonly fullWidth = input(false);
    public readonly fitContent = input(false);
    public readonly shrinkItems = input(false);
    public readonly stackOnNarrow = input(true);
    public readonly wrapOnNarrow = input(false);
    protected readonly optionViewModels = computed(() => {
        this.languageVersion();
        return this.options().map(option => ({
            ...option,
            labelText: option.label ?? (option.labelKey !== undefined ? this.translate.instant(option.labelKey) : ''),
        }));
    });

    protected readonly containerClass = computed(() => {
        const classes = [
            'fd-ui-segmented-toggle',
            `fd-ui-segmented-toggle--appearance-${this.appearance()}`,
            `fd-ui-segmented-toggle--size-${this.size()}`,
        ];
        if (this.fullWidth()) {
            classes.push('fd-ui-segmented-toggle--full-width');
        }
        if (this.shrinkItems()) {
            classes.push('fd-ui-segmented-toggle--shrink-items');
        }
        if (this.stackOnNarrow()) {
            classes.push('fd-ui-segmented-toggle--stack-on-narrow');
        }
        if (this.wrapOnNarrow()) {
            classes.push('fd-ui-segmented-toggle--wrap-on-narrow');
        }
        return classes.join(' ');
    });

    public constructor() {
        this.translate.onLangChange.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.languageVersion.update(version => version + 1);
        });
    }

    protected select(value: string): void {
        if (value === this.selectedValue()) {
            return;
        }

        this.selectedValue.set(value);
    }
}
